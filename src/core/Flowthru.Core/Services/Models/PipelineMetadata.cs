namespace Flowthru.Services.Models;

/// <summary>
/// Metadata about a flow's structure and configuration.
/// </summary>
/// <remarks>
/// Provides read-only information about a flow without executing it.
/// Useful for discovery, validation, and UI generation.
/// </remarks>
public sealed record FlowMetadata
{
  /// <summary>
  /// The flow's registered name.
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Optional description of the flow's purpose.
  /// </summary>
  public string? Description { get; init; }

  /// <summary>
  /// Total number of steps in the flow.
  /// </summary>
  public required int StepCount { get; init; }

  /// <summary>
  /// Number of execution layers in the flow's DAG.
  /// </summary>
  public required int LayerCount { get; init; }

  /// <summary>
  /// Labels of external data sources (Layer 0 inputs).
  /// </summary>
  public required IReadOnlyList<string> ExternalInputs { get; init; }

  /// <summary>
  /// Whether the flow has been built (DAG analyzed).
  /// </summary>
  public required bool IsBuilt { get; init; }
}
