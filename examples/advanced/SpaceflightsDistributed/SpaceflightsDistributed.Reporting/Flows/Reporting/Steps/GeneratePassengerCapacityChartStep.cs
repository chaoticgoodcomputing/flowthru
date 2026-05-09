using Flowthru.Step;

using Plotly.NET;
using Plotly.NET.LayoutObjects;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace SpaceflightsDistributed.Reporting.Flows.Reporting.Steps;

/// <summary>
/// Generates a bar chart comparing average passenger capacity by shuttle type.
/// </summary>
[FlowthruStep]
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

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="GeneratePassengerCapacityChartStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static PreprocessedShuttleSchema MakeShuttle(string type, int capacity) =>
      new()
      {
        Id = System.Guid.NewGuid().ToString(),
        ShuttleType = type,
        CompanyId = "c1",
        Engines = 2,
        PassengerCapacity = capacity,
        Crew = 4,
        Price = 500m,
        DCheckComplete = true,
        MoonClearanceComplete = false,
      };

    [StepTest(typeof(GeneratePassengerCapacityChartStep))]
    public void ValidInput_ProducesChart()
    {
      var input = Samples.Of(MakeShuttle("TypeA", 100), MakeShuttle("TypeB", 200));

      var chart = Invoke(Create(), input);

      Assert.That(chart, Is.Not.Null);
    }

    [StepTest(typeof(GeneratePassengerCapacityChartStep))]
    public void SingleShuttleType_ProducesChart()
    {
      var input = Samples.Of(MakeShuttle("TypeA", 150));

      var chart = Invoke(Create(), input);

      Assert.That(chart, Is.Not.Null);
    }
  }
#endif
}
