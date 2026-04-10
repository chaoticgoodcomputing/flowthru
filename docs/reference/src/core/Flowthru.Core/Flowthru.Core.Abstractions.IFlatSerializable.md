# <a id="Flowthru_Core_Abstractions_IFlatSerializable"></a> Interface IFlatSerializable

Namespace: [Flowthru.Core.Abstractions](Flowthru.Core.Abstractions.md)  
Assembly: Flowthru.Core.dll  

Marker interface for schema types that contain only flat, primitive data.

```csharp
public interface IFlatSerializable
```

## Examples

<pre><code class="lang-csharp">// ✅ Flat schema - CSV compatible
public record CompanySchema : IFlatSerializable
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public decimal Rating { get; init; }
    public int FoundedYear { get; init; }
    public bool IataApproved { get; init; }
}

// ✅ Compiles successfully - flat schema with flat storage
catalog.Companies = CreateCsvDataset&lt;CompanySchema&gt;("companies", "companies.csv");

// ❌ Nested schema - requires JSON or Parquet
public record CrossValidationResults : INestedSerializable
{
    public List&lt;FoldMetric&gt; FoldMetrics { get; init; } = new();
    public double MeanR2Score { get; init; }
}

// ❌ Compile error: CrossValidationResults does not implement IFlatSerializable
catalog.Results = CreateCsvDataset&lt;CrossValidationResults&gt;("results", "results.csv");

// ✅ Correct: Use JSON for nested data
catalog.Results = CreateJsonObject&lt;CrossValidationResults&gt;("results", "results.json");</code></pre>

## Remarks

<p>
<strong>Purpose:</strong> Enables compile-time validation that flat file formats
(CSV, TSV, etc.) receive compatible schema types.
</p>
<p>
<strong>What qualifies as "flat"?</strong>
</p>
<p>
A schema is considered flat if all properties are:
</p>
<ul><li>Primitive types (int, long, double, decimal, bool, string, DateTime, DateTimeOffset, etc.)</li><li>Nullable primitives (int?, double?, bool?, etc.)</li><li>Enums (and nullable enums)</li><li>Value types that serialize to single values (Guid, TimeSpan, etc.)</li></ul>
<p>
A schema is NOT flat if it contains:
</p>
<ul><li>Collections (List&lt;T&gt;, Array, IEnumerable&lt;T&gt;, etc.)</li><li>Nested objects (custom class/record properties)</li><li>Dictionaries or other complex structures</li></ul>
<p>
<strong>Compatible Storage Formats:</strong>
</p>
<ul><li>CSV files (CsvCatalogDataset&lt;T&gt;)</li><li>Excel files (ExcelCatalogDataset&lt;T&gt;)</li><li>JSON files (JsonCatalogDataset&lt;T&gt; - also supports nested)</li><li>Parquet files (ParquetCatalogDataset&lt;T&gt; - also supports nested)</li><li>Database tables (flat row structure)</li></ul>
<p>
<strong>Design Rationale:</strong>
</p>
<p>
This marker interface follows Flowthru's philosophy of "fail at compile-time, not runtime."
By requiring flat-only storage formats to constrain their generic type parameter with
<code>where T : IFlatSerializable</code>, we catch schema-format mismatches during compilation
rather than discovering silent data loss or runtime serialization errors during execution.
</p>

