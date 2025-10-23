namespace Flowthru.Abstractions;

/// <summary>
/// Marker interface for schema types that contain only flat, primitive data.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Enables compile-time validation that flat file formats
/// (CSV, TSV, etc.) receive compatible schema types.
/// </para>
/// <para>
/// <strong>What qualifies as "flat"?</strong>
/// </para>
/// <para>
/// A schema is considered flat if all properties are:
/// </para>
/// <list type="bullet">
/// <item>Primitive types (int, long, double, decimal, bool, string, DateTime, DateTimeOffset, etc.)</item>
/// <item>Nullable primitives (int?, double?, bool?, etc.)</item>
/// <item>Enums (and nullable enums)</item>
/// <item>Value types that serialize to single values (Guid, TimeSpan, etc.)</item>
/// </list>
/// <para>
/// A schema is NOT flat if it contains:
/// </para>
/// <list type="bullet">
/// <item>Collections (List&lt;T&gt;, Array, IEnumerable&lt;T&gt;, etc.)</item>
/// <item>Nested objects (custom class/record properties)</item>
/// <item>Dictionaries or other complex structures</item>
/// </list>
/// <para>
/// <strong>Compatible Storage Formats:</strong>
/// </para>
/// <list type="bullet">
/// <item>CSV files (CsvCatalogDataset&lt;T&gt;)</item>
/// <item>Excel files (ExcelCatalogDataset&lt;T&gt;)</item>
/// <item>JSON files (JsonCatalogDataset&lt;T&gt; - also supports nested)</item>
/// <item>Parquet files (ParquetCatalogDataset&lt;T&gt; - also supports nested)</item>
/// <item>Database tables (flat row structure)</item>
/// </list>
/// <para>
/// <strong>Design Rationale:</strong>
/// </para>
/// <para>
/// This marker interface follows Flowthru's philosophy of "fail at compile-time, not runtime."
/// By requiring flat-only storage formats to constrain their generic type parameter with
/// <c>where T : IFlatSerializable</c>, we catch schema-format mismatches during compilation
/// rather than discovering silent data loss or runtime serialization errors during execution.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // ✅ Flat schema - CSV compatible
/// public record CompanySchema : IFlatSerializable
/// {
///     public string Id { get; init; } = null!;
///     public string Name { get; init; } = null!;
///     public decimal Rating { get; init; }
///     public int FoundedYear { get; init; }
///     public bool IataApproved { get; init; }
/// }
/// 
/// // ✅ Compiles successfully - flat schema with flat storage
/// catalog.Companies = CreateCsvDataset&lt;CompanySchema&gt;("companies", "companies.csv");
/// 
/// // ❌ Nested schema - requires JSON or Parquet
/// public record CrossValidationResults : INestedSerializable
/// {
///     public List&lt;FoldMetric&gt; FoldMetrics { get; init; } = new();
///     public double MeanR2Score { get; init; }
/// }
/// 
/// // ❌ Compile error: CrossValidationResults does not implement IFlatSerializable
/// catalog.Results = CreateCsvDataset&lt;CrossValidationResults&gt;("results", "results.csv");
/// 
/// // ✅ Correct: Use JSON for nested data
/// catalog.Results = CreateJsonObject&lt;CrossValidationResults&gt;("results", "results.json");
/// </code>
/// </example>
public interface IFlatSerializable {
  // Marker interface - no members required
}
