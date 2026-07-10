using Flowthru.Data.Schema;

namespace WideTransformBenchmark.Data._01_Raw.Schemas;

/// <summary>
/// One fabricated sensor reading as it would arrive from a noisy ingest feed:
/// a duplicate-prone composite key (<see cref="DeviceId"/>,
/// <see cref="Channel"/>, <see cref="ObservedAt"/>), the two columns worth
/// keeping (<see cref="Reading"/>, <see cref="Unit"/>), and a tail of fat
/// lineage columns that exist only to be pruned. The optimize pass — sort by
/// the composite key, keep the first-ingested row per key, project away the
/// lineage tail — is the canonical wide transform both benchmark paths run.
/// </summary>
/// <remarks>
/// The generator emits rows in <see cref="RowId"/> order, so "first-ingested
/// per key" means "lowest <see cref="RowId"/> per key" on both paths: LINQ's
/// stable <c>OrderBy</c> + <c>DistinctBy</c> keeps it implicitly; the SQL keeps
/// it explicitly via <c>row_number() OVER (... ORDER BY RowId)</c>.
/// </remarks>
[FlowthruSchema]
public partial record RawReadingRow
{
  /// <summary>Ingest sequence number (0..N-1) — the dedup tie-break. Pruned by the optimize pass.</summary>
  public required long RowId { get; init; }

  /// <summary>Reporting device — composite-key part 1.</summary>
  public required string DeviceId { get; init; }

  /// <summary>Sensor channel on the device (temp, humidity, ...) — composite-key part 2.</summary>
  public required string Channel { get; init; }

  /// <summary>Observation timestamp (UTC, whole seconds) — composite-key part 3.</summary>
  public required DateTime ObservedAt { get; init; }

  /// <summary>The measured value — kept by the optimize pass.</summary>
  public required double Reading { get; init; }

  /// <summary>Unit of measure for <see cref="Reading"/> — kept by the optimize pass.</summary>
  public required string Unit { get; init; }

  /// <summary>Source file the row claims to come from. Prunable lineage.</summary>
  public required string SourceFile { get; init; }

  /// <summary>Collector that ingested the row. Prunable lineage.</summary>
  public required string IngestedBy { get; init; }

  /// <summary>The original wire payload — the fat column that makes pruning worthwhile.</summary>
  public required string RawPayload { get; init; }

  /// <summary>Ingest batch the row arrived in. Prunable lineage.</summary>
  public required long BatchId { get; init; }

  /// <summary>Payload checksum as recorded at ingest. Prunable lineage.</summary>
  public required string Checksum { get; init; }
}
