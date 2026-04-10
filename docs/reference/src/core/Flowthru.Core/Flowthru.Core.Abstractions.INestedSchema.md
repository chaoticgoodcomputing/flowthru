# <a id="Flowthru_Core_Abstractions_INestedSchema"></a> Interface INestedSchema

Namespace: [Flowthru.Core.Abstractions](Flowthru.Core.Abstractions.md)  
Assembly: Flowthru.Core.dll  

Marker interface for schema types with nested structure (collections or nested objects).

```csharp
public interface INestedSchema
```

## Examples

<pre><code class="lang-csharp">// ✅ Nested schema with collection
public sealed record CrossValidationResults(
    List&lt;FoldMetric&gt; FoldMetrics,  // Nested collection
    double MeanR2Score,
    int NumFolds
) : INestedSchema, IStructuredSerializable;

// ✅ Nested schema with nested object
public sealed record CustomerOrder(
    string OrderId,
    CustomerInfo Customer,  // Nested object
    DateTime OrderDate
) : INestedSchema, IStructuredSerializable;

// Individual nested type can be flat
public sealed record FoldMetric(
    int FoldNumber,
    double R2Score
) : IFlatSchema, ITextSerializable;</code></pre>

## Remarks

<p>
<strong>Purpose:</strong> Indicates a schema contains hierarchical data requiring
storage formats that support nested structures.
</p>
<p>
<strong>Nested Structure Characteristics:</strong>
</p>
<ul><li>Collections: List&lt;T&gt;, Array, IEnumerable&lt;T&gt;, ICollection&lt;T&gt;, etc.</li><li>Nested objects: Properties that are custom classes or records</li><li>Dictionaries: Dictionary&lt;TKey, TValue&gt;, IDictionary, etc.</li><li>Complex hierarchical structures</li></ul>
<p>
<strong>Compatible Storage Formats:</strong>
</p>
<ul><li>JSON files (preserves hierarchy)</li><li>Parquet files (supports nested columns)</li><li>XML files (hierarchical by nature)</li><li>Document databases (MongoDB, etc.)</li></ul>
<p>
<strong>Incompatible Storage Formats:</strong>
</p>
<ul><li>❌ CSV files - cannot represent nested structures</li><li>❌ Excel files - limited nesting support, loses structure</li><li>❌ Flat relational tables - requires denormalization</li></ul>
<p>
<strong>Design Rationale:</strong>
</p>
<p>
This marker interface serves multiple purposes:
</p>
<ol><li><strong>Self-Documentation:</strong> Clearly signals schema complexity</li><li><strong>Intent Declaration:</strong> Distinguishes "happens to be flat" from "designed to be flat"</li><li><strong>Migration Safety:</strong> Identifies schemas that cannot use flat formats</li><li><strong>Future Validation:</strong> Enables build-time checks via analyzers</li></ol>
<p>
<strong>Relationship with IFlatSchema:</strong>
</p>
<p>
These interfaces are mutually exclusive. A schema should implement exactly one:
</p>
<ul><li><xref href="Flowthru.Core.Abstractions.IFlatSchema" data-throw-if-not-resolved="false"></xref> - All primitive properties</li><li><xref href="Flowthru.Core.Abstractions.INestedSchema" data-throw-if-not-resolved="false"></xref> - Contains collections or nested objects</li></ul>

