using Flowthru.Pipelines;

namespace Flowthru.Services.Models;

/// <summary>
/// Request model for pipeline execution.
/// </summary>
/// <remarks>
/// Encapsulates all configuration needed to execute a pipeline programmatically,
/// separate from CLI argument parsing.
/// </remarks>
public record PipelineExecutionRequest
{
  /// <summary>
  /// Name of the pipeline to execute.
  /// </summary>
  public required string PipelineName { get; init; }

  /// <summary>
  /// Execution options (dry run, parallel execution, etc.).
  /// </summary>
  /// <remarks>
  /// If null, uses default execution options.
  /// </remarks>
  public ExecutionOptions? Options { get; init; }

  /// <summary>
  /// Whether to export DAG metadata.
  /// </summary>
  /// <remarks>
  /// Defaults to true. Only applies if a metadata builder is configured.
  /// </remarks>
  public bool ExportMetadata { get; init; } = true;

  /// <summary>
  /// Output directory for metadata (if null, uses default from metadata builder).
  /// </summary>
  public string? MetadataOutputDirectory { get; init; }
}
