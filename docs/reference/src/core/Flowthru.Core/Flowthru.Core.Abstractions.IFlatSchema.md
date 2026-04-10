# <a id="Flowthru_Core_Abstractions_IFlatSchema"></a> Interface IFlatSchema

Namespace: [Flowthru.Core.Abstractions](Flowthru.Core.Abstractions.md)  
Assembly: Flowthru.Core.dll  

Marker interface for schema types with flat (non-nested) structure.

```csharp
public interface IFlatSchema
```

## Examples

<pre><code class="lang-csharp">// ✅ Flat schema with multiple serialization capabilities
public sealed record CompanySchema(
    int Id,
    string Name,
    float Rating,
    bool IsActive
) : IFlatSchema, ITextSerializable, IBinarySerializable;

// ❌ Not flat - contains collection
public sealed record OrderSchema(
    string OrderId,
    List&lt;LineItem&gt; Items  // Collection = nested!
) : INestedSchema, IStructuredSerializable;</code></pre>

## Remarks

<p>
<strong>Purpose:</strong> Indicates a schema contains only primitive properties,
with no collections or nested objects.
</p>
<p>
<strong>Flat Structure Requirements:</strong>
</p>
<ul><li>All properties are primitives (int, long, double, decimal, bool, string, DateTime, etc.)</li><li>Nullable primitives (int?, double?, bool?, etc.) are allowed</li><li>Enums and nullable enums are allowed</li><li>Value types that serialize to single values (Guid, TimeSpan, etc.) are allowed</li></ul>
<p>
<strong>Not Flat (Use INestedSchema instead):</strong>
</p>
<ul><li>Collections: List&lt;T&gt;, Array, IEnumerable&lt;T&gt;, etc.</li><li>Nested objects: Custom class/record properties</li><li>Dictionaries or complex structures</li></ul>
<p>
<strong>Design Rationale:</strong>
</p>
<p>
Separating structural properties (flat vs nested) from serialization capabilities
(text/structured/binary) enables:
- Explicit schema documentation
- Compile-time format validation
- Format serializers can enforce appropriate constraints
- Clear intent about data structure complexity
</p>
<p>
<strong>Relationship with Serialization Markers:</strong>
</p>
<p>
A flat schema should also implement one or more serialization capability markers:
- <xref href="Flowthru.Core.Abstractions.ITextSerializable" data-throw-if-not-resolved="false"></xref> - Can serialize to CSV/TSV
- <xref href="Flowthru.Core.Abstractions.IStructuredSerializable" data-throw-if-not-resolved="false"></xref> - Can serialize to JSON/XML
- <xref href="Flowthru.Core.Abstractions.IBinarySerializable" data-throw-if-not-resolved="false"></xref> - Can serialize to Parquet/Avro
</p>

