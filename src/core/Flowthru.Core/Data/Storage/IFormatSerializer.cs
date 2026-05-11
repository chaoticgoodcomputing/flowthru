namespace Flowthru.Data.Storage;

/// <summary>
/// Format extension that can both serialize and deserialize rows. The
/// canonical "round-trip-capable" format interface; composes
/// <see cref="IFormatRowReader{TRow}"/> and
/// <see cref="IFormatRowWriter{TRow}"/>. Most first-party formats
/// (CSV, JSON, Parquet, XML) implement this; read-only formats
/// (e.g., Excel via ExcelDataReader) implement
/// <see cref="IFormatRowReader{TRow}"/> only.
/// </summary>
/// <typeparam name="TRow">The row type the format handles.</typeparam>
public interface IFormatSerializer<TRow>
  : IFormatRowReader<TRow>, IFormatRowWriter<TRow>
  where TRow : notnull
{
}
