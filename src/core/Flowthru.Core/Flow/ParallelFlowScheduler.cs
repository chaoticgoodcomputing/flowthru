using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
  private readonly ILogger _logger;
  private readonly IServiceProfileProvider _profiles;

  /// <summary>
  /// Construct with the engine's shared <see cref="ILogger"/> and the
  /// host's <see cref="IServiceProfileProvider"/>. The engine and every
  /// step share one <c>"Flowthru"</c>-category logger; this scheduler
  /// logs per-step lifecycle boundaries directly through it. When no
  /// logger is supplied (the historical parameterless ctor path used by
  /// <see cref="BuiltFlow.RunAsync()"/>) the <see cref="NullLogger"/>
  /// instance drops calls silently. When no profile provider is supplied
  /// the permissive <see cref="DefaultServiceProfileProvider"/> is used,
  /// so conflict gating is a no-op until a resource declares a capacity.
  /// </summary>
  public ParallelFlowScheduler(
    ILogger? logger = null,
    IServiceProfileProvider? profiles = null
  )
  {
    _logger = logger ?? NullLogger.Instance;
    _profiles = profiles ?? new DefaultServiceProfileProvider();
  }

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
    var runStopwatch = Stopwatch.StartNew();

    if (orderedSteps.Count == 0)
    {
      runStopwatch.Stop();
      return new FlowResult(Array.Empty<StepResult>(), runStopwatch.Elapsed);
    }

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

    // ── Conflict keys per step ────────────────────────────────────────
    // A step's conflict keys come from its service dependencies whose
    // resolved profile constrains concurrency (capacity < ∞). The
    // scheduler admits at most `capacity` concurrent holders of a key,
    // refusing to *dispatch* an over-capacity step rather than
    // dispatching it and blocking — the latter wastes a threadpool
    // thread (the pathology a single-worker resource causes today).
    // Item-derived read/write keys are a later slice; for now a step's
    // keys are its own service deps.
    var capacityByKey = new Dictionary<string, int>(StringComparer.Ordinal);
    var keysByStep = new string[orderedSteps.Count][];
    for (var i = 0; i < orderedSteps.Count; i++)
    {
      List<string>? keys = null;
      foreach (var (dep, op) in ConflictKeys.Of(orderedSteps[i]))
      {
        var capacity = _profiles.Resolve(dep).CapacityFor(op);
        if (capacity >= int.MaxValue) continue; // unbounded for this op — no conflict
        // The op-class is part of the key, so read:X and write:X are
        // distinct (concurrent readers don't conflict with one writer).
        var key = ConflictKeys.KeyFor(dep, op);
        (keys ??= new List<string>()).Add(key);
        // A key identifies one shared resource+op, so its capacity is
        // global; if sources disagree, the most restrictive wins.
        capacityByKey[key] = capacityByKey.TryGetValue(key, out var existing)
          ? Math.Min(existing, capacity)
          : capacity;
      }
      keysByStep[i] = keys is null ? Array.Empty<string>() : keys.Distinct().ToArray();
    }

    // ── Initial ready set ─────────────────────────────────────────────
    // A List (not a Queue) so the dispatch pass can skip a step blocked
    // on a full key and still dispatch a later, unblocked one.
    var ready = new List<int>();
    for (var i = 0; i < orderedSteps.Count; i++)
      if (pendingDeps[i] == 0) ready.Add(i);

    var stopAcceptingNew = false;
    var inFlight = new Dictionary<Task<(int Index, StepResult Result)>, byte>();
    var inFlightByKey = new Dictionary<string, int>(StringComparer.Ordinal);
    var maxConcurrency = Math.Max(1, options.Parallelism);

    bool KeysAvailable(string[] keys)
    {
      foreach (var key in keys)
        if (inFlightByKey.GetValueOrDefault(key) >= capacityByKey[key]) return false;
      return true;
    }

    // ── Dispatch loop ─────────────────────────────────────────────────
    while (ready.Count > 0 || inFlight.Count > 0)
    {
      // Dispatch pass: scan the ready set in order, dispatching each step
      // whose keys all have free capacity, up to maxConcurrency. Acquire
      // all of a step's keys atomically at dispatch — a step never waits
      // on a key mid-flight, so the conflict layer cannot deadlock.
      var r = 0;
      while (!stopAcceptingNew && r < ready.Count && inFlight.Count < maxConcurrency)
      {
        var idx = ready[r];
        var step = orderedSteps[idx];

        // Cache short-circuit: a fresh step is never dispatched and
        // acquires no keys; its dependents unlock off the synthetic
        // Succeeded exactly as a real success would.
        if (options.CachePlan is { } plan && plan.IsFresh(step.Label))
        {
          ready.RemoveAt(r);
          resultsByIndex[idx] = new StepResult.Succeeded(step.Label, TimeSpan.Zero)
          {
            Reason = "cached",
          };
          foreach (var dependent in dependents[idx])
            if (--pendingDeps[dependent] == 0) ready.Add(dependent);
          continue; // list shifted left; re-evaluate the same index
        }

        // Conflict gate: only dispatch when every key has free capacity.
        if (!KeysAvailable(keysByStep[idx])) { r++; continue; }

        foreach (var key in keysByStep[idx])
          inFlightByKey[key] = inFlightByKey.GetValueOrDefault(key) + 1;
        ready.RemoveAt(r);
        inFlight[ExecuteOneAsync(step, idx, options, cancellationToken)] = 0;
        // list shifted; the same index now points at the next ready step
      }

      if (inFlight.Count == 0) break;

      var completed = await Task.WhenAny(inFlight.Keys).ConfigureAwait(false);
      inFlight.Remove(completed);
      var (idx2, result2) = completed.Result;
      resultsByIndex[idx2] = result2;

      // Release the completed step's keys.
      foreach (var key in keysByStep[idx2])
        inFlightByKey[key] = inFlightByKey.GetValueOrDefault(key) - 1;

      if (result2 is StepResult.Failed && options.StopOnFirstError)
      {
        stopAcceptingNew = true;
      }

      // Schedule dependents whose deps are now satisfied.
      if (!stopAcceptingNew)
      {
        foreach (var dependent in dependents[idx2])
        {
          if (--pendingDeps[dependent] == 0) ready.Add(dependent);
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

    runStopwatch.Stop();
    return new FlowResult(resultsByIndex.Cast<StepResult>().ToList(), runStopwatch.Elapsed);
  }

  /// <summary>
  /// Execute a single step inside an <see cref="Activity"/> scope and
  /// return its outcome alongside its index for the dispatch loop's
  /// <c>Task.WhenAny</c> bookkeeping. Wall-clock duration is
  /// captured by a <see cref="Stopwatch"/> spanning the load →
  /// transform → save chain — diagnostic providers (heat-map,
  /// step-timings, run-summary) consume it directly off the
  /// resulting <see cref="StepResult"/>.
  /// </summary>
  private async Task<(int Index, StepResult Result)> ExecuteOneAsync(
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
        new RuntimeError.Cancelled("Cancellation requested before step execution"),
        TimeSpan.Zero
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

    _logger.LogInformation("  → {StepLabel} executing…", step.Label);

    var sw = Stopwatch.StartNew();
    var result = await step.Execute().Run(cancellationToken).ConfigureAwait(false);
    sw.Stop();

    var stepResult = result switch
    {
      EffResult<FlowUnit>.Success => (StepResult)new StepResult.Succeeded(step.Label, sw.Elapsed),
      EffResult<FlowUnit>.Failure f => new StepResult.Failed(
        step.Label,
        f.Error is RuntimeError.StepFailed ? f.Error : new RuntimeError.StepFailed(step.Label, f.Error),
        sw.Elapsed
      ),
      _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
    };

    var ms = sw.Elapsed.TotalMilliseconds;
    if (stepResult is StepResult.Failed failed)
    {
      _logger.LogWarning(
        "  ✗ {StepLabel} failed in {Duration:F2} ms: {Reason}",
        step.Label, ms, failed.Error.Message
      );
    }
    else
    {
      _logger.LogInformation("  ✓ {StepLabel} ({Duration:F2} ms)", step.Label, ms);
    }

    activity?.SetStatus(
      stepResult is StepResult.Failed ? ActivityStatusCode.Error : ActivityStatusCode.Ok,
      (stepResult as StepResult.Failed)?.Error.Message
    );
    return (index, stepResult);
  }
}
