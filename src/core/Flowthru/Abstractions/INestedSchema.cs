namespace Flowthru.Abstractions;

/// <summary>
/// Marker interface for schema types with nested structure (collections or nested objects).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Indicates a schema contains hierarchical data requiring
/// storage formats that support nested structures.
/// </para>
/// <para>
/// <strong>Nested Structure Characteristics:</strong>
/// </para>
/// <list type="bullet">
/// <item>Collections: List&lt;T&gt;, Array, IEnumerable&lt;T&gt;, ICollection&lt;T&gt;, etc.</item>
/// <item>Nested objects: Properties that are custom classes or records</item>
/// <item>Dictionaries: Dictionary&lt;TKey, TValue&gt;, IDictionary, etc.</item>
/// <item>Complex hierarchical structures</item>
/// </list>
/// <para>
/// <strong>Compatible Storage Formats:</strong>
/// </para>
/// <list type="bullet">
/// <item>JSON files (preserves hierarchy)</item>
/// <item>Parquet files (supports nested columns)</item>
/// <item>XML files (hierarchical by nature)</item>
/// <item>Document databases (MongoDB, etc.)</item>
/// </list>
/// <para>
/// <strong>Incompatible Storage Formats:</strong>
/// </para>
/// <list type="bullet">
/// <item>❌ CSV files - cannot represent nested structures</item>
/// <item>❌ Excel files - limited nesting support, loses structure</item>
/// <item>❌ Flat relational tables - requires denormalization</item>
/// </list>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <para>
/// This marker interface serves multiple purposes:
/// </para>
/// <list type="number">
/// <item><strong>Self-Documentation:</strong> Clearly signals schema complexity</item>
/// <item><strong>Intent Declaration:</strong> Distinguishes "happens to be flat" from "designed to be flat"</item>
/// <item><strong>Migration Safety:</strong> Identifies schemas that cannot use flat formats</item>
/// <item><strong>Future Validation:</strong> Enables build-time checks via analyzers</item>
/// </list>
/// <para>
/// <strong>Relationship with IFlatSchema:</strong>
/// </para>
/// <para>
/// These interfaces are mutually exclusive. A schema should implement exactly one:
/// </para>
/// <list type="bullet">
/// <item><see cref="IFlatSchema"/> - All primitive properties</item>
/// <item><see cref="INestedSchema"/> - Contains collections or nested objects</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // ✅ Nested schema with collection
/// public sealed record CrossValidationResults(
///     List&lt;FoldMetric&gt; FoldMetrics,  // Nested collection
///     double MeanR2Score,
///     int NumFolds
/// ) : INestedSchema, IStructuredSerializable;
///
/// // ✅ Nested schema with nested object
/// public sealed record CustomerOrder(
///     string OrderId,
///     CustomerInfo Customer,  // Nested object
///     DateTime OrderDate
/// ) : INestedSchema, IStructuredSerializable;
///
/// // Individual nested type can be flat
/// public sealed record FoldMetric(
///     int FoldNumber,
///     double R2Score
/// ) : IFlatSchema, ITextSerializable;
/// </code>
/// </example>
public interface INestedSchema
{
  // Marker interface - no members required
}
