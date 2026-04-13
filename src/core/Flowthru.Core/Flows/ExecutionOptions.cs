using Flowthru.Core.Graph;
using Flowthru.Core.Results;

namespace Flowthru.Core.Flows;

/// <summary>
/// Configuration options for pipeline execution.
/// </summary>
/// <remarks>
/// Controls how pipelines are executed and how results are presented.
/// </remarks>
public class ExecutionOptions
{
  /// <summary>
  /// Whether to perform a dry run, and at what validation depth.
  /// </summary>
  /// <remarks>
  /// Assign <c>true</c> to perform all pre-flight operations (structure validation,
  /// validation hooks, and external data source inspection) without executing nodes.
  /// Assign a <see cref="ValidationDepth"/> value to control how deeply the pre-flight
  /// checks run — for example, <see cref="ValidationDepth.StructureOnly"/> validates
  /// the pipeline graph and runs extension hooks without probing any data sources.
  /// Assign <c>false</c> (default) to run normally without a dry-run stop.
  /// </remarks>
  public DryRunOption DryRun { get; set; } = false;

  /// <summary>
  /// Whether to stop execution on the first node failure.
  /// </summary>
  /// <remarks>
  /// When true (default), pipeline execution stops immediately when a node fails.
  /// When false, execution continues to independent nodes (Phase 2 feature).
  /// </remarks>
  public bool StopOnFirstError { get; set; } = true;

  /// <summary>
  /// Maximum number of steps that may execute concurrently.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Controls the degree of parallelism in the task-graph scheduler. Steps whose
  /// dependencies are all satisfied are dispatched immediately, up to this limit.
  /// </para>
  /// <para>
  /// <list type="bullet">
  /// <item><c>1</c> (default) — sequential execution; one step at a time in topological order.</item>
  /// <item><c>N &gt; 1</c> — up to N independent steps run concurrently.</item>
  /// <item><c>-1</c> or <see cref="int.MaxValue"/> — unbounded parallelism; all ready steps
  /// are dispatched immediately.</item>
  /// </list>
  /// </para>
  /// </remarks>
  public int MaxDegreeOfParallelism { get; set; } = 1;

  /// <summary>
  /// The result formatter to use for displaying execution results.
  /// </summary>
  /// <remarks>
  /// Defaults to ConsoleResultFormatter if not specified.
  /// </remarks>
  public IFlowResultFormatter? ResultFormatter { get; set; }

  /// <summary>
  /// Optional slicing strategy to apply when executing pipelines.
  /// </summary>
  /// <remarks>
  /// When provided, only nodes matching the slice strategy will be executed.
  /// Used when slicing flags are provided without a specific pipeline name.
  /// </remarks>
  public FlowSliceStrategy? SliceStrategy { get; set; }

  /// <summary>
  /// Gets the configured formatter or creates a default one.
  /// </summary>
  /// <returns>The result formatter to use</returns>
  internal IFlowResultFormatter GetFormatter()
  {
    return ResultFormatter ?? new ConsoleResultFormatter();
  }
}
