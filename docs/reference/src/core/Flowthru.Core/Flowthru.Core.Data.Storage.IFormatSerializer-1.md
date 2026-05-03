# <a id="Flowthru_Core_Data_Storage_IFormatSerializer_1"></a> Interface IFormatSerializer<TRow\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Interface for format serialization - handles row-based serialization/deserialization.

```csharp
public interface IFormatSerializer<TRow> where TRow : notnull
```

#### Type Parameters

`TRow` 

The row type (schema) to serialize

## Examples

<pre><code class="lang-csharp">// CSV serializer with flat schema constraint
var csvSerializer = new CsvFormatSerializer&lt;CompanySchema&gt;();

// Deserialize from stream to rows
await foreach (var row in csvSerializer.DeserializeRows(stream))
{
    Console.WriteLine($"Company: {row.Name}");
}

// Serialize rows to stream
await csvSerializer.SerializeRows(stream, rows);</code></pre>

## Remarks

<p>
<strong>Responsibility:</strong> Abstract HOW data is serialized (CSV, JSON, Parquet, etc.).
</p>
<p>
<strong>Separation of Concerns:</strong>
</p>
<p>
The format serializer is isolated from:
- Storage location (file vs memory) - handled by <xref href="Flowthru.Core.Data.Storage.IStorageMedium" data-throw-if-not-resolved="false"></xref>
- Container type (IEnumerable vs IDataView) - handled by <xref href="Flowthru.Core.Data.Storage.IContainerAdapter%602" data-throw-if-not-resolved="false"></xref>
</p>
<p>
<strong>Streaming Design:</strong>
</p>
<p>
Uses <xref href="System.Collections.Generic.IAsyncEnumerable%601" data-throw-if-not-resolved="false"></xref> for row streaming to:
- Support large datasets without loading everything into memory
- Enable backpressure and cancellation
- Allow format-agnostic streaming pipelines
</p>
<p>
<strong>Type Constraints:</strong>
</p>
<p>
The <code>notnull</code> constraint ensures all format serializers support modern C# patterns:
</p>
<ul><li><strong>Traditional schemas:</strong> Classes/records with parameterless constructors</li><li><strong>Required members:</strong> C# 11+ schemas with <code>required</code> properties</li><li><strong>Positional records:</strong> Records with primary constructors</li></ul>
<p>
This constraint explicitly prohibits the <code>new()</code> constraint, which is incompatible
with required members and positional records. Format serializers must use
<xref href="Flowthru.Core.Data.Storage.SchemaActivator" data-throw-if-not-resolved="false"></xref> or equivalent techniques to instantiate schemas.
</p>
<p>
Format serializers may add additional constraints for format-specific requirements:
</p>
<pre><code class="lang-csharp">public class CsvFormatSerializer&lt;T&gt; : IFormatSerializer&lt;T&gt;
    where T : notnull, IFlatSchema, ITextSerializable
{
    // Compile-time enforcement: notnull + flat + text serializable
}</code></pre>
<p>
<strong>Design Pattern:</strong>
</p>
<p>
This is the middle layer in the composition pattern:
</p>
<pre><code class="lang-csharp">Medium (bytes) → Format (rows) → Container (in-memory)
Stream         → IAsyncEnumerable&lt;TRow&gt; → IEnumerable&lt;TRow&gt;</code></pre>

## Properties

### <a id="Flowthru_Core_Data_Storage_IFormatSerializer_1_Traits"></a> Traits

Structural capabilities of this format serializer.

```csharp
StorageTraits Traits { get; }
```

#### Property Value

 [StorageTraits](Flowthru.Core.Data.Capabilities.StorageTraits.md)

#### Remarks

<p>
Format traits focus on HOW data is serialized and whether it supports streaming.
For composed adapters, these traits are merged with medium and container traits.
</p>
<p>
Most formats should declare <code>CanStream = true</code> if they can deserialize row-by-row
without buffering the entire stream (e.g., CSV, JSONL). Formats that require full
parsing before yielding rows (e.g., JSON arrays) should set <code>CanStream = false</code>.
</p>

## Methods

### <a id="Flowthru_Core_Data_Storage_IFormatSerializer_1_DeserializeRows_System_IO_Stream_"></a> DeserializeRows\(Stream\)

Deserializes a stream of bytes into a stream of rows.

