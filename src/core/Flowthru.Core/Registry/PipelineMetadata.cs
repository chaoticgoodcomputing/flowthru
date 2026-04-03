using Flowthru.Flows.Validation;

namespace Flowthru.Registry;

/// <summary>
/// Metadata describing a registered flow.
/// </summary>
/// <remarks>
/// Used internally by the flow registry to store Flow information
/// beyond just the Flow instance itself.
/// </remarks>
internal class FlowRegistration
{
  /// <summary>
  /// Flow name (unique identifier).
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Optional human-readable description of what the Flow does.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Validation configuration for this flow.
  /// </summary>
  public ValidationOptions ValidationOptions { get; set; } = ValidationOptions.Default();
}
