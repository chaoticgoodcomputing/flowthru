# <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1"></a> Class ExcelFormatSerializer<TRow\>

Namespace: [Flowthru.Core.Data.Storage.Format](Flowthru.Core.Data.Storage.Format.md)  
Assembly: Flowthru.Extensions.Excel.dll  

Serializes flat schemas to/from Excel (.xlsx) files using ExcelDataReader.

```csharp
public sealed class ExcelFormatSerializer<TRow> : IFormatSerializer<TRow> where TRow : notnull, IFlatSchema, ITextSerializable
```

#### Type Parameters

`TRow` 

Row type (must be flat and text-serializable)

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ExcelFormatSerializer<TRow\>](Flowthru.Core.Data.Storage.Format.ExcelFormatSerializer\-1.md)

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
<strong>Read-Only:</strong> ExcelDataReader only supports reading Excel files.
Calling SerializeRows will throw NotSupportedException.
</p>
<p>
<strong>Sheet Selection:</strong> Reads from specified sheet name.
</p>
<p>
<strong>Null Handling:</strong> Empty cells (DBNull) deserialize to null for nullable
properties by default. Catalog authors can additionally treat specific string sentinels
as null via the <code>nullValues</code> constructor parameter — for example
<code>["", "NA", "N/A", "NULL"]</code> for messy spreadsheet exports. The override applies
only to properties declared nullable in the schema (<code>string?</code>, <code>int?</code>, etc.).
Non-nullable properties are unaffected.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1__ctor_System_String_"></a> ExcelFormatSerializer\(string\)

Initializes a new instance of the <xref href="Flowthru.Core.Data.Storage.Format.ExcelFormatSerializer%601" data-throw-if-not-resolved="false"></xref> class with
the specified sheet name. Empty cells in nullable properties deserialize to null.

```csharp
public ExcelFormatSerializer(string sheetName)
```

#### Parameters

`sheetName` [string](https://learn.microsoft.com/dotnet/api/system.string)

The name of the Excel sheet to read from.

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1__ctor_System_String_System_Collections_Generic_IReadOnlyList_System_String__"></a> ExcelFormatSerializer\(string, IReadOnlyList<string\>\)

Initializes a new instance with a custom set of null-representation strings.

```csharp
public ExcelFormatSerializer(string sheetName, IReadOnlyList<string> nullValues)
```

#### Parameters

`sheetName` [string](https://learn.microsoft.com/dotnet/api/system.string)

The name of the Excel sheet to read from.

`nullValues` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

Strings that should deserialize to null for nullable properties. Pass
<code>["", "NA", "N/A", "NULL"]</code> for pandas-style handling of messy exports.

## Fields

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1_DefaultNullValues"></a> DefaultNullValues

The default set of strings treated as null on read for nullable properties.

```csharp
public static readonly IReadOnlyList<string> DefaultNullValues
```

#### Field Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Properties

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1_NullValues"></a> NullValues

Gets the null-representation strings for this serializer.

```csharp
public IReadOnlyList<string> NullValues { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1_Traits"></a> Traits

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

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1_DeserializeRows_System_IO_Stream_"></a> DeserializeRows\(Stream\)

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

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1_GetPropertyMappingConfiguration"></a> GetPropertyMappingConfiguration\(\)

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

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1_SerializeRows_System_IO_Stream_System_Collections_Generic_IAsyncEnumerable__0__"></a> SerializeRows\(Stream, IAsyncEnumerable<TRow\>\)

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

