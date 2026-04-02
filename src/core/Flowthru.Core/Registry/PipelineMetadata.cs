using Flowthru.Flows.Validation;

namespace Flowthru.Registry;

/// <summary>
/// Metadata describing a registered flow.
/// </summary>
/// <remarks>
/// Used internally by the flow registry to store flow information
/// beyond just the flow instance itself.
/// </remarks>
internal class FlowRegistration
{
  /// <summary>
  /// Flow name (unique identifier).
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Optional human-readable description of what the flow does.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Validation configuration for this flow.
  /// </summary>
  public ValidationOptions ValidationOptions { get; set; } = ValidationOptions.Default();
}
