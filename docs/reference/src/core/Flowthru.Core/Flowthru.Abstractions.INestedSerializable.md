# <a id="Flowthru_Abstractions_INestedSerializable"></a> Interface INestedSerializable

Namespace: [Flowthru.Abstractions](Flowthru.Abstractions.md)  
Assembly: Flowthru.Core.dll  

Marker interface for schema types that contain nested structures or collections.

```csharp
public interface INestedSerializable
```

## Examples

<pre><code class="lang-csharp">// ✅ Nested schema - requires hierarchical storage
public record CrossValidationResults : INestedSerializable
{
    // Collection of nested objects
    public List&lt;FoldMetric&gt; FoldMetrics { get; init; } = new();

    // Flat properties are fine in nested schemas
    public double MeanR2Score { get; init; }
    public double StdDevR2Score { get; init; }
    public int NumFolds { get; init; }
}

public record FoldMetric : IFlatSerializable  // Individual fold is flat
{
    public int FoldNumber { get; init; }
    public double R2Score { get; init; }
    public double MeanAbsoluteError { get; init; }
}

// ✅ Correct: Use JSON for nested data
catalog.CrossValidationResults = CreateJsonObject&lt;CrossValidationResults&gt;(
    "cross_validation_results",
    "model_output/cross_validation_results.json");

// ❌ Compile error if attempted with CSV (prevented by IFlatSerializable constraint)
// catalog.CrossValidationResults = CreateCsvDataset&lt;CrossValidationResults&gt;(...);</code></pre>

## Remarks

<p>
<strong>Purpose:</strong> Documents that a schema contains hierarchical data and
requires storage formats that support nested structures (JSON, Parquet, XML, etc.).
</p>
<p>
<strong>What qualifies as "nested"?</strong>
</p>
<p>
A schema is considered nested if it contains one or more of:
</p>
<ul><li>Collections (List&lt;T&gt;, Array, IEnumerable&lt;T&gt;, ICollection&lt;T&gt;, etc.)</li><li>Nested objects (properties that are custom classes or records)</li><li>Dictionaries (Dictionary&lt;TKey, TValue&gt;, IDictionary, etc.)</li><li>Complex hierarchical structures</li></ul>
<p>
<strong>Compatible Storage Formats:</strong>
</p>
<ul><li>JSON files (JsonCatalogDataset&lt;T&gt;, JsonCatalogObject&lt;T&gt;)</li><li>Parquet files (ParquetCatalogDataset&lt;T&gt; - supports nested columns)</li><li>XML files (if implemented)</li><li>Document databases (MongoDB, etc.)</li></ul>
<p>
<strong>Incompatible Storage Formats:</strong>
</p>
<ul><li>❌ CSV files - cannot represent nested structures</li><li>❌ Excel files - limited nesting support, loses structure</li><li>❌ Relational database tables - requires denormalization or separate tables</li></ul>
<p>
<strong>Design Rationale:</strong>
</p>
<p>
While this interface is not enforced by generic constraints (since nested-compatible
formats like JSON can also handle flat data), it serves important documentation and
validation purposes:
</p>
<ol><li><strong>Self-Documentation:</strong> Clearly signals schema complexity to developers</li><li><strong>Future Analyzer Support:</strong> Enables build-time validation via Roslyn analyzers</li><li><strong>Intent Declaration:</strong> Distinguishes "happens to be flat" from "designed to be flat"</li><li><strong>Migration Safety:</strong> Helps identify schemas that cannot be migrated to flat formats</li></ol>
<p>
<strong>Relationship with IFlatSerializable:</strong>
</p>
<p>
These interfaces are mutually exclusive. A schema should implement exactly one:
</p>
<ul><li><xref href="Flowthru.Abstractions.IFlatSerializable" data-throw-if-not-resolved="false"></xref> - All primitive properties, CSV-compatible</li><li><xref href="Flowthru.Abstractions.INestedSerializable" data-throw-if-not-resolved="false"></xref> - Contains collections or nested objects</li></ul>

