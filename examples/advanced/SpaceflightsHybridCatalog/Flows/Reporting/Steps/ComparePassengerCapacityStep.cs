using Flowthru.Step;
using SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;
using SpaceflightsHybridCatalog.Data._08_Reporting.Schemas;

namespace SpaceflightsHybridCatalog.Flows.Reporting.Steps;

/// <summary>
/// Aggregates shuttle passenger capacity data by shuttle type.
/// </summary>
[FlowthruStep]
public static class ComparePassengerCapacityStep
{
  public static Func<
    IEnumerable<PreprocessedShuttleSchema>,
    IEnumerable<ShuttleCapacityReport>
  > Create()
  {
    return input =>
      input
        .GroupBy(s => s.ShuttleType)
        .Select(g => new ShuttleCapacityReport
        {
          ShuttleType = g.Key,
          AvgPassengerCapacity = (decimal)g.Average(s => s.PassengerCapacity),
        });
  }
}
