namespace Flowthru.Services.Models;

/// <summary>
/// Metadata about a pipeline's structure and configuration.
/// </summary>
/// <remarks>
/// Provides read-only information about a pipeline without executing it.
/// Useful for discovery, validation, and UI generation.
/// </remarks>
public sealed record PipelineMetadata
{
  /// <summary>
  /// The pipeline's registered name.
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Optional description of the pipeline's purpose.
  /// </summary>
  public string? Description { get; init; }

  /// <summary>
  /// Tags associated with the pipeline.
  /// </summary>
  public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

  /// <summary>
  /// Total number of nodes in the pipeline.
  /// </summary>
  public required int NodeCount { get; init; }

  /// <summary>
  /// Number of execution layers in the pipeline's DAG.
  /// </summary>
  public required int LayerCount { get; init; }

  /// <summary>
  /// Labels of external data sources (Layer 0 inputs).
  /// </summary>
  public required IReadOnlyList<string> ExternalInputs { get; init; }

  /// <summary>
  /// Whether the pipeline has been built (DAG analyzed).
  /// </summary>
  public required bool IsBuilt { get; init; }
}
