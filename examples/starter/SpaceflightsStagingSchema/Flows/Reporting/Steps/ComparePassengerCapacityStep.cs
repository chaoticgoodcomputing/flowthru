using Flowthru.Core.Steps;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;
using SpaceflightsStagingSchema.Data._08_Reporting.Schemas;

namespace SpaceflightsStagingSchema.Flows.Reporting.Steps;

/// <summary>
/// Aggregates passenger capacity by shuttle type from the production shuttle
/// table — the canonical source for shuttle metadata. Includes shuttles that
/// were never reviewed (and therefore aren't in the model input table).
/// </summary>
[FlowthruStep]
public static class ComparePassengerCapacityStep
{
  public static Func<
    IEnumerable<PreprocessedShuttleSchema>,
    IEnumerable<ShuttleCapacityReport>
  > Create()
  {
    return (input) =>
      input
        .GroupBy(s => s.ShuttleType)
        .Select(g => new ShuttleCapacityReport
        {
          ShuttleType = g.Key,
          AvgPassengerCapacity = (decimal)g.Average(s => s.PassengerCapacity),
        });
  }
}
