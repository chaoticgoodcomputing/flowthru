namespace Flowthru.Abstractions;

/// <summary>
/// Marker interface for schema types that contain nested structures or collections.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Documents that a schema contains hierarchical data and
/// requires storage formats that support nested structures (JSON, Parquet, XML, etc.).
/// </para>
/// <para>
/// <strong>What qualifies as "nested"?</strong>
/// </para>
/// <para>
/// A schema is considered nested if it contains one or more of:
/// </para>
/// <list type="bullet">
/// <item>Collections (List&lt;T&gt;, Array, IEnumerable&lt;T&gt;, ICollection&lt;T&gt;, etc.)</item>
/// <item>Nested objects (properties that are custom classes or records)</item>
/// <item>Dictionaries (Dictionary&lt;TKey, TValue&gt;, IDictionary, etc.)</item>
/// <item>Complex hierarchical structures</item>
/// </list>
/// <para>
/// <strong>Compatible Storage Formats:</strong>
/// </para>
/// <list type="bullet">
/// <item>JSON files (JsonCatalogDataset&lt;T&gt;, JsonCatalogObject&lt;T&gt;)</item>
/// <item>Parquet files (ParquetCatalogDataset&lt;T&gt; - supports nested columns)</item>
/// <item>XML files (if implemented)</item>
/// <item>Document databases (MongoDB, etc.)</item>
/// </list>
/// <para>
/// <strong>Incompatible Storage Formats:</strong>
/// </para>
/// <list type="bullet">
/// <item>❌ CSV files - cannot represent nested structures</item>
/// <item>❌ Excel files - limited nesting support, loses structure</item>
/// <item>❌ Relational database tables - requires denormalization or separate tables</item>
/// </list>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <para>
/// While this interface is not enforced by generic constraints (since nested-compatible
/// formats like JSON can also handle flat data), it serves important documentation and
/// validation purposes:
/// </para>
/// <list type="number">
/// <item><strong>Self-Documentation:</strong> Clearly signals schema complexity to developers</item>
/// <item><strong>Future Analyzer Support:</strong> Enables build-time validation via Roslyn analyzers</item>
/// <item><strong>Intent Declaration:</strong> Distinguishes "happens to be flat" from "designed to be flat"</item>
/// <item><strong>Migration Safety:</strong> Helps identify schemas that cannot be migrated to flat formats</item>
/// </list>
/// <para>
/// <strong>Relationship with IFlatSerializable:</strong>
/// </para>
/// <para>
/// These interfaces are mutually exclusive. A schema should implement exactly one:
/// </para>
/// <list type="bullet">
/// <item><see cref="IFlatSerializable"/> - All primitive properties, CSV-compatible</item>
/// <item><see cref="INestedSerializable"/> - Contains collections or nested objects</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // ✅ Nested schema - requires hierarchical storage
/// public record CrossValidationResults : INestedSerializable
/// {
///     // Collection of nested objects
///     public List&lt;FoldMetric&gt; FoldMetrics { get; init; } = new();
///     
///     // Flat properties are fine in nested schemas
///     public double MeanR2Score { get; init; }
///     public double StdDevR2Score { get; init; }
///     public int NumFolds { get; init; }
/// }
/// 
/// public record FoldMetric : IFlatSerializable  // Individual fold is flat
/// {
///     public int FoldNumber { get; init; }
///     public double R2Score { get; init; }
///     public double MeanAbsoluteError { get; init; }
/// }
/// 
/// // ✅ Correct: Use JSON for nested data
/// catalog.CrossValidationResults = CreateJsonObject&lt;CrossValidationResults&gt;(
///     "cross_validation_results",
///     "model_output/cross_validation_results.json");
/// 
/// // ❌ Compile error if attempted with CSV (prevented by IFlatSerializable constraint)
/// // catalog.CrossValidationResults = CreateCsvDataset&lt;CrossValidationResults&gt;(...);
/// </code>
/// </example>
public interface INestedSerializable {
  // Marker interface - no members required
}
