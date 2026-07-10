using Flowthru.Step;
using WideTransformBenchmark.Data._01_Raw.Schemas;
using WideTransformBenchmark.Data._04_Reporting.Schemas;
#if FUNIT_ENABLED
using Flowthru.Step.Testing;
#endif

namespace WideTransformBenchmark.Flows.Analyze.Steps;

/// <summary>
/// Pair the measurement rows into one comparison per dataset size: eager
/// wall-clock vs engine wall-clock, the speedup multiple, and the managed
/// allocation multiple. Pure arithmetic over the harness's staged facts.
/// </summary>
[FlowthruStep]
public static class BuildComparisonStep
{
  private const double BytesPerMb = 1024.0 * 1024.0;

  public static Func<IEnumerable<BenchmarkMeasurement>, IEnumerable<BenchmarkComparison>> Create()
  {
    return measurements => measurements
      .GroupBy(m => m.InputRows)
      .OrderBy(g => g.Key)
      .Select(g =>
      {
        var eager = g.Single(m =>
          string.Equals(m.TransformPath, "Eager", StringComparison.OrdinalIgnoreCase));
        var engine = g.Single(m =>
          string.Equals(m.TransformPath, "Engine", StringComparison.OrdinalIgnoreCase));

        return new BenchmarkComparison
        {
          InputRows = g.Key,
          OutputRows = eager.OutputRows,
          EagerMs = eager.ElapsedMs,
          EngineMs = engine.ElapsedMs,
          SpeedupX = Round2((double)eager.ElapsedMs / Math.Max(1, engine.ElapsedMs)),
          EagerAllocatedMb = Round2(eager.AllocatedBytes / BytesPerMb),
          EngineAllocatedMb = Round2(engine.AllocatedBytes / BytesPerMb),
          AllocationRatioX = Round2(
            (double)eager.AllocatedBytes / Math.Max(1, engine.AllocatedBytes)),
        };
      })
      .ToList();
  }

  private static double Round2(double value) => Math.Round(value, 2);

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="BuildComparisonStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static BenchmarkMeasurement Row(
      string path, int inputRows, long elapsedMs, long allocatedBytes
    ) =>
      new()
      {
        TransformPath = path,
        InputRows = inputRows,
        OutputRows = inputRows * 4 / 5,
        ElapsedMs = elapsedMs,
        AllocatedBytes = allocatedBytes,
      };

    [FUnitStepTest(typeof(BuildComparisonStep))]
    public void PairsPaths_ComputesSpeedupAndAllocationRatio()
    {
      var rows = new[]
      {
        Row("Eager", 10_000, elapsedMs: 100, allocatedBytes: 64 * 1024 * 1024),
        Row("Engine", 10_000, elapsedMs: 40, allocatedBytes: 8 * 1024 * 1024),
      };

      var result = Invoke(BuildComparisonStep.Create(), rows).Single();

      Assert.That(result.InputRows, Is.EqualTo(10_000));
      Assert.That(result.EagerMs, Is.EqualTo(100));
      Assert.That(result.EngineMs, Is.EqualTo(40));
      Assert.That(result.SpeedupX, Is.EqualTo(2.5));
      Assert.That(result.EagerAllocatedMb, Is.EqualTo(64.0));
      Assert.That(result.EngineAllocatedMb, Is.EqualTo(8.0));
      Assert.That(result.AllocationRatioX, Is.EqualTo(8.0));
    }

    [FUnitStepTest(typeof(BuildComparisonStep))]
    public void MultipleSizes_YieldOneRowEach_OrderedAscending()
    {
      var rows = new[]
      {
        Row("Engine", 40_000, 50, 1),
        Row("Eager", 10_000, 10, 1),
        Row("Eager", 40_000, 100, 1),
        Row("Engine", 10_000, 20, 1),
      };

      var result = Invoke(BuildComparisonStep.Create(), rows).ToList();

      Assert.That(result.Select(c => c.InputRows), Is.EqualTo(new[] { 10_000, 40_000 }));
      Assert.That(result[0].SpeedupX, Is.EqualTo(0.5)); // eager faster below the crossover
      Assert.That(result[1].SpeedupX, Is.EqualTo(2.0));
    }
  }
#endif
}
