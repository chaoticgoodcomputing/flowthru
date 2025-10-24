using Flowthru.Nodes;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.Reporting.Nodes;

/// <summary>
/// Generates a bar chart comparing average passenger capacity by shuttle type.
/// Matches Kedro spaceflights reporting pipeline's compare_passenger_capacity function.
/// </summary>
/// <remarks>
/// <para>
/// This node aggregates preprocessed shuttle data by shuttle type and calculates the mean
/// passenger capacity for each type. The result is visualized as a bar chart using
/// Plotly.NET, matching the structure of Kedro's plotly.express-based visualization.
/// </para>
/// <para>
/// <strong>Input:</strong> Preprocessed shuttle data (CleanedShuttles catalog entry)
/// </para>
/// <para>
/// <strong>Output:</strong> GenericChart object stored in memory for downstream processing
/// </para>
/// <para>
/// <strong>Architecture:</strong> This node focuses purely on chart generation logic.
/// Serialization to JSON or image export is handled by downstream nodes, enabling
/// separation of concerns and reusable export pipelines.
/// </para>
/// </remarks>
public class ComparePassengerCapacityNode : NodeBase<ShuttleSchema, GenericChart> {
  protected override Task<IEnumerable<GenericChart>> Transform(IEnumerable<ShuttleSchema> input) {
    // Aggregate by shuttle type and calculate mean passenger capacity
    var aggregated = input
        .GroupBy(s => s.ShuttleType)
        .Select(g => new {
          ShuttleType = g.Key,
          AvgCapacity = g.Average(s => (double)s.PassengerCapacity)
        })
        .OrderByDescending(x => x.AvgCapacity)
        .ToList();

    Logger?.LogInformation(
        "Aggregated passenger capacity for {Count} shuttle types",
        aggregated.Count);

    // Extract data for chart
    var shuttleTypes = aggregated.Select(x => x.ShuttleType).ToList();
    var capacities = aggregated.Select(x => x.AvgCapacity).ToList();

    // Create column/bar chart using Plotly.NET.CSharp API
    // Positional parameters: x values (keys), y values (heights)
    var chart = CSharpChart.Column<string, double, double>(
        shuttleTypes,
        capacities
    )
    .WithXAxisStyle(Title.init("Shuttle Type"))
    .WithYAxisStyle(Title.init("Average Passenger Capacity"))
    .WithTitle("Shuttle Passenger Capacity by Type");

    Logger?.LogInformation(
        "Generated GenericChart for passenger capacity comparison with {Count} shuttle types",
        shuttleTypes.Count);

    // Return as singleton (single chart object)
    return Task.FromResult<IEnumerable<GenericChart>>(new[] { chart });
  }
}
