using System.IO.Compression;
using Flowthru.Data.Schema;
using Parquet;
using Parquet.Serialization;

namespace Flowthru.Data.Storage.Parquet;

/// <summary>
/// Performance and behavior tuning options for Parquet catalog entries.
/// </summary>
/// <typeparam name="TRow">The row schema type this options object is bound to.</typeparam>
/// <remarks>
/// <para>
/// Pass an instance to the
/// <c>ItemFactory.Enumerable.Parquet&lt;TRow&gt;</c> /
/// <c>ItemFactory.Directory.Parquet&lt;TRow&gt;</c> smart constructors
/// to override defaults. A bare entry with no options uses
/// production-ready defaults:
/// </para>
/// <list type="bullet">
/// <item><b>RowGroupSize</b> — 1 000 000 rows. The write path streams in batches of this size,
/// keeping peak write-side memory bounded regardless of dataset size.</item>
/// <item><b>CompressionMethod</b> — Snappy (best latency/ratio balance for analytic workloads).</item>
/// <item><b>UseDictionaryEncoding</b> — true (automatic dictionary encoding for low-cardinality
/// columns).</item>
/// </list>
/// <para>
/// <b>Row group sizing guidance.</b> Target 128–512 MB of uncompressed row data per group. At
/// ~100 bytes/row, 1 000 000 rows ≈ 100 MB — a reasonable default. Reduce for wider rows;
/// increase for narrower (purely numeric) rows.
/// </para>
/// <para>
/// <b>Compression guidance.</b>
/// <list type="bullet">
/// <item><see cref="CompressionMethod.Snappy"/> — low CPU, fast decompression; best for
/// interactive and real-time workloads.</item>
/// <item><see cref="CompressionMethod.Zstd"/> — better ratio than Snappy at moderate CPU cost.</item>
/// <item><see cref="CompressionMethod.Gzip"/> — highest ratio, slowest; use when storage cost
/// dominates query latency.</item>
/// </list>
/// </para>
/// <para>
/// <b>Per-column encoding hints</b> (e.g. Delta encoding for sorted ID columns) require
/// Parquet.Net v6+ which is not yet on NuGet. Until then, <see cref="UseDictionaryEncoding"/>
/// and <see cref="UseDeltaBinaryPackedEncoding"/> apply globally.
/// </para>
/// </remarks>
public sealed record ParquetItemOptions<TRow>
  where TRow : notnull, IFlatSchema, IBinarySerializable
{
  // ── Write path ────────────────────────────────────────────────────────

  /// <summary>Number of rows per row group on write. Defaults to 1 000 000.</summary>
  public int RowGroupSize { get; init; } = 1_000_000;

  /// <summary>Compression algorithm applied to each data page. Defaults to Snappy.</summary>
  public CompressionMethod CompressionMethod { get; init; } = CompressionMethod.Snappy;

  /// <summary>Compression level hint passed to the codec. Defaults to <see cref="CompressionLevel.Optimal"/>.</summary>
  public CompressionLevel CompressionLevel { get; init; } = CompressionLevel.Optimal;

  // ── Encoding ──────────────────────────────────────────────────────────

  /// <summary>Enable dictionary encoding globally. Defaults to <c>true</c>.</summary>
  public bool UseDictionaryEncoding { get; init; } = true;

  /// <summary>
  /// Uniqueness factor threshold below which dictionary encoding is applied.
  /// Defaults to 0.8 (apply when ≤ 80% of values are unique).
  /// </summary>
  public double DictionaryEncodingThreshold { get; init; } = 0.8;

  /// <summary>Enable delta-binary-packed encoding globally for integer columns. Defaults to <c>false</c>.</summary>
  public bool UseDeltaBinaryPackedEncoding { get; init; } = false;

  // ── Type mapping ──────────────────────────────────────────────────────

  /// <summary>Deserialize Parquet DATE columns as <see cref="DateOnly"/> instead of <see cref="DateTime"/>.</summary>
  public bool UseDateOnlyForDates { get; init; } = false;

  /// <summary>Deserialize Parquet TIME (millis) columns as <see cref="TimeOnly"/>.</summary>
  public bool UseTimeOnlyForTimeMillis { get; init; } = false;

  /// <summary>Deserialize Parquet TIME (micros) columns as <see cref="TimeOnly"/>.</summary>
  public bool UseTimeOnlyForTimeMicros { get; init; } = false;

  /// <summary>Use <c>BigDecimal</c> instead of <c>decimal</c> for high-precision decimal columns.</summary>
  public bool UseBigDecimal { get; init; } = false;

  // ── Memory pool ───────────────────────────────────────────────────────

  /// <summary>Maximum bytes kept in the small-object pool. Defaults to 16 MB.</summary>
  public int MaximumSmallPoolFreeBytes { get; init; } = 16 * 1024 * 1024;

  /// <summary>Maximum bytes kept in the large-object pool. Defaults to 64 MB.</summary>
  public int MaximumLargePoolFreeBytes { get; init; } = 64 * 1024 * 1024;

  // ── Internal Parquet.Net options materialisation ──────────────────────

  internal ParquetSerializerOptions ToWriteOptions(bool append = false) =>
    new()
    {
      Append = append,
      CompressionMethod = CompressionMethod,
      CompressionLevel = CompressionLevel,
      RowGroupSize = RowGroupSize,
      ParquetOptions = BuildParquetOptions(),
    };

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
