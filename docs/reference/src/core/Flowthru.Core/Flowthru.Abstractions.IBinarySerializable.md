# <a id="Flowthru_Abstractions_IBinarySerializable"></a> Interface IBinarySerializable

Namespace: [Flowthru.Abstractions](Flowthru.Abstractions.md)  
Assembly: Flowthru.Core.dll  

Marker interface for schema types that can be serialized to columnar binary formats (Parquet, Avro).

```csharp
public interface IBinarySerializable
```

## Examples

<pre><code class="lang-csharp">// ✅ Flat schema with binary serialization - optimal
public sealed record FeatureRow(
    DateTime Timestamp,
    double Feature1,
    double Feature2,
    int Label
) : IFlatSchema, IBinarySerializable, ITextSerializable;

// ✅ Multiple format support for flexibility
public sealed record CompanySchema(
    int Id,
    string Name,
    float Rating
) : IFlatSchema, ITextSerializable, IBinarySerializable, IStructuredSerializable;

// ⚠️ Nested schema - binary serialization less optimal
public sealed record OrderWithItems(
    string OrderId,
    List&lt;LineItem&gt; Items  // Nested column in Parquet
) : INestedSchema, IBinarySerializable, IStructuredSerializable;</code></pre>

## Remarks

<p>
<strong>Purpose:</strong> Indicates a schema is compatible with efficient, columnar binary
formats designed for analytical workloads.
</p>
<p>
<strong>Compatible Formats:</strong>
</p>
<ul><li>Parquet (Apache Parquet - columnar storage)</li><li>Avro (Apache Avro - row-based but schema-aware)</li><li>ORC (Optimized Row Columnar - Hive format)</li><li>Arrow (Apache Arrow - in-memory columnar format)</li></ul>
<p>
<strong>Requirements:</strong>
</p>
<p>
Most binary formats work best with flat schemas, though formats like Parquet
support nested columns:
</p>
<ul><li><strong>Flat schemas (<xref href="Flowthru.Abstractions.IFlatSchema" data-throw-if-not-resolved="false"></xref>):</strong> Optimal - full columnar compression</li><li><strong>Nested schemas (<xref href="Flowthru.Abstractions.INestedSchema" data-throw-if-not-resolved="false"></xref>):</strong> Supported but may lose some benefits</li></ul>
<p>
<strong>Design Rationale:</strong>
</p>
<p>
Binary serialization offers significant advantages:
</p>
<ul><li><strong>Performance:</strong> Orders of magnitude faster read/write than CSV</li><li><strong>Compression:</strong> Columnar formats achieve excellent compression ratios</li><li><strong>Type Preservation:</strong> Native storage of numeric types (no string conversion)</li><li><strong>Predicate Pushdown:</strong> Read only required columns/rows</li><li><strong>Schema Evolution:</strong> Built-in schema versioning support</li></ul>
<p>
<strong>When to Use:</strong>
</p>
<ul><li>Large datasets (&gt;10MB)</li><li>Analytical queries (aggregations, filters)</li><li>Data lake storage</li><li>Machine learning feature stores</li><li>Long-term data archival</li></ul>
<p>
<strong>Trade-offs:</strong>
</p>
<ul><li>❌ Not human-readable (use JSON for debugging)</li><li>❌ Requires specialized libraries (not plain text editors)</li><li>✅ Superior performance for large-scale data</li><li>✅ Industry standard for big data ecosystems</li></ul>

