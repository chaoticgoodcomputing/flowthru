namespace Flowthru.Data.Storage;

/// <summary>
/// Format extension that can serialize rows to a byte stream. Compile-time
/// read-only-ness — a format that genuinely cannot write — is expressed
/// by NOT implementing this interface. Runtime read-only-ness — an
/// instance pointed at a read-only file system — is expressed via
/// <see cref="StorageTraits.CanWrite"/>. Both signals coexist.
/// </summary>
/// <typeparam name="TRow">The row type consumed during serialization.</typeparam>
/// <remarks>
/// Implementations handle format-specific initialization: CSV writes the
/// header row, JSON writes the opening array bracket, Parquet writes
/// schema metadata before the first row group. The serialization pass
/// owns the entire stream.
/// </remarks>
public interface IFormatRowWriter<TRow> : IFormatBase<TRow>
  where TRow : notnull
{
  /// <summary>Serializes a stream of rows into a stream of bytes.</summary>
  Task SerializeRows(Stream stream, IAsyncEnumerable<TRow> rows);
}
