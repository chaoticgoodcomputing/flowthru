using System.IO.Compression;
using Flowthru.Core.Abstractions;
using Parquet;
using Parquet.Serialization;

namespace Flowthru.Core.Data;

/// <summary>
/// Performance and behavior tuning options for Parquet catalog entries.
/// </summary>
/// <typeparam name="TRow">The row schema type this options object is bound to.</typeparam>
/// <remarks>
/// <para>
/// Pass an instance to <see cref="ParquetItemExtensions.Parquet{TRow}"/> to override defaults.
/// A bare catalog entry with no options uses production-ready defaults automatically:
/// </para>
/// <list type="bullet">
/// <item><b>RowGroupSize</b> — 1 000 000 rows (Parquet.Net default). The write path streams rows
/// in batches of this size, keeping peak write-side memory bounded regardless of dataset size.</item>
/// <item><b>CompressionMethod</b> — Snappy (best latency/ratio balance for analytic workloads).</item>
/// <item><b>UseDictionaryEncoding</b> — true (automatic dictionary encoding for low-cardinality
/// columns such as categorical strings and IDs).</item>
/// </list>
/// <para>
/// <b>Row group sizing guidance (per Parquet best practices):</b>
/// Target 128 MB – 512 MB of uncompressed row data per group. At ~100 bytes/row, 1 000 000 rows
/// ≈ 100 MB — a reasonable default. For wider rows, reduce <see cref="RowGroupSize"/>. For
/// narrower rows (e.g. pure numeric), you can increase it.
/// </para>
/// <para>
/// <b>Compression guidance:</b>
/// <list type="bullet">
/// <item><see cref="CompressionMethod.Snappy"/> — low CPU, fast decompression; best for interactive
/// and real-time workloads.</item>
/// <item><see cref="CompressionMethod.Zstd"/> — tunable; better ratio than Snappy at moderate CPU
/// cost; suitable for cold/archival paths.</item>
/// <item><see cref="CompressionMethod.Gzip"/> — highest ratio, slowest; use when storage cost
/// dominates query latency requirements.</item>
/// </list>
/// </para>
/// <para>
/// <b>Per-column encoding hints</b> (e.g. Delta encoding for sorted ID columns) require
/// <c>ColumnEncodingHints</c>, which is a Parquet.Net v6+ API not yet published to NuGet.
/// This will be surfaced via a <c>WithEncodingHint(expr, hint)</c> fluent method once v6 is stable.
/// In the meantime, <see cref="UseDictionaryEncoding"/> and <see cref="UseDeltaBinaryPackedEncoding"/>
/// apply globally.
/// </para>
/// </remarks>
public sealed record ParquetItemOptions<TRow>
  where TRow : notnull, IFlatSchema, IBinarySerializable
{
  // ── Write path ────────────────────────────────────────────────────────────

  /// <summary>
  /// Number of rows per row group on write. Defaults to 1 000 000.
  /// </summary>
  /// <remarks>
  /// The write path buffers up to this many rows in memory, then flushes one row group to disk.
  /// Peak write-side memory is bounded to approximately <c>RowGroupSize × (row byte width)</c>.
  /// </remarks>
  public int RowGroupSize { get; init; } = 1_000_000;

  /// <summary>
  /// Compression algorithm applied to each data page. Defaults to <see cref="CompressionMethod.Snappy"/>.
  /// </summary>
  public CompressionMethod CompressionMethod { get; init; } = CompressionMethod.Snappy;

  /// <summary>
  /// Compression level hint passed to the chosen codec. Defaults to <see cref="CompressionLevel.Optimal"/>.
  /// </summary>
  public CompressionLevel CompressionLevel { get; init; } = CompressionLevel.Optimal;

  // ── Encoding ──────────────────────────────────────────────────────────────

  /// <summary>
  /// Enable dictionary encoding globally. Defaults to <c>true</c>.
  /// </summary>
  /// <remarks>
  /// Dictionary encoding stores repeated values (low-cardinality columns like enums, categories,
  /// product codes) as integer references to a dictionary side-table. This typically halves storage
  /// for such columns. Disable only if all columns have near-100% unique values.
  /// </remarks>
  public bool UseDictionaryEncoding { get; init; } = true;

  /// <summary>
  /// Uniqueness factor threshold (0–1) below which dictionary encoding is applied.
  /// Defaults to 0.8 (i.e. dictionary encoding when ≤ 80% of values are unique).
  /// </summary>
  public double DictionaryEncodingThreshold { get; init; } = 0.8;

  /// <summary>
  /// Enable delta-binary-packed encoding globally for integer columns. Defaults to <c>false</c>.
  /// </summary>
  /// <remarks>
  /// Most effective on monotonically increasing integer columns (auto-increment IDs, timestamps)
  /// where successive delta values are small. May slightly increase CPU on read. Leave at
  /// <c>false</c> unless you have profiled this as a bottleneck.
  /// </remarks>
  public bool UseDeltaBinaryPackedEncoding { get; init; } = false;

  // ── Type mapping ─────────────────────────────────────────────────────────

  /// <summary>
  /// Deserialize Parquet DATE columns as <see cref="DateOnly"/> instead of <see cref="DateTime"/>.
  /// Defaults to <c>false</c> for backwards compatibility.
  /// </summary>
  public bool UseDateOnlyForDates { get; init; } = false;

  /// <summary>
  /// Deserialize Parquet TIME (millisecond precision) columns as <see cref="TimeOnly"/>.
  /// Defaults to <c>false</c>.
  /// </summary>
  public bool UseTimeOnlyForTimeMillis { get; init; } = false;

  /// <summary>
  /// Deserialize Parquet TIME (microsecond precision) columns as <see cref="TimeOnly"/>.
  /// Defaults to <c>false</c>.
  /// </summary>
  public bool UseTimeOnlyForTimeMicros { get; init; } = false;

  /// <summary>
  /// Use <c>BigDecimal</c> instead of <c>decimal</c> for high-precision decimal columns.
  /// Defaults to <c>false</c>.
  /// </summary>
  public bool UseBigDecimal { get; init; } = false;

  // ── Memory pool ───────────────────────────────────────────────────────────

  /// <summary>
  /// Maximum bytes kept in the small-object pool before the GC may reclaim them.
  /// Defaults to 16 MB. Reduce to lower peak memory; increase to reduce GC pressure on
  /// write-heavy workloads.
  /// </summary>
  public int MaximumSmallPoolFreeBytes { get; init; } = 16 * 1024 * 1024;

  /// <summary>
  /// Maximum bytes kept in the large-object pool before the GC may reclaim them.
  /// Defaults to 64 MB.
  /// </summary>
  public int MaximumLargePoolFreeBytes { get; init; } = 64 * 1024 * 1024;

  // ── Internal helpers ─────────────────────────────────────────────────────

  /// <summary>
  /// Materializes a <see cref="ParquetSerializerOptions"/> suitable for a write operation.
  /// </summary>
  internal ParquetSerializerOptions ToWriteOptions(bool append = false) =>
    new()
    {
      Append = append,
      CompressionMethod = CompressionMethod,
      CompressionLevel = CompressionLevel,
      RowGroupSize = RowGroupSize,
      ParquetOptions = BuildParquetOptions(),
    };

  /// <summary>
  /// Materializes a <see cref="ParquetSerializerOptions"/> suitable for a read operation.
  /// </summary>
  internal ParquetSerializerOptions ToReadOptions() =>
    new() { ParquetOptions = BuildParquetOptions() };

  private ParquetOptions BuildParquetOptions() =>
    new()
    {
      UseDictionaryEncoding = UseDictionaryEncoding,
      DictionaryEncodingThreshold = DictionaryEncodingThreshold,
      UseDeltaBinaryPackedEncoding = UseDeltaBinaryPackedEncoding,
      UseDateOnlyTypeForDates = UseDateOnlyForDates,
      UseTimeOnlyTypeForTimeMillis = UseTimeOnlyForTimeMillis,
      UseTimeOnlyTypeForTimeMicros = UseTimeOnlyForTimeMicros,
      UseBigDecimal = UseBigDecimal,
      MaximumSmallPoolFreeBytes = MaximumSmallPoolFreeBytes,
      MaximumLargePoolFreeBytes = MaximumLargePoolFreeBytes,
    };
}
