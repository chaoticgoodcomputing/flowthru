# <a id="Flowthru_Data_Storage_SingletonJsonStorageAdapter_1"></a> Class SingletonJsonStorageAdapter<T\>

Namespace: [Flowthru.Data.Storage](Flowthru.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Direct JSON file storage for singleton objects (not collections).

```csharp
public sealed class SingletonJsonStorageAdapter<T> : IStorageAdapter<T> where T : IStructuredSerializable
```

#### Type Parameters

`T` 

The object type to serialize

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SingletonJsonStorageAdapter<T\>](Flowthru.Data.Storage.SingletonJsonStorageAdapter\-1.md)

#### Implements

[IStorageAdapter<T\>](Flowthru.Data.Storage.IStorageAdapter\-1.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Examples

<pre><code class="lang-csharp">var storage = new SingletonJsonStorageAdapter&lt;LinearRegressionModel&gt;("model.json");
var entry = new Item&lt;LinearRegressionModel&gt;("model", storage);

// Save
await entry.Save(model).RunAsync();

// Load
var loadedModel = await entry.Load().RunAsync();</code></pre>

## Remarks

<p>
<strong>Design Rationale:</strong> Singleton objects don't need the full
medium/format/container composition since they don't stream rows. This adapter
provides direct JSON serialization for single objects.
</p>
<p>
<strong>Use Cases:</strong>
</p>
<ul><li>ML models (LinearRegressionModel)</li><li>Metrics objects (ModelMetrics, CrossValidationResults)</li><li>Configuration files</li><li>Any single object (not a collection)</li></ul>
<p>
<strong>Serialization Format:</strong> JSON object (not wrapped in array)
</p>
<p>
<strong>Storage Traits:</strong> All traits use filesystem baseline defaults
</p>

## Constructors

### <a id="Flowthru_Data_Storage_SingletonJsonStorageAdapter_1__ctor_System_String_"></a> SingletonJsonStorageAdapter\(string\)

Creates a new singleton JSON storage adapter with default options.
Uses JsonFormatSerializer's default options to ensure consistent behavior,
including SerializedLabel attribute support.

```csharp
public SingletonJsonStorageAdapter(string filePath)
```

#### Parameters

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to JSON file

### <a id="Flowthru_Data_Storage_SingletonJsonStorageAdapter_1__ctor_System_String_System_Text_Json_JsonSerializerOptions_"></a> SingletonJsonStorageAdapter\(string, JsonSerializerOptions\)

Creates a new singleton JSON storage adapter with custom options.

```csharp
public SingletonJsonStorageAdapter(string filePath, JsonSerializerOptions options)
```

#### Parameters

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

Path to JSON file

`options` [JsonSerializerOptions](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions)

JSON serialization options

## Properties

### <a id="Flowthru_Data_Storage_SingletonJsonStorageAdapter_1_FilePath"></a> FilePath

Gets the file path used by this adapter.

```csharp
public string FilePath { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Data_Storage_SingletonJsonStorageAdapter_1_Options"></a> Options

Gets the JSON serialization options.

```csharp
public JsonSerializerOptions Options { get; }
```

#### Property Value

 [JsonSerializerOptions](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions)

### <a id="Flowthru_Data_Storage_SingletonJsonStorageAdapter_1_Traits"></a> Traits

Structural constraints and capabilities of this storage implementation.

```csharp
public StorageTraits Traits { get; }
```

#### Property Value

 [StorageTraits](Flowthru.Data.Capabilities.StorageTraits.md)

#### Remarks

<p>
Adapter authors must declare what their storage can and cannot do.
These are intrinsic properties of the storage medium, not runtime state.
</p>
<p>
Pipeline validation uses these traits to fail fast when a pipeline attempts
invalid operations (e.g., writing to a read-only source).
</p>

## Methods

### <a id="Flowthru_Data_Storage_SingletonJsonStorageAdapter_1_Exists"></a> Exists\(\)

Checks if data exists at this storage location.

```csharp
public FlowIO<bool> Exists()
```

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

Effect that produces true if data exists, false otherwise

#### Remarks

<p>
Delegates to the underlying medium's Exists check.
Used to determine if a catalog entry is a seed (Layer 0 input).
</p>

### <a id="Flowthru_Data_Storage_SingletonJsonStorageAdapter_1_InspectDeep"></a> InspectDeep\(\)

Performs deep validation by examining the entire dataset.

```csharp
public FlowIO<ValidationResult> InspectDeep()
```

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Data.Validation.ValidationResult.md)\>

Effect producing validation result

#### Remarks

<p>
<strong>Semantic Intent:</strong> Validate that all data is available, accessible, and valid.
</p>
<p>
<strong>Additional Checks Beyond Shallow:</strong>
</p>
<ul><li>Validate ALL rows can be deserialized (not just sample)</li><li>Check data quality constraints across entire dataset</li><li>Detect corruption or inconsistencies throughout data</li></ul>
<p>
<strong>Implementation Guidelines:</strong>
</p>
<ul><li>File adapters: Read and validate entire file</li><li>Memory adapters: Validate all stored data</li><li>Database adapters: Full table scan with validation</li><li>Null adapters: Always return success (no data required)</li></ul>
<p>
<strong>Performance:</strong> Potentially expensive - only use when data integrity is critical.
</p>

### <a id="Flowthru_Data_Storage_SingletonJsonStorageAdapter_1_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

Performs shallow validation by checking data availability and sampling a subset of data.

```csharp
public FlowIO<ValidationResult> InspectShallow(int sampleSize)
```

#### Parameters

`sampleSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of rows/records to sample for validation

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Data.Validation.ValidationResult.md)\>

