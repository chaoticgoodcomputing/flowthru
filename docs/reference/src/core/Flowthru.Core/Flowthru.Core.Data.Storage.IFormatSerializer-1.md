# <a id="Flowthru_Core_Data_Storage_IFormatSerializer_1"></a> Interface IFormatSerializer<TRow\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Format extension that supports both reading and writing — the composition of
<xref href="Flowthru.Core.Data.Storage.IFormatRowReader%601" data-throw-if-not-resolved="false"></xref> and <xref href="Flowthru.Core.Data.Storage.IFormatRowWriter%601" data-throw-if-not-resolved="false"></xref>.

```csharp
public interface IFormatSerializer<TRow> : IFormatRowReader<TRow>, IFormatRowWriter<TRow>, IFormatBase<TRow> where TRow : notnull
```

#### Type Parameters

`TRow` 

The row type the format handles.

#### Implements

[IFormatRowReader<TRow\>](Flowthru.Core.Data.Storage.IFormatRowReader\-1.md), 
[IFormatRowWriter<TRow\>](Flowthru.Core.Data.Storage.IFormatRowWriter\-1.md), 
[IFormatBase<TRow\>](Flowthru.Core.Data.Storage.IFormatBase\-1.md)

## Examples

<pre><code class="lang-csharp">// CSV serializer with flat schema constraint
var csv = new CsvFormatSerializer&lt;CompanySchema&gt;();

// Deserialize from stream to rows
await foreach (var row in csv.DeserializeRows(stream))
{
  Console.WriteLine($"Company: {row.Name}");
}

// Serialize rows to stream
await csv.SerializeRows(stream, rows);</code></pre>

## Remarks

<p>
<strong>Use this interface for write-capable formats.</strong> CSV, JSON, and
Parquet all implement <xref href="Flowthru.Core.Data.Storage.IFormatSerializer%601" data-throw-if-not-resolved="false"></xref>. Read-only formats —
Excel and the like — implement only <xref href="Flowthru.Core.Data.Storage.IFormatRowReader%601" data-throw-if-not-resolved="false"></xref>; their
inability to write is a compile-time fact, not a runtime trait check.
</p>
<p>
<strong>Generic constraints.</strong> Format extensions typically add format-
specific constraints on top of <code>TRow : notnull</code>:
</p>
<pre><code class="lang-csharp">public sealed class CsvFormatSerializer&lt;T&gt; : IFormatSerializer&lt;T&gt;
  where T : notnull, IFlatSchema, ITextSerializable
{
  // Compile-time enforcement: notnull + flat + text serializable
}</code></pre>
<p>
The <code>notnull</code> constraint deliberately prohibits the <code>new()</code> constraint,
which is incompatible with required members and positional records. Format
implementations use <xref href="Flowthru.Core.Data.Storage.SchemaActivator" data-throw-if-not-resolved="false"></xref> or equivalent for instantiation.
</p>
<p>
<strong>Layered design.</strong>
</p>
<pre><code class="lang-csharp">Medium (bytes) → Format (rows) → Container (in-memory)
Stream         → IAsyncEnumerable&lt;TRow&gt; → IEnumerable&lt;TRow&gt;</code></pre>
<p>
The segmented split (Phase D) lets <xref href="Flowthru.Core.Data.Storage.ComposedStorageAdapter%602" data-throw-if-not-resolved="false"></xref>
dispatch on interface presence: a Storage adapter built from
<xref href="Flowthru.Core.Data.Storage.IFormatRowReader%601" data-throw-if-not-resolved="false"></xref> alone exposes a read-only surface;
one built from a full <xref href="Flowthru.Core.Data.Storage.IFormatSerializer%601" data-throw-if-not-resolved="false"></xref> exposes both
directions.
</p>

