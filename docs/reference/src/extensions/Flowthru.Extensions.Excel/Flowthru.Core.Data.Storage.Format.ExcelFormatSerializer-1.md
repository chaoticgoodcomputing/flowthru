# <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1"></a> Class ExcelFormatSerializer<TRow\>

Namespace: [Flowthru.Core.Data.Storage.Format](Flowthru.Core.Data.Storage.Format.md)  
Assembly: Flowthru.Extensions.Excel.dll  

Serializes flat schemas to/from Excel (.xlsx) files using ExcelDataReader.

```csharp
public sealed class ExcelFormatSerializer<TRow> : IFormatSerializer<TRow>, IFormatRowReader<TRow>, IFormatRowWriter<TRow>, IFormatBase<TRow> where TRow : notnull, IFlatSchema, ITextSerializable
```

#### Type Parameters

`TRow` 

Row type (must be flat and text-serializable)

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ExcelFormatSerializer<TRow\>](Flowthru.Core.Data.Storage.Format.ExcelFormatSerializer\-1.md)

#### Implements

IFormatSerializer<TRow\>, 
IFormatRowReader<TRow\>, 
IFormatRowWriter<TRow\>, 
IFormatBase<TRow\>

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

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1_RowFeatures"></a> RowFeatures

Row-shape capabilities this format supports. Defaults to all-false; format
implementations override the property to declare honestly which features round-trip.

```csharp
public FormatRowFeatures RowFeatures { get; }
```

#### Property Value

 FormatRowFeatures

#### Remarks

<p>
Companion to <xref href="Flowthru.Core.Data.Storage.IFormatBase%601.Traits" data-throw-if-not-resolved="false"></xref>. Where <xref href="Flowthru.Core.Data.Storage.IFormatBase%601.Traits" data-throw-if-not-resolved="false"></xref> describes
medium-level capabilities (read/write, streaming, transactional),
<xref href="Flowthru.Core.Data.Storage.IFormatBase%601.RowFeatures" data-throw-if-not-resolved="false"></xref> describes which row-shape features the format honors
(<xref href="Flowthru.Core.Abstractions.IScalar" data-throw-if-not-resolved="false"></xref> NewType wrappers,
<xref href="Flowthru.Core.Abstractions.INestedSchema" data-throw-if-not-resolved="false"></xref> structures, etc.).
</p>
<p>
The kit's <code>FormatSerializerConformance&lt;TRow&gt;</code> consults these flags to gate
fixtures: when <xref href="Flowthru.Core.Data.Capabilities.FormatRowFeatures.SupportsIScalar" data-throw-if-not-resolved="false"></xref> is <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a>,
the IScalar fixture for that format skips with an explanatory message rather than
failing. When the flag is <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a>, the fixture must round-trip
successfully or the test fails.
</p>
<p>
The default-interface-method returns <code>new FormatRowFeatures()</code> (all false) —
a format that doesn't override is reported as supporting only the universal feature
surface in the capability matrix.
</p>

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1_Traits"></a> Traits

Structural capabilities of this format serializer.

```csharp
public StorageTraits Traits { get; }
```

#### Property Value

 StorageTraits

#### Remarks

Format traits focus on HOW data is serialized and whether it supports streaming.
For composed adapters, these traits are merged with medium and container traits.
Most formats should declare <code>CanStream = true</code> if they can deserialize row-by-row
without buffering the entire stream (e.g., CSV, Parquet). Formats that require full
parsing before yielding rows (e.g., JSON arrays) should set <code>CanStream = false</code>.

## Methods

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1_DeserializeRows_System_IO_Stream_"></a> DeserializeRows\(Stream\)

Deserializes a stream of bytes into a stream of rows.

```csharp
public IAsyncEnumerable<TRow> DeserializeRows(Stream stream)
```

#### Parameters

`stream` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

The stream containing serialized data.

#### Returns

 [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<TRow\>

Async enumerable of deserialized rows.

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1_GetPropertyMappingConfiguration"></a> GetPropertyMappingConfiguration\(\)

Configures how this serializer handles property-to-field name mapping for the schema.

```csharp
public PropertyMappingConfiguration GetPropertyMappingConfiguration()
```

#### Returns

 PropertyMappingConfiguration

Property mapping configuration describing the mapping strategy.

#### Remarks

<p>
<strong>Contractual Obligation:</strong> Every format implementor MUST implement this
method to explicitly declare how it handles property name mapping.
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

### <a id="Flowthru_Core_Data_Storage_Format_ExcelFormatSerializer_1_SerializeRows_System_IO_Stream_System_Collections_Generic_IAsyncEnumerable__0__"></a> SerializeRows\(Stream, IAsyncEnumerable<TRow\>\)

Serializes a stream of rows into a stream of bytes.

```csharp
public Task SerializeRows(Stream stream, IAsyncEnumerable<TRow> rows)
```

#### Parameters

`stream` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

The stream to write serialized data to.

`rows` [IAsyncEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable\-1)<TRow\>

The rows to serialize.

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)

Task that completes when serialization finishes.

