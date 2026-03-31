using Flowthru.Data;
using Flowthru.Data.Validation;
using Flowthru.Meta.Models;
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
///         // Execute with optional slicing
///         var options = new ExecutionOptions
///         {
///             DryRun = false,
///             SliceStrategy = new PipelineSliceStrategy
///             {
///                 Pipelines = new HashSet&lt;string&gt; { "data_processing" }
///             }
///         };
///
///         var result = await _flowthru.ExecutePipelineAsync(options);
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
  /// Gets all registered catalog instances.
  /// </summary>
  IReadOnlyList<DataCatalogBase> Catalogs { get; }

  /// <summary>
  /// Executes all registered pipelines, optionally sliced by criteria.
  /// </summary>
  /// <param name="options">Execution options with optional slice strategy</param>
  /// <param name="exportMetadata">Whether to export DAG metadata</param>
  /// <param name="metadataOutputDirectory">Override for metadata output directory</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Execution result with timing, node results, and status</returns>
  /// <remarks>
  /// This method always merges all registered pipelines into a single DAG,
  /// then applies optional slicing criteria from the execution options.
  /// This enables cross-pipeline queries (e.g., --to-data across all pipelines).
  /// To execute only specific pipelines, use SliceStrategy.Pipelines.
  ///
  /// The method performs:
  /// 1. Pipeline merging into unified DAG
  /// 2. Service injection
  /// 3. DAG building and slice application
  /// 4. Metadata export (if requested)
  /// 5. External input validation
  /// 6. Pipeline execution (unless dry run)
  /// 7. Result formatting
  /// </remarks>
  Task<PipelineResult> ExecutePipelineAsync(
    ExecutionOptions? options = null,
    bool exportMetadata = true,
    string? metadataOutputDirectory = null,
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
  /// Gets the full DAG metadata for pipeline introspection.
  /// </summary>
  /// <param name="pipelineName">
  /// Optional pipeline name to inspect a single pipeline.
  /// When null, all registered pipelines are merged into a unified DAG.
  /// </param>
  /// <param name="sliceStrategy">
  /// Optional slice strategy to filter the DAG (e.g., from-node, to-data).
  /// When provided, the returned metadata includes slice overlay information
  /// (SlicedNodeIds and SlicedCatalogEntryKeys) identifying which nodes
  /// and data are in the active execution subset.
  /// </param>
  /// <returns>
  /// Full DAG metadata including nodes, catalog entries, edges, schemas,
  /// and producer-consumer relationships.
  /// </returns>
  /// <exception cref="KeyNotFoundException">
  /// Thrown if <paramref name="pipelineName"/> is specified but not found.
  /// </exception>
  /// <remarks>
  /// This method does not execute the pipeline. It returns structural metadata
  /// useful for visualization, impact analysis, data lineage, and debugging.
  ///
  /// Examples:
  /// <code>
  /// // Inspect all pipelines merged
  /// var dag = flowthru.GetDagMetadata();
  ///
  /// // Inspect a single pipeline
  /// var dag = flowthru.GetDagMetadata("DataProcessing");
  ///
  /// // Inspect downstream of a specific node
  /// var dag = flowthru.GetDagMetadata(sliceStrategy: new PipelineSliceStrategy
  /// {
  ///     FromNodes = new HashSet&lt;string&gt; { "PreprocessCompanies" }
  /// });
  /// </code>
  /// </remarks>
  DagMetadata GetDagMetadata(
    string? pipelineName = null,
    PipelineSliceStrategy? sliceStrategy = null
  );

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
