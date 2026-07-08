namespace Flowthru.Step.DuckDb;

/// <summary>
/// Per-transform tuning for how the engine writes the output Parquet
/// file. Defaults match the Parquet extension's write defaults so a
/// DuckDB-written file behaves like a Flowthru-written one downstream.
/// </summary>
public sealed record DuckDbTransformOptions
{
  /// <summary>
  /// Compression codec for the output file. Default
  /// <see cref="DuckDbParquetCompression.Snappy"/>.
  /// </summary>
  public DuckDbParquetCompression Compression { get; init; } = DuckDbParquetCompression.Snappy;

  /// <summary>
  /// Rows per output row group. <c>null</c> uses DuckDB's default
  /// (122,880). Larger groups compress better; smaller groups let
  /// downstream streaming reads hold less in memory at once.
  /// </summary>
  public long? RowGroupSize { get; init; }

  /// <summary>The shared default instance.</summary>
  public static DuckDbTransformOptions Default { get; } = new();
}

/// <summary>
/// Parquet compression codecs DuckDB can write.
/// </summary>
public enum DuckDbParquetCompression
{
  /// <summary>Snappy — fast, moderate ratio; the ecosystem default.</summary>
  Snappy,
  /// <summary>Zstandard — slower writes, better ratio.</summary>
  Zstd,
  /// <summary>Gzip — widest compatibility, slowest.</summary>
  Gzip,
  /// <summary>No compression.</summary>
  Uncompressed,
}
