using Flowthru.Core.Steps;
using Flowthru.Misc.DataFrames;
using KedroSpaceflightsSpark.Data._02_Intermediate.Schemas;
using KedroSpaceflightsSpark.Data._08_Reporting.Schemas;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace KedroSpaceflightsSpark.Flows.Reporting.Steps;

/// <summary>
/// Generates a bar chart comparing average passenger capacity by shuttle type.
///
/// The aggregation runs entirely in Spark: the 15k-row TypedFrame is filtered,
/// grouped, and aggregated before materialization. Only the small per-type summary
/// (~31 rows) is collected into .NET memory to feed Plotly.NET.
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
      var aggregated = input
        .Where(s => s.PassengerCapacity > 0)
        .GroupBy(s => s.ShuttleType)
        .Aggregate(ctx => new ShuttleCapacityReport
        {
          ShuttleType = ctx.Key,
          AvgPassengerCapacity = ctx.Avg(s => (double)s.PassengerCapacity),
        })
        .OrderByDescending(r => r.AvgPassengerCapacity)
        .ToList();

      logger?.LogInformation(
        "Generating passenger capacity chart from {Count} shuttle types",
        aggregated.Count
      );

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
