namespace Flowthru.Abstractions;

/// <summary>
/// Marker interface for schema types that can be serialized to text-based formats (CSV, TSV).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Indicates a schema is compatible with text-based, flat file
/// formats that represent data in rows and columns.
/// </para>
/// <para>
/// <strong>Compatible Formats:</strong>
/// </para>
/// <list type="bullet">
/// <item>CSV (Comma-Separated Values)</item>
/// <item>TSV (Tab-Separated Values)</item>
/// <item>Other delimited text formats</item>
/// </list>
/// <para>
/// <strong>Requirements:</strong>
/// </para>
/// <para>
/// Schemas implementing this interface must:
/// </para>
/// <list type="bullet">
/// <item>Also implement <see cref="IFlatSchema"/> (no nested structures)</item>
/// <item>Have properties that can be converted to/from string representation</item>
/// <item>Support single-value serialization per field</item>
/// </list>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <para>
/// Separating serialization capability from structure allows:
/// - Compile-time enforcement: <c>CsvFormatSerializer&lt;T&gt; where T : IFlatSchema, ITextSerializable</c>
/// - Multiple serialization targets: A flat schema can be both text and binary serializable
/// - Clear documentation of supported formats
/// </para>
/// <para>
/// <strong>Typical Usage:</strong>
/// </para>
/// <para>
/// Most flat schemas should implement both <see cref="ITextSerializable"/> and
/// <see cref="IBinarySerializable"/> to support maximum format flexibility.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // ✅ Flat schema with text serialization
/// public sealed record CompanySchema(
///     int Id,
///     string Name,
///     float Rating
/// ) : IFlatSchema, ITextSerializable;
///
/// // ✅ Flat schema with multiple serialization capabilities
/// public sealed record DataRow(
///     DateTime Timestamp,
///     double Value,
///     string Category
/// ) : IFlatSchema, ITextSerializable, IBinarySerializable;
///
/// // ❌ Cannot be text serialized - nested structure
/// public sealed record NestedData(
///     string Id,
///     List&lt;string&gt; Tags  // Collection!
/// ) : INestedSchema, IStructuredSerializable;  // Not ITextSerializable
/// </code>
/// </example>
public interface ITextSerializable
{
  // Marker interface - no members required
}
