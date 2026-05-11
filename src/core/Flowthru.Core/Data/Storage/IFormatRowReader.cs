namespace Flowthru.Data.Storage;

/// <summary>
/// Format extension that can deserialize rows from a byte stream.
/// Read-only formats (e.g., Excel via ExcelDataReader) implement this
/// interface and not <see cref="IFormatRowWriter{TRow}"/> — their
/// inability to write is a structural fact carried in the type system,
/// not a runtime trait check.
/// </summary>
/// <typeparam name="TRow">The row type produced by deserialization.</typeparam>
public interface IFormatRowReader<TRow> : IFormatBase<TRow>
  where TRow : notnull
{
  /// <summary>
  /// Deserializes a stream of bytes into a stream of rows. Rows are yielded
  /// as deserialized (lazy enumeration) so consumers can process large
  /// datasets without materializing everything in memory.
  /// </summary>
  IAsyncEnumerable<TRow> DeserializeRows(Stream stream);
}
