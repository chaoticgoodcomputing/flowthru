namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Format extension that produces rows incrementally without buffering the full
/// input. Implementing this interface is a <em>structural</em> claim — distinct from
/// the runtime <see cref="Capabilities.StorageTraits.CanStream"/> flag — that the
/// format's deserialization path is genuinely streaming: bounded memory, no
/// "parse-the-whole-array-then-yield" buffering.
/// </summary>
/// <typeparam name="TRow">The row type produced by deserialization.</typeparam>
/// <remarks>
/// <para>
/// Phase D (capability-segmented interfaces) added this sub-interface of
/// <see cref="IFormatRowReader{TRow}"/>. The reader segment alone says
/// "I can produce <see cref="IAsyncEnumerable{T}"/>"; this segment additionally
/// says "I do so without first materializing the input." Formats that buffer the
/// full payload (JSON-as-array via <c>JsonSerializer.Deserialize</c>) implement
/// only the parent <see cref="IFormatRowReader{TRow}"/>. Formats that decode
/// row-at-a-time off a forward-only cursor (CsvHelper, Parquet's row-group
/// iteration, Spark Structured Streaming sources) implement this segment.
/// </para>
/// <para>
/// <strong>Why a marker rather than a new method.</strong> The parent's
/// <see cref="IFormatRowReader{TRow}.DeserializeRows"/> already returns
/// <see cref="IAsyncEnumerable{T}"/>; what differs is the <em>guarantee</em>
/// behind the enumeration, not the surface. Consumers needing bounded-memory
/// processing on unknown-size sources can branch on
/// <c>reader is IFormatStreamReader&lt;TRow&gt;</c> at compile time.
/// </para>
/// <para>
/// <strong>Future Spark-style continuous sources.</strong> Unbounded streaming
/// (Spark Structured Streaming, Kafka, Kinesis) is a storage-medium concern
/// rather than a format concern. This segment captures the format-side property
/// — "decoding does not require seeing the end of the input" — that any
/// streaming medium must compose with.
/// </para>
/// </remarks>
public interface IFormatStreamReader<TRow> : IFormatRowReader<TRow>
  where TRow : notnull
{
}
