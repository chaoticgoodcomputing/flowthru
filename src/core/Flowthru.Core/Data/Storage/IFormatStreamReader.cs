namespace Flowthru.Data.Storage;

/// <summary>
/// Format extension that produces rows incrementally without buffering
/// the full input. Implementing this interface is a *structural* claim —
/// distinct from the runtime <see cref="StorageTraits.CanStream"/> flag —
/// that the format's deserialization path is genuinely streaming:
/// bounded memory, no "parse-the-whole-array-then-yield" buffering.
/// </summary>
/// <typeparam name="TRow">The row type produced by deserialization.</typeparam>
/// <remarks>
/// Formats that buffer the full payload before yielding implement only the
/// parent <see cref="IFormatRowReader{TRow}"/>. Formats that decode
/// row-at-a-time off a forward-only cursor — CsvHelper, Parquet's row-group
/// iteration, JSON via <c>DeserializeAsyncEnumerable</c> — implement this
/// segment.
/// </remarks>
public interface IFormatStreamReader<TRow> : IFormatRowReader<TRow>
  where TRow : notnull
{
}
