using Flowthru.Abstractions;

namespace Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;

/// <summary>
/// Schema for synthetic test data generated for diagnostic purposes.
/// Contains a single floating-point value sampled from a normal distribution.
/// </summary>
/// <remarks>
/// Used to demonstrate no-input nodes that generate data without external sources.
/// </remarks>
public record SyntheticDataPoint : IFlatSerializable {
  /// <summary>
  /// Sequential index of the data point (0-based)
  /// </summary>
  public int Index { get; init; }

  /// <summary>
  /// Random value sampled from a normal distribution
  /// </summary>
  public float Value { get; init; }
}
