namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Format extension that can serialize rows to a byte stream. Write-only sinks (rare
/// — typically only logging-style formats) would implement this interface and not
/// <see cref="IFormatRowReader{TRow}"/>; in the current first-party suite, every
/// writer is also a reader and composes both via <see cref="IFormatSerializer{TRow}"/>.
/// </summary>
/// <typeparam name="TRow">The row type consumed during serialization.</typeparam>
/// <remarks>
/// <para>
/// Phase D (capability-segmented interfaces) introduced this segment alongside
/// <see cref="IFormatRowReader{TRow}"/>. Compile-time read-only-ness (a format that
/// genuinely cannot write) is expressed by NOT implementing this interface; runtime
/// read-only-ness (an instance pointed at a read-only file system) is expressed via
/// <see cref="Capabilities.StorageTraits.CanWrite"/>. Both signals coexist.
/// </para>
/// <para>
/// <strong>Streaming Behavior.</strong> Rows should be written as they are enumerated
/// (lazy evaluation) so callers can hand off large datasets without buffering.
/// </para>
/// <para>
/// <strong>Format-Specific Headers.</strong> Implementations should handle format-
/// specific initialization: CSV writes the header row with column names; JSON writes
/// the opening array bracket; Parquet writes schema metadata before the first row
/// group. The serialization pass owns the entire stream; do not assume an upstream
/// consumer has prepared anything.
/// </para>
/// </remarks>
public interface IFormatRowWriter<TRow> : IFormatBase<TRow>
  where TRow : notnull
{
  /// <summary>
  /// Serializes a stream of rows into a stream of bytes.
  /// </summary>
  /// <param name="stream">The stream to write serialized data to.</param>
  /// <param name="rows">The rows to serialize.</param>
  /// <returns>Task that completes when serialization finishes.</returns>
  Task SerializeRows(Stream stream, IAsyncEnumerable<TRow> rows);
}
