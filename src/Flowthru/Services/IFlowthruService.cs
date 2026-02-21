using Flowthru.Data;
using Flowthru.Data.Validation;
using Flowthru.Pipelines;
using Flowthru.Services.Models;

namespace Flowthru.Services;

/// <summary>
/// Core service for executing Flowthru pipelines programmatically.
/// </summary>
/// <remarks>
/// <para>
/// This service is DI-injectable and CLI-agnostic, enabling use in:
/// - Console applications (via shallow CLI wrapper)
/// - ASP.NET Core applications (controller/background service injection)
/// - Azure Functions (function injection)
/// - Unit tests (with mocked dependencies)
/// </para>
/// <para>
/// <strong>Usage Example:</strong>
/// <code>
/// public class DataProcessingService
/// {
///     private readonly IFlowthruService _flowthru;
///
///     public DataProcessingService(IFlowthruService flowthru)
///     {
///         _flowthru = flowthru;
///     }
///
///     public async Task ProcessData()
///     {
///         var request = new PipelineExecutionRequest
///         {
///             PipelineName = "data_processing",
///             Options = new ExecutionOptions { DryRun = false }
///         };
///
///         var result = await _flowthru.ExecutePipelineAsync(request);
///
///         if (result.Success)
///         {
///             Console.WriteLine($"Processed {result.NodeResults.Count} nodes");
///         }
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public interface IFlowthruService
{
  /// <summary>
  /// Gets the names of all registered pipelines.
  /// </summary>
  IReadOnlyCollection<string> PipelineNames { get; }

  /// <summary>
  /// Gets the catalog instance.
  /// </summary>
  DataCatalogBase Catalog { get; }

  /// <summary>
  /// Executes a specific pipeline by name.
  /// </summary>
  /// <param name="request">Execution configuration</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Execution result with timing, node results, and status</returns>
  /// <exception cref="KeyNotFoundException">Thrown if pipeline name not found</exception>
  /// <remarks>
  /// This method performs:
  /// 1. Pipeline retrieval and validation
  /// 2. Service injection
  /// 3. DAG building and analysis
  /// 4. Metadata export (if requested)
  /// 5. External input validation
  /// 6. Pipeline execution (unless dry run)
  /// 7. Result formatting
  /// </remarks>
  Task<PipelineResult> ExecutePipelineAsync(
    PipelineExecutionRequest request,
    CancellationToken cancellationToken = default
  );

  /// <summary>
  /// Executes all registered pipelines in dependency order.
  /// </summary>
  /// <param name="options">Execution options</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Execution result for merged pipeline</returns>
  /// <remarks>
  /// Merges all pipelines into a single DAG and executes them.
  /// Useful for running entire data processing workflows.
  /// </remarks>
  Task<PipelineResult> ExecuteAllPipelinesAsync(
    ExecutionOptions? options = null,
    CancellationToken cancellationToken = default
  );

  /// <summary>
  /// Gets metadata about a pipeline's structure.
  /// </summary>
  /// <param name="pipelineName">Pipeline name</param>
  /// <returns>Pipeline metadata</returns>
  /// <exception cref="KeyNotFoundException">Thrown if pipeline name not found</exception>
  /// <remarks>
  /// Returns structural information without executing the pipeline.
  /// The pipeline must be built for accurate layer and input information.
  /// </remarks>
  PipelineMetadata GetPipelineMetadata(string pipelineName);

  /// <summary>
  /// Validates all external inputs (Layer 0) for a pipeline.
  /// </summary>
  /// <param name="pipelineName">Pipeline name</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Validation result</returns>
  /// <exception cref="KeyNotFoundException">Thrown if pipeline name not found</exception>
  /// <remarks>
  /// Checks accessibility of external data sources without executing the pipeline.
  /// Useful for pre-flight validation in CI/CD or scheduled jobs.
  /// </remarks>
  Task<ValidationResult> ValidatePipelineAsync(
    string pipelineName,
    CancellationToken cancellationToken = default
  );
}
