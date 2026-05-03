# <a id="Flowthru_Core_Data_Storage_Format_JsonFormatSerializer_1"></a> Class JsonFormatSerializer<TRow\>

Namespace: [Flowthru.Core.Data.Storage.Format](Flowthru.Core.Data.Storage.Format.md)  
Assembly: Flowthru.Core.dll  

Format serializer for JSON (JavaScript Object Notation) files.

```csharp
public sealed class JsonFormatSerializer<TRow> : IFormatSerializer<TRow>, IFormatRowReader<TRow>, IFormatRowWriter<TRow>, IFormatBase<TRow> where TRow : notnull, IStructuredSerializable
```

#### Type Parameters

`TRow` 

The row schema type

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[JsonFormatSerializer<TRow\>](Flowthru.Core.Data.Storage.Format.JsonFormatSerializer\-1.md)

#### Implements

[IFormatSerializer<TRow\>](Flowthru.Core.Data.Storage.IFormatSerializer\-1.md), 
[IFormatRowReader<TRow\>](Flowthru.Core.Data.Storage.IFormatRowReader\-1.md), 
[IFormatRowWriter<TRow\>](Flowthru.Core.Data.Storage.IFormatRowWriter\-1.md), 
[IFormatBase<TRow\>](Flowthru.Core.Data.Storage.IFormatBase\-1.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Examples

<pre><code class="lang-csharp">// Flat schema
public record MetricsSchema(
    double Accuracy,
    double Precision,
    double Recall
) : IFlatSchema, IStructuredSerializable;

// Nested schema
public record ResultsSchema(
    List&lt;MetricsSchema&gt; FoldMetrics,
    double MeanAccuracy
) : INestedSchema, IStructuredSerializable;

var serializer = new JsonFormatSerializer&lt;ResultsSchema&gt;();

// Serialize
var results = new[] {
    new ResultsSchema(new List&lt;MetricsSchema&gt; { /* ... */ }, 0.95)
};

using var writeStream = File.Create("results.json");
await serializer.SerializeRows(writeStream, results.ToAsyncEnumerable());</code></pre>

## Remarks

<p>
<strong>Type Constraints:</strong>
</p>
<p>
TRow must implement <xref href="Flowthru.Core.Abstractions.IStructuredSerializable" data-throw-if-not-resolved="false"></xref>, which supports both:
</p>
<ul><li><xref href="Flowthru.Core.Abstractions.IFlatSchema" data-throw-if-not-resolved="false"></xref> - Simple flat structures</li><li><xref href="Flowthru.Core.Abstractions.INestedSchema" data-throw-if-not-resolved="false"></xref> - Complex nested structures</li></ul>
<p>
JSON is flexible and can handle any schema structure, making it suitable for:
- Configuration objects
- Model metadata and metrics
- Nested result structures
- Human-readable data files
</p>
<p>
<strong>Configuration:</strong>
</p>
<p>
Uses System.Text.Json with default configuration:
- WriteIndented = true (pretty printing)
- PropertyNamingPolicy = CamelCase
- DefaultIgnoreCondition = WhenWritingNull
</p>
<p>
Custom JsonSerializerOptions can be provided via constructor.
</p>
<p>
<strong>Streaming Behavior:</strong>
</p>
<p>
JSON serialization streams rows as a JSON array:
</p>
<pre><code class="lang-csharp">[
  { "id": 1, "name": "Item 1" },
  { "id": 2, "name": "Item 2" }
]</code></pre>
<p>
Both deserialization and serialization are streaming, yielding/consuming
rows lazily for memory efficiency.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_Format_JsonFormatSerializer_1__ctor"></a> JsonFormatSerializer\(\)

Creates a new JSON format serializer with default configuration.

```csharp
public JsonFormatSerializer()
```

#### Remarks

<p>
<strong>Property Naming:</strong> No default naming policy is applied.
Use <xref href="Flowthru.Core.Abstractions.SerializedLabelAttribute" data-throw-if-not-resolved="false"></xref> to specify property names explicitly.
If no SerializedLabel is present, the C# property name is used as-is.
</p>

### <a id="Flowthru_Core_Data_Storage_Format_JsonFormatSerializer_1__ctor_System_Text_Json_JsonSerializerOptions_"></a> JsonFormatSerializer\(JsonSerializerOptions\)

Creates a new JSON format serializer with custom options.

```csharp
public JsonFormatSerializer(JsonSerializerOptions options)
```

#### Parameters

`options` [JsonSerializerOptions](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions)

JSON serialization options

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

Thrown if options is null

## Properties

### <a id="Flowthru_Core_Data_Storage_Format_JsonFormatSerializer_1_Options"></a> Options

Gets the JSON serialization options for this serializer.

```csharp
public JsonSerializerOptions Options { get; }
```

#### Property Value

 [JsonSerializerOptions](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions)

### <a id="Flowthru_Core_Data_Storage_Format_JsonFormatSerializer_1_RowFeatures"></a> RowFeatures

Row-shape capabilities this format supports. Defaults to all-false; format
implementations override the property to declare honestly which features round-trip.

```csharp
public FormatRowFeatures RowFeatures { get; }
```

#### Property Value

 [FormatRowFeatures](Flowthru.Core.Data.Capabilities.FormatRowFeatures.md)

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

### <a id="Flowthru_Core_Data_Storage_Format_JsonFormatSerializer_1_Traits"></a> Traits

Structural capabilities of this format serializer.

```csharp
public StorageTraits Traits { get; }
```

#### Property Value

 [StorageTraits](Flowthru.Core.Data.Capabilities.StorageTraits.md)

#### Remarks

Format traits focus on HOW data is serialized and whether it supports streaming.
For composed adapters, these traits are merged with medium and container traits.
Most formats should declare <code>CanStream = true</code> if they can deserialize row-by-row
without buffering the entire stream (e.g., CSV, Parquet). Formats that require full
parsing before yielding rows (e.g., JSON arrays) should set <code>CanStream = false</code>.

## Methods

### <a id="Flowthru_Core_Data_Storage_Format_JsonFormatSerializer_1_DeserializeRows_System_IO_Stream_"></a> DeserializeRows\(Stream\)

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

### <a id="Flowthru_Core_Data_Storage_Format_JsonFormatSerializer_1_GetPropertyMappingConfiguration"></a> GetPropertyMappingConfiguration\(\)

Configures how this serializer handles property-to-field name mapping for the schema.

```csharp
public PropertyMappingConfiguration GetPropertyMappingConfiguration()
```

#### Returns

 [PropertyMappingConfiguration](Flowthru.Core.Data.Storage.PropertyMappingConfiguration.md)

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

### <a id="Flowthru_Core_Data_Storage_Format_JsonFormatSerializer_1_SerializeRows_System_IO_Stream_System_Collections_Generic_IAsyncEnumerable__0__"></a> SerializeRows\(Stream, IAsyncEnumerable<TRow\>\)

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

