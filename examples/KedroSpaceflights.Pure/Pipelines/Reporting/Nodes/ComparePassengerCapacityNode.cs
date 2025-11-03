using KedroSpaceflights.Pure.Data._02_Intermediate.Schemas;
using KedroSpaceflights.Pure.Data._05_Reporting.Schemas;

namespace KedroSpaceflights.Pure.Pipelines.Reporting.Nodes;

public static class ComparePassengerCapacityNode
{
  public static Func<
    IEnumerable<PreprocessedShuttleSchema>,
    Task<IEnumerable<ShuttleCapacityReport>>
  > Create()
  {
    return async (input) =>
    {
      var report = input
        .GroupBy(s => s.ShuttleType)
        .Select(g => new ShuttleCapacityReport
        {
          ShuttleType = g.Key,
          AvgPassengerCapacity = (decimal)g.Average(s => s.PassengerCapacity),
        });

      return await Task.FromResult(report);
    };
  }
}
