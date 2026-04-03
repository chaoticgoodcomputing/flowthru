using Flowthru.Data;
using Flowthru.Data.Validation;
using Flowthru.Flows;
using Flowthru.Meta.Models;
using Flowthru.Services.Models;

namespace Flowthru.Services;

/// <summary>
/// Core service for executing Flowthru flows programmatically.
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
///             SliceStrategy = new FlowSliceStrategy
///             {
///                 Flows = new HashSet&lt;string&gt; { "data_processing" }
///             }
///         };
///
///         var result = await _flowthru.ExecuteFlowAsync(options);
///
///         if (result.Success)
///         {
///             Console.WriteLine($"Processed {result.StepResults.Count} flow");
///         }
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public interface IFlowthruService
{
  /// <summary>
  /// Gets the names of all registered flows.
  /// </summary>
  IReadOnlyCollection<string> FlowNames { get; }

  /// <summary>
  /// Gets all registered catalog instances.
  /// </summary>
  IReadOnlyList<CatalogAbstract> Catalogs { get; }

  /// <summary>
  /// Executes all registered flows, optionally sliced by criteria.
  /// </summary>
  /// <param name="options">Execution options with optional slice strategy</param>
  /// <param name="exportMetadata">Whether to export DAG metadata</param>
  /// <param name="metadataOutputDirectory">Override for metadata output directory</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Execution result with timing, step results, and status</returns>
  /// <remarks>
  /// This method always merges all registered flows into a single DAG,
  /// then applies optional slicing criteria from the execution options.
  /// This enables cross-flow queries (e.g., --to-data across all flows).
  /// To execute only specific flows, use SliceStrategy.Flows.
  ///
  /// The method performs:
  /// 1. Flow merging into unified DAG
  /// 2. Service injection
  /// 3. DAG building and slice application
  /// 4. Metadata export (if requested)
  /// 5. External input validation
  /// 6. Flow execution (unless dry run)
  /// 7. Result formatting
  /// </remarks>
  Task<FlowResult> ExecuteFlowAsync(
    ExecutionOptions? options = null,
    bool exportMetadata = true,
    string? metadataOutputDirectory = null,
    CancellationToken cancellationToken = default
  );

  /// <summary>
  /// Gets metadata about a Flow's structure.
  /// </summary>
  /// <param name="flowName">Flow name</param>
  /// <returns>Flow metadata</returns>
  /// <exception cref="KeyNotFoundException">Thrown if Flow name not found</exception>
  /// <remarks>
  /// Returns structural information without executing the flow.
  /// The Flow must be built for accurate layer and input information.
  /// </remarks>
  FlowMetadata GetFlowMetadata(string flowName);

  /// <summary>
  /// Gets the full DAG metadata for Flow introspection.
  /// </summary>
  /// <param name="flowName">
  /// Optional Flow name to inspect a single flow.
  /// When null, all registered flows are merged into a unified DAG.
  /// </param>
  /// <param name="sliceStrategy">
  /// Optional slice strategy to filter the DAG (e.g., from-node).
  /// When provided, the returned metadata includes slice overlay information
  /// (SlicedStepIds and SlicedItemKeys) identifying which nodes
  /// and data are in the active execution subset.
  /// </param>
  /// <returns>
  /// Full DAG metadata including steps, catalog entries, edges, schemas,
  /// and producer-consumer relationships.
  /// </returns>
  /// <exception cref="KeyNotFoundException">
  /// Thrown if <paramref name="flowName"/> is specified but not found.
  /// </exception>
  /// <remarks>
  /// This method does not execute the flow. It returns structural metadata
  /// useful for visualization, impact analysis, data lineage, and debugging.
  ///
  /// Examples:
  /// <code>
  /// // Inspect all Flow merged
  /// var dag = flowthru.GetDagMetadata();
  ///
  /// // Inspect a single flow
  /// var dag = flowthru.GetDagMetadata("DataProcessing");
  ///
  /// // Inspect downstream of a specific Flow node
  /// var dag = flowthru.GetDagMetadata(sliceStrategy: new FlowSliceStrategy
  /// {
  ///     FromSteps = new HashSet&lt;string&gt; { "PreprocessCompanies" }
  /// });
  /// </code>
  /// </remarks>
  DagMetadata GetDagMetadata(string? flowName = null, FlowSliceStrategy? sliceStrategy = null);

  /// <summary>
  /// Validates all external inputs (Layer 0) for a flow.
  /// </summary>
  /// <param name="flowName">Flow name</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Validation result</returns>
  /// <exception cref="KeyNotFoundException">Thrown if Flow name not found</exception>
  /// <remarks>
  /// Checks accessibility of external data sources without executing the flow.
  /// Useful for pre-flight validation in CI/CD or scheduled jobs.
  /// </remarks>
  Task<ValidationResult> ValidateFlowAsync(
    string flowName,
    CancellationToken cancellationToken = default
  );
}
