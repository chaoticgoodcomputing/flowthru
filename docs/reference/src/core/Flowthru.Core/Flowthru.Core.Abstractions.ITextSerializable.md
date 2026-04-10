# <a id="Flowthru_Core_Abstractions_ITextSerializable"></a> Interface ITextSerializable

Namespace: [Flowthru.Core.Abstractions](Flowthru.Core.Abstractions.md)  
Assembly: Flowthru.Core.dll  

Marker interface for schema types that can be serialized to text-based formats (CSV, TSV).

```csharp
public interface ITextSerializable
```

## Examples

<pre><code class="lang-csharp">// ✅ Flat schema with text serialization
public sealed record CompanySchema(
    int Id,
    string Name,
    float Rating
) : IFlatSchema, ITextSerializable;

// ✅ Flat schema with multiple serialization capabilities
public sealed record DataRow(
    DateTime Timestamp,
    double Value,
    string Category
) : IFlatSchema, ITextSerializable, IBinarySerializable;

// ❌ Cannot be text serialized - nested structure
public sealed record NestedData(
    string Id,
    List&lt;string&gt; Tags  // Collection!
) : INestedSchema, IStructuredSerializable;  // Not ITextSerializable</code></pre>

## Remarks

<p>
<strong>Purpose:</strong> Indicates a schema is compatible with text-based, flat file
formats that represent data in rows and columns.
</p>
<p>
<strong>Compatible Formats:</strong>
</p>
<ul><li>CSV (Comma-Separated Values)</li><li>TSV (Tab-Separated Values)</li><li>Other delimited text formats</li></ul>
<p>
<strong>Requirements:</strong>
</p>
<p>
Schemas implementing this interface must:
</p>
<ul><li>Also implement <xref href="Flowthru.Core.Abstractions.IFlatSchema" data-throw-if-not-resolved="false"></xref> (no nested structures)</li><li>Have properties that can be converted to/from string representation</li><li>Support single-value serialization per field</li></ul>
<p>
<strong>Design Rationale:</strong>
</p>
<p>
Separating serialization capability from structure allows:
- Compile-time enforcement: <code>CsvFormatSerializer&lt;T&gt; where T : IFlatSchema, ITextSerializable</code>
- Multiple serialization targets: A flat schema can be both text and binary serializable
- Clear documentation of supported formats
</p>
<p>
<strong>Typical Usage:</strong>
</p>
<p>
Most flat schemas should implement both <xref href="Flowthru.Core.Abstractions.ITextSerializable" data-throw-if-not-resolved="false"></xref> and
<xref href="Flowthru.Core.Abstractions.IBinarySerializable" data-throw-if-not-resolved="false"></xref> to support maximum format flexibility.
</p>

