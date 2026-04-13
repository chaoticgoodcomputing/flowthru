using System.Collections.Concurrent;
using Flowthru.Core.Flows;
using Microsoft.Extensions.Logging;

namespace Flowthru.Core.Graph;

/// <summary>
/// Executes a built flow DAG using a task-graph scheduler.
/// </summary>
/// <remarks>
/// <para>
/// Steps are dispatched as soon as all their dependencies complete, up to a configurable
/// maximum degree of parallelism. With <c>MaxDegreeOfParallelism = 1</c> the scheduler
/// produces a valid topological order and is behaviourally equivalent to the previous
/// sequential layer-by-layer loop, though the specific ordering within a layer may differ
/// (depth-first rather than breadth-first).
/// </para>
/// <para>
/// <strong>Correctness guarantees provided by the DAG:</strong>
/// <list type="bullet">
/// <item>Single Producer Rule — no two steps write to the same catalog entry, so
/// concurrent steps in the same "layer" cannot have write conflicts.</item>
/// <item>Dependency edges — a step is never dispatched before all its producers have
/// written their outputs.</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class TaskGraphExecutor
{
  private readonly IReadOnlyList<FlowStep> _steps;
  private readonly int _maxDegreeOfParallelism;
  private readonly ILogger? _logger;
  private readonly Func<FlowStep, CancellationToken, Task<StepResult>> _executeStep;

  /// <param name="steps">
  /// The flat list of steps to execute (after slicing). Dependencies must have been
  /// resolved by <see cref="DependencyAnalyzer.BuildDependencyGraph"/> before calling
  /// <see cref="RunAsync"/>.
  /// </param>
  /// <param name="maxDegreeOfParallelism">
  /// Maximum concurrent steps. Pass 1 for sequential execution.
  /// Pass <see cref="int.MaxValue"/> for unbounded parallelism.
  /// </param>
  /// <param name="executeStep">Per-step execution delegate (matches <c>ExecuteStepWithTrackingAsync</c>).</param>
  /// <param name="logger">Optional logger.</param>
  internal TaskGraphExecutor(
    IReadOnlyList<FlowStep> steps,
    int maxDegreeOfParallelism,
    Func<FlowStep, CancellationToken, Task<StepResult>> executeStep,
    ILogger? logger = null
  )
  {
    if (maxDegreeOfParallelism < 1 && maxDegreeOfParallelism != -1)
    {
      throw new ArgumentOutOfRangeException(
        nameof(maxDegreeOfParallelism),
        "Must be -1 (unbounded) or a positive integer."
      );
    }

    _steps = steps;
    _maxDegreeOfParallelism = maxDegreeOfParallelism == -1 ? int.MaxValue : maxDegreeOfParallelism;
    _executeStep = executeStep;
    _logger = logger;
  }

  /// <summary>
  /// Runs all steps in dependency order, returning per-step results.
  /// </summary>
  /// <param name="stopOnFirstError">
  /// When <c>true</c>, cancels in-flight steps and stops dispatch on the first failure.
  /// When <c>false</c>, only the failed step and its transitive dependents are skipped;
  /// independent branches continue executing.
  /// </param>
  /// <param name="cancellationToken">External cancellation token.</param>
  /// <returns>
  /// Dictionary of step label → <see cref="StepResult"/>. Includes results for every step
  /// that was dispatched (not steps skipped due to a failed dependency).
  /// </returns>
  internal async Task<Dictionary<string, StepResult>> RunAsync(
    bool stopOnFirstError,
    CancellationToken cancellationToken
  )
  {
    // --- Build scheduling state ---------------------------------------------------

    // How many unfinished dependencies each step is still waiting on.
    var pendingDeps = new ConcurrentDictionary<FlowStep, int>(
      _steps.Select(s => new KeyValuePair<FlowStep, int>(s, s.Dependencies.Count))
    );

    // Reverse adjacency: for each step, which steps depend on it?
    var dependents = _steps.ToDictionary(s => s, _ => new List<FlowStep>());
    foreach (var step in _steps)
    {
      foreach (var dep in step.Dependencies)
      {
        // dep must be in dependents because BuildDependencyGraph only adds
        // steps that are in _steps (sliced or full set).
        if (dependents.TryGetValue(dep, out var list))
        {
          list.Add(step);
        }
      }
    }

    // Steps whose upstream is fully satisfied (or has no dependencies).
    // Channel is unbounded write / bounded dispatch (controlled by the semaphore).
    var readyQueue = new ConcurrentQueue<FlowStep>(_steps.Where(s => s.Dependencies.Count == 0));

    // Tracks which steps were skipped because an upstream dependency failed.
    var skipped = new HashSet<FlowStep>();

    var results = new ConcurrentDictionary<string, StepResult>();
    var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism, _maxDegreeOfParallelism);

    // Linked source: either the external token or our own stop-on-error cancellation.
    using var internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    var dispatchToken = internalCts.Token;

    // --- Dispatch loop ------------------------------------------------------------

    // We spin tasks until every step has either produced a result or been skipped.
    var inFlight = new List<Task>();
    var totalSteps = _steps.Count;

    while (results.Count + skipped.Count < totalSteps)
    {
      // Drain all currently runnable steps into in-flight tasks.
      while (readyQueue.TryDequeue(out var step))
      {
        if (skipped.Contains(step))
        {
          // Already marked skipped (a dependency failed after this was enqueued).
          continue;
        }

        await semaphore.WaitAsync(dispatchToken).ConfigureAwait(false);

        var capturedStep = step;
        var task = Task.Run(
          async () =>
          {
            try
            {
              _logger?.LogDebug("Dispatching step: {StepName}", capturedStep.Label);
              var result = await _executeStep(capturedStep, dispatchToken).ConfigureAwait(false);
              results[capturedStep.Label] = result;

              if (!result.Success)
              {
                _logger?.LogWarning("Step failed: {StepName}", capturedStep.Label);

                if (stopOnFirstError)
                {
                  // Cancel everything else.
                  internalCts.Cancel();
                }
                else
                {
                  // Only skip transitive dependents of this step.
                  SkipDownstream(capturedStep, skipped, dependents);
                }
              }
              else
              {
                // Notify dependents — decrement their pending count.
                EnqueueReadyDependents(capturedStep, pendingDeps, dependents, skipped, readyQueue);
              }
            }
            finally
            {
              semaphore.Release();
            }
          },
          dispatchToken
        );

        inFlight.Add(task);
      }

      if (inFlight.Count == 0)
      {
        // Nothing dispatched and nothing running. If not all steps accounted for,
        // the dependency graph has a cycle that AssignLayers should have caught.
        if (results.Count + skipped.Count < totalSteps)
        {
          var unaccounted = _steps
            .Where(s => !results.ContainsKey(s.Label) && !skipped.Contains(s))
            .Select(s => s.Label);
          throw new InvalidOperationException(
            $"Task graph stalled — possible undetected cycle. Unresolved steps: "
              + string.Join(", ", unaccounted)
          );
        }
        break;
      }

      // Wait for at least one in-flight task to finish, then re-check the ready queue.
      var completed = await Task.WhenAny(inFlight).ConfigureAwait(false);

      // Propagate any unhandled exceptions from the task wrapper.
      // (Step failures are encoded in StepResult, not thrown — but defensive.)
      try
      {
        await completed.ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        // Expected when stopOnFirstError cancels the token.
      }

      inFlight.Remove(completed);

      // If the dispatch token was cancelled (stop-on-first-error), drain remaining
      // in-flight tasks before surfacing.
      if (dispatchToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
      {
        // Wait for any tasks that are already running to finish (they may still complete
        // normally since they hold the semaphore slot — they only check the token at I/O
        // boundaries within ExecuteStepWithTrackingAsync).
        try
        {
          await Task.WhenAll(inFlight).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }

        break;
      }
    }

    // Propagate external cancellation to the caller.
    cancellationToken.ThrowIfCancellationRequested();

    return new Dictionary<string, StepResult>(results);
  }

  // ---------------------------------------------------------------------------
  // Helpers
  // ---------------------------------------------------------------------------

  private static void EnqueueReadyDependents(
    FlowStep completedStep,
    ConcurrentDictionary<FlowStep, int> pendingDeps,
    Dictionary<FlowStep, List<FlowStep>> dependents,
    HashSet<FlowStep> skipped,
    ConcurrentQueue<FlowStep> readyQueue
  )
  {
    foreach (var dependent in dependents[completedStep])
    {
      if (skipped.Contains(dependent))
      {
        continue;
      }

      var remaining = pendingDeps.AddOrUpdate(dependent, 0, (_, current) => current - 1);
      if (remaining == 0)
      {
        readyQueue.Enqueue(dependent);
      }
    }
  }

  private static void SkipDownstream(
    FlowStep failedStep,
    HashSet<FlowStep> skipped,
    Dictionary<FlowStep, List<FlowStep>> dependents
  )
  {
    // BFS over the dependents graph.
    var queue = new Queue<FlowStep>(dependents[failedStep]);
    while (queue.TryDequeue(out var step))
    {
      if (skipped.Add(step))
      {
        foreach (var downstream in dependents[step])
        {
          queue.Enqueue(downstream);
        }
      }
    }
  }
}
