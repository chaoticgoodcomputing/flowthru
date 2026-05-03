# <a id="Flowthru_Core_Data_Storage_Format_ParquetFormatSerializer_1"></a> Class ParquetFormatSerializer<TRow\>

Namespace: [Flowthru.Core.Data.Storage.Format](Flowthru.Core.Data.Storage.Format.md)  
Assembly: Flowthru.Extensions.Parquet.dll  

Format serializer for Parquet (columnar storage) files using adapter pattern.

```csharp
[OptOutOfPropertyPlanner("Parquet's runtime DTO synthesis via System.Reflection.Emit is structurally different from the reflection walks PropertyMappingPlanner subsumes for CSV/Excel/JSON. Migrating Parquet to consume the planner is a deliberate follow-up effort outside Phase B's scope — it requires reworking how the typed DTO is built per-row from PropertyBinding metadata. The capability matrix surfaces this opt-out under 'manual mapping' so reviewers and end users see the gap explicitly.")]
public sealed class ParquetFormatSerializer<TRow> : IFormatSerializer<TRow> where TRow : notnull, IFlatSchema, IBinarySerializable
```

#### Type Parameters

`TRow` 

The Flowthru schema type

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ParquetFormatSerializer<TRow\>](Flowthru.Core.Data.Storage.Format.ParquetFormatSerializer\-1.md)

#### Implements

IFormatSerializer<TRow\>

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
<strong>Architecture:</strong>
</p>
<p>
Converts between Flowthru schemas (with required members) and Parquet-compatible DTOs:
</p>
<pre><code class="lang-csharp">Serialize:   TRow (required members) → DTO (parameterless ctor) → Parquet
Deserialize: Parquet → DTO (parameterless ctor) → TRow (required members)</code></pre>
<p>
<strong>Features:</strong>
</p>
<ul><li>SerializedLabel - Respects [SerializedLabel] attributes for property name mapping</li><li>Null Safety - Enforces non-nullable contracts during deserialization</li><li>Value Type Nullability - DTOs use nullable value types to match Parquet schema conventions</li><li>Enum Support - Automatically converts between Parquet's integer storage and enum types</li><li>Row group streaming - Writes in bounded batches (default 1M rows/group); peak write memory
is bounded to one row group regardless of total dataset size.</li></ul>
<p>
<strong>Current Limitations:</strong>
</p>
<ul><li>SerializedEnum attributes are not used - enums stored/retrieved by underlying integer value</li><li>Per-column encoding hints require Parquet.Net v6 (not yet on NuGet); use
<xref href="Flowthru.Core.Data.ParquetItemOptions%601.UseDictionaryEncoding" data-throw-if-not-resolved="false"></xref> as a global flag in the meantime.</li></ul>

## Constructors

### <a id="Flowthru_Core_Data_Storage_Format_ParquetFormatSerializer_1__ctor"></a> ParquetFormatSerializer\(\)

Initializes a new instance with default production-ready options.

```csharp
public ParquetFormatSerializer()
```

### <a id="Flowthru_Core_Data_Storage_Format_ParquetFormatSerializer_1__ctor_Flowthru_Core_Data_ParquetItemOptions__0__"></a> ParquetFormatSerializer\(ParquetItemOptions<TRow\>?\)

Initializes a new instance with caller-supplied tuning options.

```csharp
public ParquetFormatSerializer(ParquetItemOptions<TRow>? options)
```

#### Parameters

`options` [ParquetItemOptions](Flowthru.Core.Data.ParquetItemOptions\-1.md)<TRow\>?

## Properties

### <a id="Flowthru_Core_Data_Storage_Format_ParquetFormatSerializer_1_Traits"></a> Traits

Structural capabilities of this format serializer.

```csharp
public StorageTraits Traits { get; }
```

#### Property Value

 StorageTraits

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

### <a id="Flowthru_Core_Data_Storage_Format_ParquetFormatSerializer_1_DeserializeRows_System_IO_Stream_"></a> DeserializeRows\(Stream\)

Deserializes a stream of bytes into a stream of rows.

```csharp
public IAsyncEnumerable<TRow> DeserializeRows(Stream stream)
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

### <a id="Flowthru_Core_Data_Storage_Format_ParquetFormatSerializer_1_GetPropertyMappingConfiguration"></a> GetPropertyMappingConfiguration\(\)

Configures how this serializer handles property-to-field name mapping for the schema.

```csharp
public PropertyMappingConfiguration GetPropertyMappingConfiguration()
```

#### Returns

 PropertyMappingConfiguration

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
<strong>SerializedLabel:</strong> Consume <xref href="Flowthru.Core.Data.Serialization.PropertyMappingPlanner" data-throw-if-not-resolved="false"></xref>
to walk properties and resolve <code>[SerializedLabel]</code>-driven field names. Return
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

### <a id="Flowthru_Core_Data_Storage_Format_ParquetFormatSerializer_1_SerializeRows_System_IO_Stream_System_Collections_Generic_IAsyncEnumerable__0__"></a> SerializeRows\(Stream, IAsyncEnumerable<TRow\>\)

Serializes a stream of rows into a stream of bytes.

```csharp
public Task SerializeRows(Stream stream, IAsyncEnumerable<TRow> rows)
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

