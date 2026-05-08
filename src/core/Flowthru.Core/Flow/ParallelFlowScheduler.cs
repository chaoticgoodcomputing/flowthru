using System.Diagnostics;

namespace Flowthru.Flow;

/// <summary>
/// Core's shipped <see cref="IFlowScheduler"/> — runs a built flow
/// sequentially when <see cref="ExecutionOptions.Parallelism"/> is
/// <c>1</c>, parallelizes within topological layers up to N when
/// it's higher. The same way Core ships
/// <c>JsonFormatSerializer</c> as the one-built-in format and the
/// <c>[FlowthruStep]</c> Func factory as the one-built-in step
/// archetype, this is the one-built-in scheduler.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Algorithm.</strong> Build a dependency map from each
/// step's <c>Inputs</c> via the producer-by-item-label map already
/// computed by <see cref="DependencyAnalyzer"/>; track each step's
/// remaining-dependency count. Maintain a "ready set" of steps whose
/// dependencies have completed; pop and execute up to
/// <see cref="ExecutionOptions.Parallelism"/> at a time. As each
/// completes, decrement its dependents' counts and add newly-ready
/// steps to the ready set.
/// </para>
/// <para>
/// <strong>Sequential = Parallelism 1.</strong> When the parallelism
/// knob is <c>1</c>, the scheduler degrades to FIFO sequential
/// execution that matches the legacy behaviour
/// <see cref="ExecutionOptions.StopOnFirstError"/> respects (failure
/// stops new dispatch; in-flight steps complete; remaining steps
/// become <see cref="StepResult.Skipped"/>).
/// </para>
/// <para>
/// <strong>Failure semantics under parallelism.</strong> When a step
/// fails and <c>StopOnFirstError = true</c>, no new steps are
/// dispatched, but the steps already in flight run to completion —
/// you cannot gracefully cancel a running step from outside.
/// Their results join the <see cref="FlowResult"/> alongside the
/// failure.
/// </para>
/// </remarks>
public sealed class ParallelFlowScheduler : IFlowScheduler
{
  /// <inheritdoc/>
  public async Task<FlowResult> ExecuteAsync(
    BuiltFlow flow,
    ExecutionOptions options,
    CancellationToken cancellationToken = default
  )
  {
    if (flow is null) throw new ArgumentNullException(nameof(flow));
    if (options is null) throw new ArgumentNullException(nameof(options));

    var orderedSteps = flow.Steps;
    var resultsByIndex = new StepResult?[orderedSteps.Count];

    if (orderedSteps.Count == 0) return new FlowResult(Array.Empty<StepResult>());

    // ── Build dependency adjacency ────────────────────────────────────
    var indexByStep = new Dictionary<IStepNode, int>(ReferenceEqualityComparer.Instance);
    for (var i = 0; i < orderedSteps.Count; i++) indexByStep[orderedSteps[i]] = i;

    var producerByItemLabel = new Dictionary<string, int>(StringComparer.Ordinal);
    for (var i = 0; i < orderedSteps.Count; i++)
    {
      foreach (var output in orderedSteps[i].Outputs)
      {
        // Last writer wins; DependencyAnalyzer has already verified
        // the single-producer law before we got here.
        producerByItemLabel[output.Label] = i;
      }
    }

    var dependents = new List<int>[orderedSteps.Count];
    var pendingDeps = new int[orderedSteps.Count];
    for (var i = 0; i < orderedSteps.Count; i++) dependents[i] = new List<int>();
    for (var i = 0; i < orderedSteps.Count; i++)
    {
      foreach (var input in orderedSteps[i].Inputs)
      {
        if (producerByItemLabel.TryGetValue(input.Label, out var producerIdx)
            && producerIdx != i)
        {
          dependents[producerIdx].Add(i);
          pendingDeps[i]++;
        }
      }
    }

    // ── Initial ready set ─────────────────────────────────────────────
    var readyQueue = new Queue<int>();
    for (var i = 0; i < orderedSteps.Count; i++)
      if (pendingDeps[i] == 0) readyQueue.Enqueue(i);

    var stopAcceptingNew = false;
    var inFlight = new Dictionary<Task<(int Index, StepResult Result)>, byte>();
    var maxConcurrency = Math.Max(1, options.Parallelism);

    // ── Dispatch loop ─────────────────────────────────────────────────
    while (readyQueue.Count > 0 || inFlight.Count > 0)
    {
      while (
        !stopAcceptingNew
        && readyQueue.Count > 0
        && inFlight.Count < maxConcurrency
      )
      {
        var idx = readyQueue.Dequeue();
        inFlight[ExecuteOneAsync(orderedSteps[idx], idx, options, cancellationToken)] = 0;
      }

      if (inFlight.Count == 0) break;

      var completed = await Task.WhenAny(inFlight.Keys).ConfigureAwait(false);
      inFlight.Remove(completed);
      var (idx2, result2) = completed.Result;
      resultsByIndex[idx2] = result2;

      if (result2 is StepResult.Failed && options.StopOnFirstError)
      {
        stopAcceptingNew = true;
      }

      // Schedule dependents whose deps are now satisfied.
      if (!stopAcceptingNew)
      {
        foreach (var dependent in dependents[idx2])
        {
          if (--pendingDeps[dependent] == 0) readyQueue.Enqueue(dependent);
        }
      }
    }

    // ── Anything still pending becomes Skipped ────────────────────────
    for (var i = 0; i < orderedSteps.Count; i++)
    {
      resultsByIndex[i] ??= new StepResult.Skipped(
        orderedSteps[i].Label,
        "Earlier step failed under StopOnFirstError"
      );
    }

    return new FlowResult(resultsByIndex.Cast<StepResult>().ToList());
  }

  /// <summary>
  /// Execute a single step inside an <see cref="Activity"/> scope and
  /// return its outcome alongside its index for the dispatch loop's
  /// <see cref="Task.WhenAny"/> bookkeeping.
  /// </summary>
  private static async Task<(int Index, StepResult Result)> ExecuteOneAsync(
    IStepNode step,
    int index,
    ExecutionOptions options,
    CancellationToken cancellationToken
  )
  {
    if (cancellationToken.IsCancellationRequested)
    {
      return (index, new StepResult.Failed(
        step.Label,
        new RuntimeError.Cancelled("Cancellation requested before step execution")
      ));
    }

    if (options.DryRun == DryRunOption.On)
    {
      return (index, new StepResult.Skipped(step.Label, "Dry run"));
    }

    using var activity = FlowthruActivitySource.Source.StartActivity(
      FlowthruActivitySource.StepActivityName,
      ActivityKind.Internal,
      default(ActivityContext),
      new KeyValuePair<string, object?>[]
      {
        new(FlowthruActivitySource.TagStepLabel, step.Label),
        new(FlowthruActivitySource.TagStepInputCount, step.Inputs.Count),
        new(FlowthruActivitySource.TagStepOutputCount, step.Outputs.Count),
      }
    );

    var result = await step.Execute().Run(cancellationToken).ConfigureAwait(false);
    var stepResult = result switch
    {
      EffResult<FlowUnit>.Success => (StepResult)new StepResult.Succeeded(step.Label),
      EffResult<FlowUnit>.Failure f => new StepResult.Failed(
        step.Label,
        f.Error is RuntimeError.StepFailed ? f.Error : new RuntimeError.StepFailed(step.Label, f.Error)
      ),
      _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
    };

    activity?.SetStatus(
      stepResult is StepResult.Failed fail ? ActivityStatusCode.Error : ActivityStatusCode.Ok,
      (stepResult as StepResult.Failed)?.Error.Message
    );
    return (index, stepResult);
  }
}
