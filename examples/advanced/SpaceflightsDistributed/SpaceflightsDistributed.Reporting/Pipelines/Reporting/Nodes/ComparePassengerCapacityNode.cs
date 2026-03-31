using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;
using SpaceflightsDistributed.Reporting.Data._08_Reporting.Schemas;

namespace SpaceflightsDistributed.Reporting.Pipelines.Reporting.Nodes;

/// <summary>
/// Aggregates shuttle passenger capacity data by shuttle type.
/// </summary>
public static class ComparePassengerCapacityNode
{
  public static Func<
    IEnumerable<PreprocessedShuttleSchema>,
    IEnumerable<ShuttleCapacityReport>
  > Create()
  {
    return (input) =>
    {
      return input
        .GroupBy(s => s.ShuttleType)
        .Select(g => new ShuttleCapacityReport
        {
          ShuttleType = g.Key,
          AvgPassengerCapacity = (decimal)g.Average(s => s.PassengerCapacity),
        });
    };
  }
}
