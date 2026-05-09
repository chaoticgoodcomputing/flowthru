using Flowthru.Step;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace SpaceflightsStagingSchema.Flows.Reporting.Steps;

/// <summary>
/// Generates a bar chart of average passenger capacity by shuttle type from
/// the production shuttle table.
/// </summary>
[FlowthruStep]
public static class GeneratePassengerCapacityChartStep
{
  public static Func<IEnumerable<PreprocessedShuttleSchema>, GenericChart> Create(
    ILogger? logger = null
  )
  {
    return (input) =>
    {
      var rows = input.ToList();

      logger?.LogInformation(
        "Generating passenger capacity chart from {Count} shuttle records",
        rows.Count
      );

      var aggregated = rows
        .GroupBy(s => s.ShuttleType)
        .Select(g => new
        {
          ShuttleType = g.Key,
          AvgPassengerCapacity = g.Average(s => s.PassengerCapacity),
        })
        .OrderByDescending(x => x.AvgPassengerCapacity)
        .ToList();

      var shuttleTypes = aggregated.Select(x => x.ShuttleType).ToList();
      var capacities = aggregated.Select(x => (double)x.AvgPassengerCapacity).ToList();

      return CSharpChart
        .Column<string, double, double>(shuttleTypes, capacities)
        .WithXAxisStyle(Title.init("Shuttle Type (Ranked by Capacity)"))
        .WithYAxisStyle(Title.init("Average Passenger Capacity"))
        .WithTitle("Shuttle Passenger Capacity Rankings")
        .WithSize(1000, 600);
    };
  }
}
