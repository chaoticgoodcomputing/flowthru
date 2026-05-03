# <a id="Flowthru_Core_Data_Storage_Format_CsvFormatSerializer_1"></a> Class CsvFormatSerializer<TRow\>

Namespace: [Flowthru.Core.Data.Storage.Format](Flowthru.Core.Data.Storage.Format.md)  
Assembly: Flowthru.Extensions.Csv.dll  

Format serializer for CSV (Comma-Separated Values) files.

```csharp
public sealed class CsvFormatSerializer<TRow> : IFormatSerializer<TRow>, IFormatRowReader<TRow>, IFormatRowWriter<TRow>, IFormatBase<TRow> where TRow : notnull, IFlatSchema, ITextSerializable
```

#### Type Parameters

`TRow` 

The row schema type

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CsvFormatSerializer<TRow\>](Flowthru.Core.Data.Storage.Format.CsvFormatSerializer\-1.md)

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

## Examples

<pre><code class="lang-csharp">public record CompanySchema(
    int Id,
    string Name,
    float Rating
) : IFlatSchema, ITextSerializable;

var serializer = new CsvFormatSerializer&lt;CompanySchema&gt;();

// Deserialize
using var readStream = File.OpenRead("companies.csv");
await foreach (var row in serializer.DeserializeRows(readStream))
{
    Console.WriteLine($"Company: {row.Name}, Rating: {row.Rating}");
}

// Serialize
var companies = new[] {
    new CompanySchema(1, "Acme Corp", 4.5f),
    new CompanySchema(2, "Tech Inc", 4.8f)
};

using var writeStream = File.Create("output.csv");
await serializer.SerializeRows(writeStream, companies.ToAsyncEnumerable());</code></pre>

## Remarks

<p>
<strong>Type Constraints:</strong>
</p>
<p>
TRow must implement both:
</p>
<ul><li><xref href="Flowthru.Core.Abstractions.IFlatSchema" data-throw-if-not-resolved="false"></xref> - No nested structures (primitives only)</li><li><xref href="Flowthru.Core.Abstractions.ITextSerializable" data-throw-if-not-resolved="false"></xref> - Can be serialized to text</li></ul>
<p>
These constraints are enforced at compile-time, preventing invalid usage:
</p>
<pre><code class="lang-csharp">// ✅ Compiles - flat schema with text serialization
var csv = new CsvFormatSerializer&lt;CompanySchema&gt;();

// ❌ Compile error - nested schema not allowed
var csv = new CsvFormatSerializer&lt;OrderWithItems&gt;(); // OrderWithItems : INestedSchema</code></pre>
<p>
<strong>Configuration:</strong>
</p>
<p>
Uses CsvHelper library with default configuration:
- HasHeaderRecord = true
- InvariantCulture
- Comma delimiter
</p>
<p>
Custom configuration can be provided via constructor.
</p>
<p>
<strong>Null Handling:</strong>
</p>
<p>
By default, empty cells in nullable properties (<code>string?</code>, <code>int?</code>,
<code>DateTime?</code>, etc.) deserialize to <code>null</code> — matching the conventional CSV
representation where <code>,,</code> indicates a missing value (the same convention pandas,
R, and most CSV consumers use). Non-nullable properties retain their type's default
behavior: <code>string</code> reads empty cells as empty strings, value types use CsvHelper
defaults.
</p>
<p>
Catalog authors can extend the set of null sentinels via the <code>nullValues</code>
constructor parameter — for example <code>["", "NA", "N/A", "NULL"]</code> to handle messy
real-world data. Nullability is detected per-property via <xref href="System.Reflection.NullabilityInfoContext" data-throw-if-not-resolved="false"></xref>;
the override applies only to properties declared nullable in the schema.
</p>
<p>
<strong>Streaming Behavior:</strong>
</p>
<p>
Both deserialization and serialization use streaming:
- Rows are yielded/consumed lazily
- Low memory footprint for large files
- Backpressure support via IAsyncEnumerable
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_Format_CsvFormatSerializer_1__ctor"></a> CsvFormatSerializer\(\)

Creates a new CSV format serializer with default configuration. Empty cells in
nullable properties deserialize to null.

```csharp
public CsvFormatSerializer()
```

### <a id="Flowthru_Core_Data_Storage_Format_CsvFormatSerializer_1__ctor_System_Collections_Generic_IReadOnlyList_System_String__"></a> CsvFormatSerializer\(IReadOnlyList<string\>\)

Creates a new CSV format serializer with a custom set of null-representation strings.

```csharp
public CsvFormatSerializer(IReadOnlyList<string> nullValues)
```

#### Parameters

`nullValues` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

Strings that should deserialize to null for nullable properties. Pass
<code>["", "NA", "N/A", "NULL"]</code> for pandas-style handling of messy data. The first
entry — typically <xref href="System.String.Empty" data-throw-if-not-resolved="false"></xref> — is also used as the canonical write
representation when a nullable property's value is null.

### <a id="Flowthru_Core_Data_Storage_Format_CsvFormatSerializer_1__ctor_CsvHelper_Configuration_CsvConfiguration_"></a> CsvFormatSerializer\(CsvConfiguration\)

Creates a new CSV format serializer with custom configuration.

```csharp
public CsvFormatSerializer(CsvConfiguration configuration)
```

#### Parameters

`configuration` CsvConfiguration

CsvHelper configuration

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

Thrown if configuration is null

### <a id="Flowthru_Core_Data_Storage_Format_CsvFormatSerializer_1__ctor_CsvHelper_Configuration_CsvConfiguration_System_Collections_Generic_IReadOnlyList_System_String__"></a> CsvFormatSerializer\(CsvConfiguration, IReadOnlyList<string\>\)

Creates a new CSV format serializer with custom configuration and null-representation
strings.

```csharp
public CsvFormatSerializer(CsvConfiguration configuration, IReadOnlyList<string> nullValues)
```

#### Parameters

`configuration` CsvConfiguration

CsvHelper configuration

`nullValues` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

Strings that should deserialize to null for nullable properties.

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

Thrown if either argument is null.

## Properties

### <a id="Flowthru_Core_Data_Storage_Format_CsvFormatSerializer_1_Configuration"></a> Configuration

Gets the CSV configuration for this serializer.

```csharp
public CsvConfiguration Configuration { get; }
```

#### Property Value

 CsvConfiguration

### <a id="Flowthru_Core_Data_Storage_Format_CsvFormatSerializer_1_NullValues"></a> NullValues

Gets the null-representation strings for this serializer.

```csharp
public IReadOnlyList<string> NullValues { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="Flowthru_Core_Data_Storage_Format_CsvFormatSerializer_1_RowFeatures"></a> RowFeatures

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

### <a id="Flowthru_Core_Data_Storage_Format_CsvFormatSerializer_1_Traits"></a> Traits

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

### <a id="Flowthru_Core_Data_Storage_Format_CsvFormatSerializer_1_DeserializeRows_System_IO_Stream_"></a> DeserializeRows\(Stream\)

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

### <a id="Flowthru_Core_Data_Storage_Format_CsvFormatSerializer_1_GetPropertyMappingConfiguration"></a> GetPropertyMappingConfiguration\(\)

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

### <a id="Flowthru_Core_Data_Storage_Format_CsvFormatSerializer_1_SerializeRows_System_IO_Stream_System_Collections_Generic_IAsyncEnumerable__0__"></a> SerializeRows\(Stream, IAsyncEnumerable<TRow\>\)

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

