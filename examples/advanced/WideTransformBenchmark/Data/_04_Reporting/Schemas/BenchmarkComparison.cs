using Flowthru.Data.Schema;

namespace WideTransformBenchmark.Data._04_Reporting.Schemas;

/// <summary>
/// The per-size eager-vs-engine verdict, computed by
/// <c>BuildComparisonStep</c> from the paired <c>BenchmarkMeasurement</c>
/// rows: wall-clock for each path, the speedup multiple, managed allocations
/// for each path, and the allocation multiple. One row per fabricated dataset
/// size, written to <c>benchmark_summary.csv</c> and rendered into
/// <c>benchmark_report.md</c>.
/// </summary>
[FlowthruSchema]
public partial record BenchmarkComparison
{
  /// <summary>Fabricated input rows (pre-dedup).</summary>
  public required int InputRows { get; init; }

  /// <summary>Rows both paths produced (post-dedup).</summary>
  public required int OutputRows { get; init; }

  /// <summary>Eager LINQ path wall-clock, ms.</summary>
  public required long EagerMs { get; init; }

  /// <summary>DuckDB engine path wall-clock, ms.</summary>
  public required long EngineMs { get; init; }

  /// <summary>Eager over engine wall-clock — above 1.0 the engine is faster.</summary>
  public required double SpeedupX { get; init; }

  /// <summary>Managed bytes the eager path allocated, in MiB.</summary>
  public required double EagerAllocatedMb { get; init; }

  /// <summary>Managed bytes the engine path allocated, in MiB.</summary>
  public required double EngineAllocatedMb { get; init; }

  /// <summary>Eager over engine managed allocations — how many times more the rows-in-CLR path allocates.</summary>
  public required double AllocationRatioX { get; init; }
}
