using Flowthru.Step;
using Flowthru.Step.Testing;
using KedroSpaceflightsFUnit.Data._02_Intermediate.Schemas;
using KedroSpaceflightsFUnit.Data._08_Reporting.Schemas;

namespace KedroSpaceflightsFUnit.Flows.Reporting.Steps;

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

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="ComparePassengerCapacityStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static PreprocessedShuttleSchema Shuttle(string type, int capacity) =>
      new()
      {
        Id = Guid.NewGuid().ToString(),
        ShuttleType = type,
        CompanyId = "C1",
        Engines = 2,
        PassengerCapacity = capacity,
        Crew = 4,
        Price = 500m,
        DCheckComplete = true,
        MoonClearanceComplete = false,
      };

    /// <summary>
    /// Single shuttle type should produce one group with the correct average.
    /// </summary>
    [FUnitStepTest(typeof(ComparePassengerCapacityStep))]
    public void SingleType_ProducesOneGroup()
    {
      // Arrange
      var input = Samples.Of(Shuttle("Type A", 100), Shuttle("Type A", 200));

      // Apply
      var result = Invoke(Create(), input).ToList();

      // Assert
      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].ShuttleType, Is.EqualTo("Type A"));
      Assert.That(result[0].AvgPassengerCapacity, Is.EqualTo(150m));
    }

    /// <summary>
    /// Two distinct shuttle types should produce two groups with correct averages.
    /// </summary>
    [FUnitStepTest(typeof(ComparePassengerCapacityStep))]
    public void TwoTypes_ProduceTwoGroups()
    {
      // Arrange
      var input = Samples.Of(Shuttle("Type A", 100), Shuttle("Type B", 300));

      // Apply
      var result = Invoke(Create(), input).OrderBy(r => r.ShuttleType).ToList();

      // Assert
      Assert.That(result, Has.Count.EqualTo(2));
      Assert.That(result[0].AvgPassengerCapacity, Is.EqualTo(100m));
      Assert.That(result[1].AvgPassengerCapacity, Is.EqualTo(300m));
    }
  }
#endif
}
