using Flowthru.Core.Abstractions;

namespace SpaceflightsDistributed.Reporting.Data._08_Reporting.Schemas;

[FlowthruSchema]
public partial record ShuttleCapacityReport
{
  [SerializedLabel("shuttle_type")]
  public required string ShuttleType { get; init; }

  [SerializedLabel("avg_passenger_capacity")]
  public required decimal AvgPassengerCapacity { get; init; }
}