```csharp
IAsyncEnumerable<TRow> DeserializeRows(Stream stream)
```

#### Parameters

`stream` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

The stream containing serialized data

#### Returns

 [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<TRow\>

Async enumerable of deserialized rows

#### Remarks

<p>
<strong>Streaming Behavior:</strong>
</p>
<p>
Rows should be yielded as they are deserialized (lazy evaluation).
This allows processing large datasets without loading everything into memory.
</p>
<p>
<strong>Error Handling:</strong>
</p>
<p>
Deserialization errors should throw exceptions:
- Format exceptions (malformed CSV, invalid JSON)
- Schema mismatches (missing columns, type conversion failures)
- I/O errors during stream reading
</p>
<p>
The caller should handle these exceptions appropriately.
</p>

### <a id="Flowthru_Core_Data_Storage_IFormatSerializer_1_GetPropertyMappingConfiguration"></a> GetPropertyMappingConfiguration\(\)

Configures how this serializer handles property-to-field name mapping for the schema.

```csharp
PropertyMappingConfiguration GetPropertyMappingConfiguration()
```

#### Returns

 [PropertyMappingConfiguration](Flowthru.Core.Data.Storage.PropertyMappingConfiguration.md)

Property mapping configuration describing the mapping strategy

#### Examples

<pre><code class="lang-csharp">// CSV serializer using SerializedLabel
public PropertyMappingConfiguration GetPropertyMappingConfiguration()
    =&gt; PropertyMappingConfiguration.FromSerializedLabel&lt;TRow&gt;();

// Parquet with library-controlled mapping
public PropertyMappingConfiguration GetPropertyMappingConfiguration()
    =&gt; PropertyMappingConfiguration.LibraryControlled();</code></pre>

#### Remarks

<p>
<strong>Contractual Obligation:</strong> Every format serializer MUST implement this method
to explicitly declare how it handles property name mapping.
</p>
<p>
<strong>Implementation Strategies:</strong>
</p>
<ul><li>
<strong>SerializedLabel:</strong> Use <xref href="Flowthru.Core.Data.Storage.Format.PropertyMappingHelper" data-throw-if-not-resolved="false"></xref>
to respect <code>[SerializedLabel]</code> attributes. Return
<xref href="Flowthru.Core.Data.Storage.PropertyMappingConfiguration.FromSerializedLabel%60%601" data-throw-if-not-resolved="false"></xref>.
</li><li>
<strong>LibraryControlled:</strong> The underlying library handles mapping with no
programmatic API; property names must match storage field names exactly. Return
<xref href="Flowthru.Core.Data.Storage.PropertyMappingConfiguration.LibraryControlled(System.String)" data-throw-if-not-resolved="false"></xref>.
</li></ul>
<p>
<strong>Design Intent:</strong> This contract makes property mapping an explicit, discoverable
capability rather than an implicit behavior, enabling:
</p>
<ul><li>Runtime introspection of mapping capabilities</li><li>Better error messages when schemas don't match storage</li><li>Documentation generation for serializer capabilities</li><li>Testing framework validation of mapping correctness</li></ul>

### <a id="Flowthru_Core_Data_Storage_IFormatSerializer_1_SerializeRows_System_IO_Stream_System_Collections_Generic_IAsyncEnumerable__0__"></a> SerializeRows\(Stream, IAsyncEnumerable<TRow\>\)

Serializes a stream of rows into a stream of bytes.

```csharp
Task SerializeRows(Stream stream, IAsyncEnumerable<TRow> rows)
```

#### Parameters

`stream` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

The stream to write serialized data to

`rows` [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<TRow\>

The rows to serialize

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)

Task that completes when serialization finishes

#### Remarks

<p>
<strong>Streaming Behavior:</strong>
</p>
<p>
Rows should be written as they are enumerated (lazy evaluation).
This allows handling large datasets efficiently.
</p>
<p>
<strong>Format-Specific Headers:</strong>
</p>
<p>
Implementations should handle format-specific initialization:
- CSV: Write header row with column names
- JSON: Write opening bracket for array
- Parquet: Write schema metadata
</p>
<p>
<strong>Error Handling:</strong>
</p>
<p>
Serialization errors should throw exceptions:
- Type conversion failures
- I/O errors during stream writing
- Invalid data values for format constraints
</p>

