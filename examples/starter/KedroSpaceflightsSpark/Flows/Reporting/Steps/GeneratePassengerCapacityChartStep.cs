using Flowthru.Core.Steps;
using Flowthru.DataFrames;
using KedroSpaceflightsSpark.Data._02_Intermediate.Schemas;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace KedroSpaceflightsSpark.Flows.Reporting.Steps;

/// <summary>
/// Generates a bar chart comparing average passenger capacity by shuttle type.
/// Receives a TypedFrame and enumerates it (triggering Spark materialization) to
/// produce the aggregated data needed by Plotly.NET.
/// </summary>
[FlowthruStep]
public static class GeneratePassengerCapacityChartStep
{
  public static Func<TypedFrame<PreprocessedShuttleSchema>, GenericChart> Create(
    ILogger? logger = null
  )
  {
    return (input) =>
    {
      var shuttles = input.ToList();

      logger?.LogInformation(
        "Generating passenger capacity chart from {Count} shuttle records",
        shuttles.Count
      );

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

      return CSharpChart
        .Column<string, double, double>(shuttleTypes, capacities)
        .WithXAxisStyle(Title.init("Shuttle Type (Ranked by Capacity)"))
        .WithYAxisStyle(Title.init("Average Passenger Capacity"))
        .WithTitle("Shuttle Passenger Capacity Rankings")
        .WithSize(1000, 600);
    };
  }
}
