using Flowthru.Data.Schema;

namespace SpaceflightsHybridCatalog.Data._08_Reporting.Schemas;

/// <summary>Passenger capacity summary report grouped by shuttle type.</summary>
[FlowthruSchema]
public partial record ShuttleCapacityReport
{
  [SerializedLabel("shuttle_type")]
  public required string ShuttleType { get; init; }

  [SerializedLabel("avg_passenger_capacity")]
  public required decimal AvgPassengerCapacity { get; init; }
}
