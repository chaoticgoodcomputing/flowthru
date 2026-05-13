using Flowthru.Step;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace SpaceflightsHybridCatalog.Flows.Reporting.Steps;

/// <summary>
/// Generates a bar chart visualization comparing average passenger capacity by shuttle type.
/// </summary>
[FlowthruStep]
public static class GeneratePassengerCapacityChartStep
{
  public static Func<IEnumerable<PreprocessedShuttleSchema>, GenericChart> Create(
    ILogger? logger = null
  )
  {
    return input =>
    {
      var shuttles = input.ToList();

      var aggregated = shuttles
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

      var chart = CSharpChart
        .Column<string, double, double>(shuttleTypes, capacities)
        .WithXAxisStyle(Title.init("Shuttle Type (Ranked by Capacity)"))
        .WithYAxisStyle(Title.init("Average Passenger Capacity"))
        .WithTitle("Shuttle Passenger Capacity Rankings")
        .WithSize(1000, 600);

      logger?.LogInformation("Generated passenger capacity bar chart with {Count} categories", aggregated.Count);

      return chart;
    };
  }
}
