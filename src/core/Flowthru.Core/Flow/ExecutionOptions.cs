using Flowthru.Caching;

namespace Flowthru.Flow;

/// <summary>
/// Knobs that change how a <see cref="BuiltFlow"/> runs without
/// changing the graph itself. Held in a record so the call site reads
/// declaratively (<c>new ExecutionOptions { DryRun = DryRunOption.On }</c>).
/// </summary>
public sealed record ExecutionOptions
{
  /// <summary>Whether to skip transform execution. Default: <see cref="DryRunOption.Off"/>.</summary>
  public DryRunOption DryRun { get; init; } = DryRunOption.Off;

  /// <summary>How thorough pre-flight inspection should be. Default: <see cref="ValidationDepth.Shallow"/>.</summary>
  public ValidationDepth ValidationDepth { get; init; } = ValidationDepth.Shallow;

  /// <summary>
  /// If true (default), the engine stops at the first failed step.
  /// If false, the engine continues running independent steps after a
  /// failure and returns a <see cref="FlowResult"/> whose
  /// <see cref="FlowResult.StepResults"/> records every per-step
  /// outcome. Used for CI test suites where seeing all failures in
  /// one pass is more useful than fail-fast.
  /// </summary>
  public bool StopOnFirstError { get; init; } = true;

  /// <summary>
  /// Maximum number of steps that may run concurrently within a
  /// topological layer. Default <c>1</c> (sequential, deterministic
  /// order); raise to N to allow up to N steps in flight at once.
  /// Steps with cross-dependencies still respect topological
  /// ordering — the knob only governs intra-layer concurrency.
  /// Honoured by <c>ParallelFlowScheduler</c>; alternative
  /// schedulers may use it differently or ignore it.
  /// </summary>
  public int Parallelism { get; init; } = 1;

  /// <summary>
  /// Pre-flight-computed cache plan, threaded into the scheduler so it
  /// can short-circuit steps the plan marks fresh. Null when caching
  /// is disabled, when no <c>UseCacheStorage</c> registration exists, or
  /// when pre-flight was skipped. The scheduler treats a null plan
  /// identically to <see cref="CachePlan.Empty"/> — run every step.
  /// Framework-set; user code typically does not assign this directly.
  /// </summary>
  public CachePlan? CachePlan { get; init; }

  /// <summary>
  /// When true, suppress cache <i>reads</i> for this run — the framework
  /// skips building the cache plan, every cacheable step runs, and the
  /// scheduler short-circuits nothing. Cache <i>writes</i> still happen:
  /// successful steps update the manifest with their new composites so
  /// the next run benefits. Default: <c>false</c>.
  /// </summary>
  /// <remarks>
  /// The CLI flag is <c>--no-cache</c>. Useful for "rebuild fresh this
  /// once but populate the cache for next time" — e.g., when you suspect
  /// a cached output is stale and want to force a recompute without
  /// erasing the manifest.
  /// </remarks>
  public bool BypassCacheReads { get; init; } = false;

  /// <summary>The default — fail-fast, shallow validation, no dry run, sequential.</summary>
  public static ExecutionOptions Default { get; } = new();
}