Effect producing validation result

#### Remarks

<p>
<strong>Semantic Intent:</strong> Validate that data is available and accessible.
</p>
<p>
<strong>Typical Checks:</strong>
</p>
<ul><li>Data source exists (file, table, etc.)</li><li>Data source is accessible (permissions, connectivity)</li><li>Sample rows can be read and deserialized successfully</li><li>Schema matches expected structure</li></ul>
<p>
<strong>Implementation Guidelines:</strong>
</p>
<ul><li>File adapters: Check file exists, read and validate sample rows</li><li>Memory adapters: Check if data has been initialized</li><li>Database adapters: Check table exists, query sample rows</li><li>Null adapters: Always return success (no data required)</li></ul>
<p>
<strong>Performance:</strong> Should be fast (~10-100ms) - suitable for pre-flight validation.
</p>

### <a id="Flowthru_Data_Storage_SingletonJsonStorageAdapter_1_Load"></a> Load\(\)

Loads data from storage.

```csharp
public FlowIO<T> Load()
```

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<T\>

Effect that produces data on success

#### Remarks

<p>
<strong>Execution Flow:</strong>
</p>
<p>
For composed adapters, this orchestrates:
</p>
<pre><code class="lang-csharp">1. medium.ReadStream()           → Stream
2. format.DeserializeRows()      → IAsyncEnumerable&lt;TRow&gt;
3. container.FromRows()          → TContainer</code></pre>
<p>
<strong>Error Handling:</strong>
</p>
<p>
Errors from any layer are propagated:
- Medium errors (file not found, access denied)
- Format errors (parse failures, schema mismatches)
- Container errors (memory allocation, type conversion)
</p>

### <a id="Flowthru_Data_Storage_SingletonJsonStorageAdapter_1_Save__0_"></a> Save\(T\)

Saves data to storage.

```csharp
public FlowIO<FlowUnit> Save(T data)
```

#### Parameters

`data` T

The data to save

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Effects.FlowUnit.md)\>

Effect that completes on successful save

#### Remarks

<p>
<strong>Execution Flow:</strong>
</p>
<p>
For composed adapters, this orchestrates:
</p>
<pre><code class="lang-csharp">1. container.ToRows()            → IAsyncEnumerable&lt;TRow&gt;
2. format.SerializeRows()        → Stream
3. medium.WriteStream()          → FlowUnit</code></pre>
<p>
<strong>Atomicity:</strong>
</p>
<p>
Implementations should strive for atomic saves to avoid partial writes on failure.
</p>

