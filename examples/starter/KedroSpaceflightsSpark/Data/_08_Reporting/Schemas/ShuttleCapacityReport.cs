using Flowthru.Core.Abstractions;

namespace KedroSpaceflightsSpark.Data._08_Reporting.Schemas;

/// <summary>
/// Passenger capacity summary grouped by shuttle type.
/// Uses double for AvgPassengerCapacity to match Spark's DoubleType output from GroupBy.Aggregate.
/// </summary>
[FlowthruSchema]
public partial record ShuttleCapacityReport
{
    [SerializedLabel("shuttle_type")]
    public required string ShuttleType { get; init; }

    [SerializedLabel("avg_passenger_capacity")]
    public required double AvgPassengerCapacity { get; init; }
}
