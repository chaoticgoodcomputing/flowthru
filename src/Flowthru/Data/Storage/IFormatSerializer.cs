namespace Flowthru.Data.Storage;

/// <summary>
/// Interface for format serialization - handles row-based serialization/deserialization.
/// </summary>
/// <typeparam name="TRow">The row type (schema) to serialize</typeparam>
/// <remarks>
/// <para>
/// <strong>Responsibility:</strong> Abstract HOW data is serialized (CSV, JSON, Parquet, etc.).
/// </para>
/// <para>
/// <strong>Separation of Concerns:</strong>
/// </para>
/// <para>
/// The format serializer is isolated from:
/// - Storage location (file vs memory) - handled by <see cref="IStorageMedium"/>
/// - Container type (IEnumerable vs IDataView) - handled by <see cref="IContainerAdapter{TContainer, TRow}"/>
/// </para>
/// <para>
/// <strong>Streaming Design:</strong>
/// </para>
/// <para>
/// Uses <see cref="IAsyncEnumerable{T}"/> for row streaming to:
/// - Support large datasets without loading everything into memory
/// - Enable backpressure and cancellation
/// - Allow format-agnostic streaming pipelines
/// </para>
/// <para>
/// <strong>Type Constraints:</strong>
/// </para>
/// <para>
/// Format serializers enforce schema compatibility through generic constraints:
/// </para>
/// <code>
/// public class CsvFormatSerializer&lt;T&gt; : IFormatSerializer&lt;T&gt;
///     where T : IFlatSchema, ITextSerializable
/// {
///     // Compile-time enforcement of flat + text serializable
/// }
/// </code>
/// <para>
/// <strong>Design Pattern:</strong>
/// </para>
/// <para>
/// This is the middle layer in the composition pattern:
/// </para>
/// <code>
/// Medium (bytes) → Format (rows) → Container (in-memory)
/// Stream         → IAsyncEnumerable&lt;TRow&gt; → IEnumerable&lt;TRow&gt;
/// </code>
/// </remarks>
/// <example>
/// <code>
/// // CSV serializer with flat schema constraint
/// var csvSerializer = new CsvFormatSerializer&lt;CompanySchema&gt;();
///
/// // Deserialize from stream to rows
/// await foreach (var row in csvSerializer.DeserializeRows(stream))
/// {
///     Console.WriteLine($"Company: {row.Name}");
/// }
///
/// // Serialize rows to stream
/// await csvSerializer.SerializeRows(stream, rows);
/// </code>
/// </example>
public interface IFormatSerializer<TRow>
{
  /// <summary>
  /// Deserializes a stream of bytes into a stream of rows.
  /// </summary>
  /// <param name="stream">The stream containing serialized data</param>
  /// <returns>Async enumerable of deserialized rows</returns>
  /// <remarks>
  /// <para>
  /// <strong>Streaming Behavior:</strong>
  /// </para>
  /// <para>
  /// Rows should be yielded as they are deserialized (lazy evaluation).
  /// This allows processing large datasets without loading everything into memory.
  /// </para>
  /// <para>
  /// <strong>Error Handling:</strong>
  /// </para>
  /// <para>
  /// Deserialization errors should throw exceptions:
  /// - Format exceptions (malformed CSV, invalid JSON)
  /// - Schema mismatches (missing columns, type conversion failures)
  /// - I/O errors during stream reading
  /// </para>
  /// <para>
  /// The caller should handle these exceptions appropriately.
  /// </para>
  /// </remarks>
  IAsyncEnumerable<TRow> DeserializeRows(Stream stream);

  /// <summary>
  /// Serializes a stream of rows into a stream of bytes.
  /// </summary>
  /// <param name="stream">The stream to write serialized data to</param>
  /// <param name="rows">The rows to serialize</param>
  /// <returns>Task that completes when serialization finishes</returns>
  /// <remarks>
  /// <para>
  /// <strong>Streaming Behavior:</strong>
  /// </para>
  /// <para>
  /// Rows should be written as they are enumerated (lazy evaluation).
  /// This allows handling large datasets efficiently.
  /// </para>
  /// <para>
  /// <strong>Format-Specific Headers:</strong>
  /// </para>
  /// <para>
  /// Implementations should handle format-specific initialization:
  /// - CSV: Write header row with column names
  /// - JSON: Write opening bracket for array
  /// - Parquet: Write schema metadata
  /// </para>
  /// <para>
  /// <strong>Error Handling:</strong>
  /// </para>
  /// <para>
  /// Serialization errors should throw exceptions:
  /// - Type conversion failures
  /// - I/O errors during stream writing
  /// - Invalid data values for format constraints
  /// </para>
  /// </remarks>
  Task SerializeRows(Stream stream, IAsyncEnumerable<TRow> rows);
}
