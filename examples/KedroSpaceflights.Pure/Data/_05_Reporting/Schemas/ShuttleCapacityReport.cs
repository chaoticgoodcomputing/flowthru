using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._05_Reporting.Schemas;

/// <summary>
/// Represents a passenger capacity summary report grouped by shuttle type.
/// </summary>
public record ShuttleCapacityReport : IFlatSchema, IStructuredSerializable
{
  /// <summary>
  /// Type or model of the shuttle.
  /// </summary>
  [SerializedLabel("shuttle_type")]
  public string ShuttleType { get; init; } = null!;

  /// <summary>
  /// Average passenger capacity for this shuttle type.
  /// </summary>
  [SerializedLabel("avg_passenger_capacity")]
  public decimal AvgPassengerCapacity { get; init; }
}
