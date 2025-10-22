using Flowthru.Pipelines.Validation;

namespace Flowthru.Registry;

/// <summary>
/// Metadata describing a registered pipeline.
/// </summary>
/// <remarks>
/// Used internally by the pipeline registry to store pipeline information
/// beyond just the pipeline instance itself.
/// </remarks>
internal class PipelineMetadata {
  /// <summary>
  /// Pipeline name (unique identifier).
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Optional human-readable description of what the pipeline does.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Tags for categorizing and filtering pipelines.
  /// </summary>
  public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

  /// <summary>
  /// Validation configuration for this pipeline.
  /// </summary>
  public ValidationOptions ValidationOptions { get; set; } = ValidationOptions.Default();
}
