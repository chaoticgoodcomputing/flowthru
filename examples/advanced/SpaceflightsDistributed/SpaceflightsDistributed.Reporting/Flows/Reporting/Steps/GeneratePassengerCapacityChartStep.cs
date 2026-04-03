using Plotly.NET;
using Plotly.NET.LayoutObjects;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace SpaceflightsDistributed.Reporting.Flows.Reporting.Steps;

/// <summary>
/// Generates a bar chart comparing average passenger capacity by shuttle type.
/// </summary>
public static class GeneratePassengerCapacityChartStep
{
  public static Func<IEnumerable<PreprocessedShuttleSchema>, GenericChart> Create()
  {
    return (input) =>
    {
      var aggregated = input
        .GroupBy(s => s.ShuttleType)
        .Select(g => new
        {
          ShuttleType = g.Key,
          AvgPassengerCapacity = g.Average(s => s.PassengerCapacity),
        })
        .OrderByDescending(x => x.AvgPassengerCapacity)
        .ToList();

      var shuttleTypes = aggregated.Select(x => x.ShuttleType).ToList();
      var capacities = aggregated.Select(x => x.AvgPassengerCapacity).ToList();

      return CSharpChart
        .Column<string, double, double>(shuttleTypes, capacities)
        .WithXAxisStyle(Title.init("Shuttle Type (Ranked by Capacity)"))
        .WithYAxisStyle(Title.init("Average Passenger Capacity"))
        .WithTitle("Shuttle Passenger Capacity Rankings")
        .WithSize(1000, 600);
    };
  }
}
