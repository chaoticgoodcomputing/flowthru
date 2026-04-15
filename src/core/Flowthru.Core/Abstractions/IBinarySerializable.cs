namespace Flowthru.Core.Abstractions;

/// <summary>
/// Marker interface for schema types that can be serialized to columnar binary formats (Parquet, Avro).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Indicates a schema is compatible with efficient, columnar binary
/// formats designed for analytical workloads.
/// </para>
/// <para>
/// <strong>Compatible Formats:</strong>
/// </para>
/// <list type="bullet">
/// <item>Parquet (Apache Parquet - columnar storage)</item>
/// <item>Avro (Apache Avro - row-based but schema-aware)</item>
/// <item>ORC (Optimized Row Columnar - Hive format)</item>
/// <item>Arrow (Apache Arrow - in-memory columnar format)</item>
/// </list>
/// <para>
/// <strong>Requirements:</strong>
/// </para>
/// <para>
/// Most binary formats work best with flat schemas, though formats like Parquet
/// support nested columns:
/// </para>
/// <list type="bullet">
/// <item><strong>Flat schemas (<see cref="IFlatSchema"/>):</strong> Optimal - full columnar compression</item>
/// <item><strong>Nested schemas (<see cref="INestedSchema"/>):</strong> Supported but may lose some benefits</item>
/// </list>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <para>
/// Binary serialization offers significant advantages:
/// </para>
/// <list type="bullet">
/// <item><strong>Performance:</strong> Orders of magnitude faster read/write than CSV</item>
/// <item><strong>Compression:</strong> Columnar formats achieve excellent compression ratios</item>
/// <item><strong>Type Preservation:</strong> Native storage of numeric types (no string conversion)</item>
/// <item><strong>Predicate Pushdown:</strong> Read only required columns/rows</item>
/// <item><strong>Schema Evolution:</strong> Built-in schema versioning support</item>
/// </list>
/// <para>
/// <strong>When to Use:</strong>
/// </para>
/// <list type="bullet">
/// <item>Large datasets (&gt;10MB)</item>
/// <item>Analytical queries (aggregations, filters)</item>
/// <item>Data lake storage</item>
/// <item>Machine learning feature stores</item>
/// <item>Long-term data archival</item>
/// </list>
/// <para>
/// <strong>Trade-offs:</strong>
/// </para>
/// <list type="bullet">
/// <item>❌ Not human-readable (use JSON for debugging)</item>
/// <item>❌ Requires specialized libraries (not plain text editors)</item>
/// <item>✅ Superior performance for large-scale data</item>
/// <item>✅ Industry standard for big data ecosystems</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // ✅ Flat schema with binary serialization - optimal
/// public sealed record FeatureRow(
///     DateTime Timestamp,
///     double Feature1,
///     double Feature2,
///     int Label
/// ) : IFlatSchema, IBinarySerializable, ITextSerializable;
///
/// // ✅ Multiple format support for flexibility
/// public sealed record CompanySchema(
///     int Id,
///     string Name,
///     float Rating
/// ) : IFlatSchema, ITextSerializable, IBinarySerializable, IStructuredSerializable;
///
/// // ⚠️ Nested schema - binary serialization less optimal
/// public sealed record OrderWithItems(
///     string OrderId,
///     List&lt;LineItem&gt; Items  // Nested column in Parquet
/// ) : INestedSchema, IBinarySerializable, IStructuredSerializable;
/// </code>
/// </example>
public interface IBinarySerializable
{
  // Marker interface - no members required
}
