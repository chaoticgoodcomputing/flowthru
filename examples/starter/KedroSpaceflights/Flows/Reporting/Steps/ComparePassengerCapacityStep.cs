using Flowthru.Step;
using KedroSpaceflights.Data._02_Intermediate.Schemas;
using KedroSpaceflights.Data._08_Reporting.Schemas;

namespace KedroSpaceflights.Flows.Reporting.Steps;

/// <summary>
/// Aggregates shuttle passenger capacity data by shuttle type.
/// </summary>
[FlowthruStep]
public static class ComparePassengerCapacityStep
{
  /// <summary>
  /// Creates a function that computes average passenger capacity grouped by shuttle type.
  /// </summary>
  /// <returns>
  /// A function that produces <see cref="ShuttleCapacityReport"/> records showing
  /// average capacity for each shuttle type.
  /// </returns>
  public static Func<
    IEnumerable<PreprocessedShuttleSchema>,
    IEnumerable<ShuttleCapacityReport>
  > Create()
  {
    return (input) =>
    {
      var report = input
        .GroupBy(s => s.ShuttleType)
        .Select(g => new ShuttleCapacityReport
        {
          ShuttleType = g.Key,
          AvgPassengerCapacity = (decimal)g.Average(s => s.PassengerCapacity),
        });

      return report;
    };
  }
}
