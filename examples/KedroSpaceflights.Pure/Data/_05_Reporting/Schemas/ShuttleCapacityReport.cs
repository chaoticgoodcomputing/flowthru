using Flowthru.Abstractions;

namespace KedroSpaceflights.Pure.Data._05_Reporting.Schemas;

public record ShuttleCapacityReport : IFlatSchema, IStructuredSerializable
{
  [SerializedLabel("shuttle_type")]
  public string ShuttleType { get; init; } = null!;

  [SerializedLabel("avg_passenger_capacity")]
  public decimal AvgPassengerCapacity { get; init; }
}
