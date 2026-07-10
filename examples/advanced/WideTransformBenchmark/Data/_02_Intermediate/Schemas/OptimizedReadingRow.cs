using Flowthru.Data.Schema;

namespace WideTransformBenchmark.Data._02_Intermediate.Schemas;

/// <summary>
/// The optimize pass's output shape: one row per composite key
/// (<see cref="DeviceId"/>, <see cref="Channel"/>, <see cref="ObservedAt"/>),
/// sorted by that key, with the fabricated lineage columns pruned away. Both
/// the eager LINQ Step and the DuckDB engine transform declare this Schema, so
/// the framework verifies each path's output against the same contract.
/// </summary>
[FlowthruSchema]
public partial record OptimizedReadingRow
{
  /// <summary>Reporting device — composite-key part 1.</summary>
  public required string DeviceId { get; init; }

  /// <summary>Sensor channel — composite-key part 2.</summary>
  public required string Channel { get; init; }

  /// <summary>Observation timestamp — composite-key part 3.</summary>
  public required DateTime ObservedAt { get; init; }

  /// <summary>The measured value.</summary>
  public required double Reading { get; init; }

  /// <summary>Unit of measure.</summary>
  public required string Unit { get; init; }
}
