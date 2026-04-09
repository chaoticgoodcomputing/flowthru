using Flowthru.Core.Data.Capabilities;

namespace Flowthru.Core.Data.Storage;

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
/// The <c>notnull</c> constraint ensures all format serializers support modern C# patterns:
/// </para>
/// <list type="bullet">
/// <item><strong>Traditional schemas:</strong> Classes/records with parameterless constructors</item>
/// <item><strong>Required members:</strong> C# 11+ schemas with <c>required</c> properties</item>
/// <item><strong>Positional records:</strong> Records with primary constructors</item>
/// </list>
/// <para>
/// This constraint explicitly prohibits the <c>new()</c> constraint, which is incompatible
/// with required members and positional records. Format serializers must use
/// <see cref="SchemaActivator"/> or equivalent techniques to instantiate schemas.
/// </para>
/// <para>
/// Format serializers may add additional constraints for format-specific requirements:
/// </para>
/// <code>
/// public class CsvFormatSerializer&lt;T&gt; : IFormatSerializer&lt;T&gt;
///     where T : notnull, IFlatSchema, ITextSerializable
/// {
///     // Compile-time enforcement: notnull + flat + text serializable
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
  where TRow : notnull
{
  /// <summary>
  /// Structural capabilities of this format serializer.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Format traits focus on HOW data is serialized and whether it supports streaming.
  /// For composed adapters, these traits are merged with medium and container traits.
  /// </para>
  /// <para>
  /// Most formats should declare <c>CanStream = true</c> if they can deserialize row-by-row
  /// without buffering the entire stream (e.g., CSV, JSONL). Formats that require full
  /// parsing before yielding rows (e.g., JSON arrays) should set <c>CanStream = false</c>.
  /// </para>
  /// </remarks>
  StorageTraits Traits { get; }

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

  /// <summary>
  /// Configures how this serializer handles property-to-field name mapping for the schema.
  /// </summary>
  /// <returns>Property mapping configuration describing the mapping strategy</returns>
  /// <remarks>
  /// <para>
  /// <strong>Contractual Obligation:</strong> Every format serializer MUST implement this method
  /// to explicitly declare how it handles property name mapping.
  /// </para>
  /// <para>
  /// <strong>Implementation Strategies:</strong>
  /// </para>
  /// <list type="bullet">
  /// <item>
  /// <strong>SerializedLabel Support:</strong> Use <see cref="Format.PropertyMappingHelper"/>
  /// to respect [SerializedLabel] attributes. Return <see cref="PropertyMappingConfiguration.FromSerializedLabel{T}()"/>.
  /// </item>
  /// <item>
  /// <strong>Native Attribute Mapping:</strong> Use format-specific attributes (e.g., ML.NET's [LoadColumn]).
  /// Return <see cref="PropertyMappingConfiguration.FromNativeAttributes(string)"/> with the attribute type name.
  /// </item>
  /// <item>
  /// <strong>Library-Controlled:</strong> When the underlying library handles mapping internally
  /// (e.g., Parquet.NET with no programmatic API). Return <see cref="PropertyMappingConfiguration.LibraryControlled()"/>.
  /// </item>
  /// <item>
  /// <strong>Adapter Pattern:</strong> Bridge between SerializedLabel and native attributes.
  /// Return <see cref="PropertyMappingConfiguration.FromAdapter{TAdapter}()"/>.
  /// </item>
  /// </list>
  /// <para>
  /// <strong>Design Intent:</strong> This contract makes property mapping an explicit, discoverable
  /// capability rather than an implicit behavior, enabling:
  /// </para>
  /// <list type="bullet">
  /// <item>Runtime introspection of mapping capabilities</item>
  /// <item>Better error messages when schemas don't match storage</item>
  /// <item>Documentation generation for serializer capabilities</item>
  /// <item>Testing framework validation of mapping correctness</item>
  /// </list>
  /// </remarks>
  /// <example>
  /// <code>
  /// // CSV serializer using SerializedLabel
  /// public PropertyMappingConfiguration GetPropertyMappingConfiguration()
  ///     => PropertyMappingConfiguration.FromSerializedLabel&lt;TRow&gt;();
  ///
  /// // ML.NET serializer using native attributes
  /// public PropertyMappingConfiguration GetPropertyMappingConfiguration()
  ///     => PropertyMappingConfiguration.FromNativeAttributes("LoadColumnAttribute");
  ///
  /// // Parquet with library-controlled mapping
  /// public PropertyMappingConfiguration GetPropertyMappingConfiguration()
  ///     => PropertyMappingConfiguration.LibraryControlled();
  /// </code>
  /// </example>
  PropertyMappingConfiguration GetPropertyMappingConfiguration();
}
