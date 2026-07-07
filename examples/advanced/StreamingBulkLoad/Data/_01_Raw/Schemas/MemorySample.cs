using Flowthru.Data.Schema;

namespace StreamingBulkLoad.Data._01_Raw.Schemas;

/// <summary>
/// One measurement row emitted by the self-instrumenting harness in
/// <c>Program.cs</c> — the peak memory an ingest variant held while loading the
/// same Parquet dataset into the same SQLite schema. Persisted as a Raw CSV
/// (<c>memory_samples.csv</c>) so the downstream Reporting Flow can read the
/// facts back like any other Catalog Item and prove the example's own thesis.
/// </summary>
[FlowthruSchema]
public partial record MemorySample
{
  /// <summary>Ingest variant: <c>Eager</c> or <c>Streaming</c>.</summary>
  public required string Variant { get; init; }

  /// <summary>Rows the variant loaded into the SQLite table (post-filter).</summary>
  public required int RowCount { get; init; }

  /// <summary>Peak OS working set observed during the variant's run, in bytes.</summary>
  public required long PeakWorkingSetBytes { get; init; }

  /// <summary>Peak managed heap (<c>GC.GetTotalMemory</c>) observed during the run, in bytes.</summary>
  public required long PeakManagedBytes { get; init; }

  /// <summary>Wall-clock duration of the variant's flow run, in milliseconds.</summary>
  public required long DurationMs { get; init; }
}
