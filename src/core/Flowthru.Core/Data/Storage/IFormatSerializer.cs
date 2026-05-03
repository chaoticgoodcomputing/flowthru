namespace Flowthru.Core.Data.Storage;

/// <summary>
/// Format extension that supports both reading and writing — the composition of
/// <see cref="IFormatRowReader{TRow}"/> and <see cref="IFormatRowWriter{TRow}"/>.
/// </summary>
/// <typeparam name="TRow">The row type the format handles.</typeparam>
/// <remarks>
/// <para>
/// <strong>Use this interface for write-capable formats.</strong> CSV, JSON, and
/// Parquet all implement <see cref="IFormatSerializer{TRow}"/>. Read-only formats —
/// Excel and the like — implement only <see cref="IFormatRowReader{TRow}"/>; their
/// inability to write is a compile-time fact, not a runtime trait check.
/// </para>
/// <para>
/// <strong>Generic constraints.</strong> Format extensions typically add format-
/// specific constraints on top of <c>TRow : notnull</c>:
/// </para>
/// <code>
/// public sealed class CsvFormatSerializer&lt;T&gt; : IFormatSerializer&lt;T&gt;
///   where T : notnull, IFlatSchema, ITextSerializable
/// {
///   // Compile-time enforcement: notnull + flat + text serializable
/// }
/// </code>
/// <para>
/// The <c>notnull</c> constraint deliberately prohibits the <c>new()</c> constraint,
/// which is incompatible with required members and positional records. Format
/// implementations use <see cref="SchemaActivator"/> or equivalent for instantiation.
/// </para>
/// <para>
/// <strong>Layered design.</strong>
/// </para>
/// <code>
/// Medium (bytes) → Format (rows) → Container (in-memory)
/// Stream         → IAsyncEnumerable&lt;TRow&gt; → IEnumerable&lt;TRow&gt;
/// </code>
/// <para>
/// The segmented split (Phase D) lets <see cref="ComposedStorageAdapter{TContainer,TRow}"/>
/// dispatch on interface presence: a Storage adapter built from
/// <see cref="IFormatRowReader{TRow}"/> alone exposes a read-only surface;
/// one built from a full <see cref="IFormatSerializer{TRow}"/> exposes both
/// directions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // CSV serializer with flat schema constraint
/// var csv = new CsvFormatSerializer&lt;CompanySchema&gt;();
///
/// // Deserialize from stream to rows
/// await foreach (var row in csv.DeserializeRows(stream))
/// {
///   Console.WriteLine($"Company: {row.Name}");
/// }
///
/// // Serialize rows to stream
/// await csv.SerializeRows(stream, rows);
/// </code>
/// </example>
public interface IFormatSerializer<TRow> : IFormatRowReader<TRow>, IFormatRowWriter<TRow>
  where TRow : notnull
{
  // Composition only — all members come from the read and write segments. Future
  // members that apply to the full duplex serializer (e.g., a `RoundTripValidate`
  // pre-flight hook) can be added here without affecting read-only formats.
}
