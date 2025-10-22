using Flowthru.Pipelines;

namespace Flowthru.Application;

/// <summary>
/// Interface for a Flowthru application that executes pipelines.
/// </summary>
/// <remarks>
/// This interface defines the contract for running pipelines. The primary
/// implementation is <see cref="FlowthruApplication"/>.
/// </remarks>
public interface IFlowthruApplication {
  /// <summary>
  /// Runs the application, selecting a pipeline based on command-line arguments.
  /// </summary>
  /// <returns>Exit code (0 = success, non-zero = failure)</returns>
  /// <remarks>
  /// <para>
  /// This is the main entry point for running pipelines. It:
  /// 1. Parses command-line arguments to select a pipeline
  /// 2. Executes the selected pipeline
  /// 3. Formats and displays results
  /// 4. Returns appropriate exit code
  /// </para>
  /// <para>
  /// If no pipeline name is provided in args, attempts to run all pipelines
  /// in dependency order (Phase 2 feature - currently throws NotImplementedException).
  /// </para>
  /// </remarks>
  Task<int> RunAsync();

  /// <summary>
  /// Runs the application with cancellation support.
  /// </summary>
  /// <param name="cancellationToken">Token to cancel execution</param>
  /// <returns>Exit code (0 = success, non-zero = failure)</returns>
  Task<int> RunAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Runs a specific pipeline by name.
  /// </summary>
  /// <param name="pipelineName">The name of the pipeline to run</param>
  /// <returns>The pipeline execution result</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the pipeline name is not registered</exception>
  Task<PipelineResult> RunPipelineAsync(string pipelineName);
}
