using Flowthru.Results;

namespace Flowthru.Application;

/// <summary>
/// Configuration options for pipeline execution.
/// </summary>
/// <remarks>
/// Controls how pipelines are executed and how results are presented.
/// </remarks>
public class ExecutionOptions {
  /// <summary>
  /// Whether to stop execution on the first node failure.
  /// </summary>
  /// <remarks>
  /// When true (default), pipeline execution stops immediately when a node fails.
  /// When false, execution continues to independent nodes (Phase 2 feature).
  /// </remarks>
  public bool StopOnFirstError { get; set; } = true;

  /// <summary>
  /// Whether to enable parallel execution of nodes within the same layer.
  /// </summary>
  /// <remarks>
  /// Phase 2 feature - currently not implemented.
  /// When true, nodes in the same execution layer run concurrently.
  /// </remarks>
  public bool EnableParallelExecution { get; set; } = false;

  /// <summary>
  /// The result formatter to use for displaying execution results.
  /// </summary>
  /// <remarks>
  /// Defaults to ConsoleResultFormatter if not specified.
  /// </remarks>
  public IPipelineResultFormatter? ResultFormatter { get; set; }

  /// <summary>
  /// Gets the configured formatter or creates a default one.
  /// </summary>
  /// <returns>The result formatter to use</returns>
  internal IPipelineResultFormatter GetFormatter() {
    return ResultFormatter ?? new ConsoleResultFormatter();
  }
}
