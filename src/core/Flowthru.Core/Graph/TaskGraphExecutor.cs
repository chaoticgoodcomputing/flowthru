using System.Collections.Concurrent;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph.Scheduling;
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
  private readonly ISchedulingStrategy _strategy;
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
  /// <param name="strategy">Priority strategy used to order ready steps on each dispatch cycle.</param>
  /// <param name="logger">Optional logger.</param>
  internal TaskGraphExecutor(
    IReadOnlyList<FlowStep> steps,
    int maxDegreeOfParallelism,
    Func<FlowStep, CancellationToken, Task<StepResult>> executeStep,
    ISchedulingStrategy strategy,
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
    _strategy = strategy;
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
    // Fast-path: surface external cancellation before any work begins.
    // ThrowIfCancellationRequested throws OperationCanceledException (base type), which
    // is what callers expect — not the TaskCanceledException that semaphore.WaitAsync
    // would throw if we let it reach the dispatch loop with an already-cancelled token.
    cancellationToken.ThrowIfCancellationRequested();

    // --- Build scheduling state ---------------------------------------------------

    // How many unfinished dependencies each step is still waiting on.
    var pendingDeps = new ConcurrentDictionary<FlowStep, int>(
      _steps.Select(s => new KeyValuePair<FlowStep, int>(s, s.Dependencies.Count))
    );

    // Reverse adjacency: for each step, which steps depend on it?
    var dependents = _steps.ToDictionary(
      s => s,
      s => (IReadOnlyList<FlowStep>)new List<FlowStep>()
    );
    foreach (var step in _steps)
    {
      foreach (var dep in step.Dependencies)
      {
        // dep must be in dependents because BuildDependencyGraph only adds
        // steps that are in _steps (sliced or full set).
        if (dependents.TryGetValue(dep, out var list))
        {
          ((List<FlowStep>)list).Add(step);
        }
      }
    }

    var schedulingContext = new SchedulingContext(dependents);

    // Newly-ready steps are added here by concurrent task completions; a ConcurrentBag
    // is safe for multi-producer, single-consumer access. On each dispatch cycle the
    // main loop drains it into a List and passes it to the strategy for ordering.
    var readyBag = new ConcurrentBag<FlowStep>(_steps.Where(s => s.Dependencies.Count == 0));

    // Tracks which steps were skipped because an upstream dependency failed.
    var skipped = new HashSet<FlowStep>();

    var results = new ConcurrentDictionary<string, StepResult>();
    var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism, _maxDegreeOfParallelism);
    var dispatchedCount = 0; // incremented atomically before each step starts

    // Linked source: either the external token or our own stop-on-error cancellation.
    using var internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    var dispatchToken = internalCts.Token;

    // --- Dispatch loop ------------------------------------------------------------

    // We spin tasks until every step has either produced a result or been skipped.
    var inFlight = new List<Task>();
    var totalSteps = _steps.Count;

    // Pre-populate worker slot IDs (1..N). The semaphore guarantees at most N concurrent
    // tasks hold the semaphore — so a slot is always available on a successful WaitAsync.
    var slotCount = Math.Max(1, Math.Min(_maxDegreeOfParallelism, totalSteps));
    var workerSlots = new ConcurrentQueue<int>(Enumerable.Range(1, slotCount));

    while (results.Count + skipped.Count < totalSteps)
    {
      // Drain all currently runnable steps into in-flight tasks.
      // Collect from the concurrent bag into a snapshot, then ask the strategy to order them.
      var readySnapshot = new List<FlowStep>();
      while (readyBag.TryTake(out var taken))
      {
        readySnapshot.Add(taken);
      }

      var prioritised = _strategy.Prioritize(readySnapshot, schedulingContext);

      foreach (var step in prioritised)
      {
        if (skipped.Contains(step))
        {
          // Already marked skipped (a dependency failed after this was enqueued).
          continue;
        }

        await semaphore.WaitAsync(dispatchToken).ConfigureAwait(false);

        // Slot dequeue is safe here: semaphore ensures at most slotCount concurrent holders.
        workerSlots.TryDequeue(out var workerId);
        var capturedStep = step;
        var task = Task.Run(
          async () =>
          {
            try
            {
              var startOrdinal = Interlocked.Increment(ref dispatchedCount);
              _logger?.LogInformation(
                "  → {StepName} executing... ({StartOrdinal} of {Total} steps, worker {WorkerId}/{TotalWorkers})",
                capturedStep.Label,
                startOrdinal,
                totalSteps,
                workerId,
                slotCount
              );

              var result = await _executeStep(capturedStep, dispatchToken).ConfigureAwait(false);
              results[capturedStep.Label] = result;
              var completedCount = results.Count;

              if (!result.Success)
              {
                _logger?.LogWarning(
                  "  ✗ {StepName} failed ({CompletedCount} of {Total} steps, worker {WorkerId}/{TotalWorkers})",
                  capturedStep.Label,
                  completedCount,
                  totalSteps,
                  workerId,
                  slotCount
                );

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
                _logger?.LogInformation(
                  "  ✓ {StepName,-40} {Duration,6:F2}s   ({InputCount,6} → {OutputCount,6} records)   ({CompletedCount} of {Total} steps, worker {WorkerId}/{TotalWorkers})",
                  capturedStep.Label,
                  result.ExecutionTime.TotalSeconds,
                  result.InputCount,
                  result.OutputCount,
                  completedCount,
                  totalSteps,
                  workerId,
                  slotCount
                );

                // Notify dependents — decrement their pending count.
                EnqueueReadyDependents(capturedStep, pendingDeps, dependents, skipped, readyBag);
              }
            }
            finally
            {
              workerSlots.Enqueue(workerId);
              semaphore.Release();
            }
          },
          dispatchToken
        );

        inFlight.Add(task);
      }

      if (inFlight.Count == 0 && readyBag.IsEmpty)
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

      // External cancellation — break immediately; ThrowIfCancellationRequested below
      // will surface the OperationCanceledException to the caller.
      if (cancellationToken.IsCancellationRequested)
      {
        break;
      }

      // Internal stop-on-first-error cancellation — drain remaining in-flight tasks
      // (they may still be running; let them finish before we return) then break.
      if (dispatchToken.IsCancellationRequested)
      {
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
    IReadOnlyDictionary<FlowStep, IReadOnlyList<FlowStep>> dependents,
    HashSet<FlowStep> skipped,
    ConcurrentBag<FlowStep> readyBag
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
        readyBag.Add(dependent);
      }
    }
  }

  private static void SkipDownstream(
    FlowStep failedStep,
    HashSet<FlowStep> skipped,
    IReadOnlyDictionary<FlowStep, IReadOnlyList<FlowStep>> dependents
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
