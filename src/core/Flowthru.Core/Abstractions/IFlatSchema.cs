namespace Flowthru.Core.Abstractions;

/// <summary>
/// Marker interface for schema types with flat (non-nested) structure.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Indicates a schema contains only primitive properties,
/// with no collections or nested objects.
/// </para>
/// <para>
/// <strong>Flat Structure Requirements:</strong>
/// </para>
/// <list type="bullet">
/// <item>All properties are primitives (int, long, double, decimal, bool, string, DateTime, etc.)</item>
/// <item>Nullable primitives (int?, double?, bool?, etc.) are allowed</item>
/// <item>Enums and nullable enums are allowed</item>
/// <item>Value types that serialize to single values (Guid, TimeSpan, etc.) are allowed</item>
/// </list>
/// <para>
/// <strong>Not Flat (Use INestedSchema instead):</strong>
/// </para>
/// <list type="bullet">
/// <item>Collections: List&lt;T&gt;, Array, IEnumerable&lt;T&gt;, etc.</item>
/// <item>Nested objects: Custom class/record properties</item>
/// <item>Dictionaries or complex structures</item>
/// </list>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <para>
/// Separating structural properties (flat vs nested) from serialization capabilities
/// (text/structured/binary) enables:
/// - Explicit schema documentation
/// - Compile-time format validation
/// - Format serializers can enforce appropriate constraints
/// - Clear intent about data structure complexity
/// </para>
/// <para>
/// <strong>Relationship with Serialization Markers:</strong>
/// </para>
/// <para>
/// A flat schema should also implement one or more serialization capability markers:
/// - <see cref="ITextSerializable"/> - Can serialize to CSV/TSV
/// - <see cref="IStructuredSerializable"/> - Can serialize to JSON/XML
/// - <see cref="IBinarySerializable"/> - Can serialize to Parquet/Avro
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // ✅ Flat schema with multiple serialization capabilities
/// public sealed record CompanySchema(
///     int Id,
///     string Name,
///     float Rating,
///     bool IsActive
/// ) : IFlatSchema, ITextSerializable, IBinarySerializable;
///
/// // ❌ Not flat - contains collection
/// public sealed record OrderSchema(
///     string OrderId,
///     List&lt;LineItem&gt; Items  // Collection = nested!
/// ) : INestedSchema, IStructuredSerializable;
/// </code>
/// </example>
public interface IFlatSchema
{
  // Marker interface - no members required
}
