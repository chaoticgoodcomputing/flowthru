using Flowthru.Step;
using Flowthru.Step.Testing;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;
using SpaceflightsDistributed.Reporting.Data._08_Reporting.Schemas;

namespace SpaceflightsDistributed.Reporting.Flows.Reporting.Steps;

/// <summary>
/// Aggregates shuttle passenger capacity data by shuttle type.
/// </summary>
[FlowthruStep]
public static class ComparePassengerCapacityStep
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

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="ComparePassengerCapacityStep"/>.</summary>
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

    [FUnitStepTest(typeof(ComparePassengerCapacityStep))]
    public void GroupsByShuttleType()
    {
      var input = new[]
      {
        MakeShuttle("TypeA", 100),
        MakeShuttle("TypeA", 200),
        MakeShuttle("TypeB", 50),
      };

      var result = Invoke(Create(), input).ToList();

      Assert.That(result, Has.Count.EqualTo(2));
      Assert.That(
        result.Single(r => r.ShuttleType == "TypeA").AvgPassengerCapacity,
        Is.EqualTo(150m)
      );
      Assert.That(
        result.Single(r => r.ShuttleType == "TypeB").AvgPassengerCapacity,
        Is.EqualTo(50m)
      );
    }

    [FUnitStepTest(typeof(ComparePassengerCapacityStep))]
    public void EmptyInput_ReturnsEmpty()
    {
      var result = Invoke(Create(), Enumerable.Empty<PreprocessedShuttleSchema>()).ToList();

      Assert.That(result, Is.Empty);
    }
  }
#endif
}
