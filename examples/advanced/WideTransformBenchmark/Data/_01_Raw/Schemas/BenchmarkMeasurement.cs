using Flowthru.Data.Schema;

namespace WideTransformBenchmark.Data._01_Raw.Schemas;

/// <summary>
/// One measured benchmark run, recorded by the harness in
/// <c>Benchmark/BenchmarkRunner.cs</c>: the same optimize pass over the same
/// fabricated dataset, timed through one of the two transform paths. Persisted
/// as a Raw CSV (<c>benchmark_measurements.csv</c>) so the Analyze Flow reads
/// the facts back like any other Catalog Item — Flowthru ingesting its own
/// profiling data.
/// </summary>
/// <remarks>
/// All fields are machine-agnostic by design: row counts, path labels, and the
/// measured numbers. No hostnames, core counts, or timestamps — the report
/// carries the run's shape, not the machine's identity.
/// </remarks>
[FlowthruSchema]
public partial record BenchmarkMeasurement
{
  /// <summary>Transform path: <c>Eager</c> (C# LINQ Step) or <c>Engine</c> (DuckDB SQL transform).</summary>
  public required string TransformPath { get; init; }

  /// <summary>Fabricated input rows the run read (pre-dedup).</summary>
  public required int InputRows { get; init; }

  /// <summary>Rows the run wrote (post-dedup). Must agree between the two paths.</summary>
  public required int OutputRows { get; init; }

  /// <summary>Wall-clock duration of the flow run, in milliseconds.</summary>
  public required long ElapsedMs { get; init; }

  /// <summary>Managed bytes allocated during the flow run (<c>GC.GetTotalAllocatedBytes</c> delta).</summary>
  public required long AllocatedBytes { get; init; }
}
