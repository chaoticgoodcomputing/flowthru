# <a id="Flowthru_Abstractions_IScalar"></a> Interface IScalar

Namespace: [Flowthru.Abstractions](Flowthru.Abstractions.md)  
Assembly: Flowthru.Core.dll  

Marker interface for types that serialize to a single primitive value —
i.e., they produce <code>"key": value</code> in JSON, not <code>"key": {...}</code> or <code>"key": [...]</code>.

```csharp
public interface IScalar
```

## Examples

<pre><code class="lang-csharp">// ✅ Single-value wrapper — safe to implement IScalar
public readonly record struct CustomerId(string Value) : IScalar;

// ✅ Schema using the NewType is classified as flat
[FlowthruSchema]
public partial record OrderSchema
{
    public required CustomerId Id { get; init; }
    public required string Name { get; init; }
}

// ❌ Multi-property struct — NOT a scalar, do not implement IScalar
public readonly record struct Address(string Street, string City) /* : IScalar — wrong */;</code></pre>

## Remarks

<p>
<strong>Purpose:</strong> Enables user-defined NewTypes and value-object wrappers to participate
in flat schema classification. Without this interface, the source generator cannot distinguish
a <code>CustomerId</code> wrapping a <code>string</code> from a nested object requiring <code>{...}</code>
serialization.
</p>
<p>
<strong>The JSON Test:</strong>
</p>
<p>
The definitive question when implementing this interface is: does your type serialize to a
single JSON value? A type is a flat scalar if and only if it produces one of:
</p>
<ul><li><code>"key": 42</code> — numeric</li><li><code>"key": "abc"</code> — string</li><li><code>"key": true</code> — boolean</li><li><code>"key": null</code> — null</li></ul>
<p>
If your type requires <code>"key": { "inner": ... }</code> or <code>"key": [...]</code>, it is NOT a
scalar and implementing this interface would misrepresent its structure, causing silent data
loss or serialization failures in flat formats like CSV.
</p>
<p>
<strong>Typical Use Cases:</strong>
</p>
<ul><li>NewType / value-object wrappers: <code>record CustomerId(string Value) : IScalar</code></li><li>Strong-typed identifiers backed by a primitive: <code>record OrderRef(Guid Id) : IScalar</code></li><li>Domain primitives that round-trip through a single string or numeric field</li></ul>
<p>
<strong>What this is NOT for:</strong>
</p>
<ul><li>Types with multiple public properties — those are nested objects</li><li>Collections or dictionaries</li><li>BCL types like <code>Guid</code>, <code>DateTime</code>, <code>TimeSpan</code> — those are recognized
automatically by the source generator as known flat scalars</li></ul>
<p>
<strong>Relationship with <xref href="Flowthru.Abstractions.IFlatSchema" data-throw-if-not-resolved="false"></xref>:</strong>
</p>
<p>
<xref href="Flowthru.Abstractions.IFlatSchema" data-throw-if-not-resolved="false"></xref> marks a <em>row type</em> — a schema whose properties are all
scalars. <xref href="Flowthru.Abstractions.IScalar" data-throw-if-not-resolved="false"></xref> marks a <em>property type</em> — a single value that can
appear as a column in a flat row. Nesting a flat row inside another row is still nesting.
</p>

