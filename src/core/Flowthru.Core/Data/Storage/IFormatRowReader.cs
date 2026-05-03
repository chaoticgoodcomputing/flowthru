namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Format extension that can deserialize rows from a byte stream. Read-only formats
/// (e.g., Excel via ExcelDataReader) implement this interface and not
/// <see cref="IFormatRowWriter{TRow}"/> — their inability to write is a structural
/// fact carried in the type system, not a runtime trait check.
/// </summary>
/// <typeparam name="TRow">The row type produced by deserialization.</typeparam>
/// <remarks>
/// <para>
/// Phase D (capability-segmented interfaces) introduced this segment alongside
/// <see cref="IFormatRowWriter{TRow}"/>. Write-capable formats compose both via
/// <see cref="IFormatSerializer{TRow}"/>; read-only formats stop at this segment.
/// </para>
/// <para>
/// <strong>Streaming Behavior.</strong> Rows should be yielded as they are
/// deserialized (lazy evaluation) so consumers can process large datasets without
/// loading everything into memory.
/// </para>
/// <para>
/// <strong>Error Handling.</strong> Deserialization errors should throw exceptions:
/// format exceptions (malformed CSV, invalid JSON), schema mismatches (missing
/// columns, type-conversion failures), or I/O errors during stream reading. Callers
/// are expected to surface these to the pipeline's error reporting.
/// </para>
/// </remarks>
public interface IFormatRowReader<TRow> : IFormatBase<TRow>
  where TRow : notnull
{
  /// <summary>
  /// Deserializes a stream of bytes into a stream of rows.
  /// </summary>
  /// <param name="stream">The stream containing serialized data.</param>
  /// <returns>Async enumerable of deserialized rows.</returns>
  IAsyncEnumerable<TRow> DeserializeRows(Stream stream);
}
