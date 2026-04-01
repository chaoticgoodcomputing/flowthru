# <a id="Flowthru_Data_Storage_MemoryStorageAdapter_1"></a> Class MemoryStorageAdapter<T\>

Namespace: [Flowthru.Data.Storage](Flowthru.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Direct memory storage adapter that bypasses serialization.

```csharp
public sealed class MemoryStorageAdapter<T> : IStorageAdapter<T>
```

#### Type Parameters

`T` 

The data type to store in memory

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MemoryStorageAdapter<T\>](Flowthru.Data.Storage.MemoryStorageAdapter\-1.md)

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

<pre><code class="lang-csharp">// Singleton usage
var modelStorage = new MemoryStorageAdapter&lt;LinearRegressionModel&gt;();
var modelEntry = new CatalogEntry&lt;LinearRegressionModel&gt;("model", modelStorage);

// Collection usage
var dataStorage = new MemoryStorageAdapter&lt;IEnumerable&lt;FeatureRow&gt;&gt;();
var dataEntry = new CatalogEntry&lt;IEnumerable&lt;FeatureRow&gt;&gt;("features", dataStorage);</code></pre>

## Remarks

<p>
<strong>Design Rationale:</strong> Memory storage doesn't need byte serialization
since objects stay in-process. This adapter provides direct Load/Save without
medium/format/container composition.
</p>
<p>
<strong>Use Cases:</strong>
</p>
<ul><li>Intermediate pipeline data that doesn't need persistence</li><li>Test data that doesn't require file I/O</li><li>Temporary results between pipeline stages</li><li>ML models, metrics, charts, or any ephemeral data</li></ul>
<p>
<strong>Thread Safety:</strong> Thread-safe for concurrent Load/Save operations
</p>
<p>
<strong>Lifetime:</strong> Data persists only for the lifetime of this instance
</p>
<p>
<strong>Storage Traits:</strong>
</p>
<ul><li>IsPersistent: false (data lost when process exits)</li><li>All other traits use filesystem baseline defaults</li></ul>

## Constructors

### <a id="Flowthru_Data_Storage_MemoryStorageAdapter_1__ctor"></a> MemoryStorageAdapter\(\)

Creates a new in-memory storage adapter.

```csharp
public MemoryStorageAdapter()
```

### <a id="Flowthru_Data_Storage_MemoryStorageAdapter_1__ctor__0_"></a> MemoryStorageAdapter\(T\)

Creates a new in-memory storage adapter with initial data.

```csharp
public MemoryStorageAdapter(T initialData)
```

#### Parameters

`initialData` T

Initial data to store

## Properties

### <a id="Flowthru_Data_Storage_MemoryStorageAdapter_1_Traits"></a> Traits

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

### <a id="Flowthru_Data_Storage_MemoryStorageAdapter_1_Exists"></a> Exists\(\)

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

### <a id="Flowthru_Data_Storage_MemoryStorageAdapter_1_InspectDeep"></a> InspectDeep\(\)

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

### <a id="Flowthru_Data_Storage_MemoryStorageAdapter_1_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

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

### <a id="Flowthru_Data_Storage_MemoryStorageAdapter_1_Load"></a> Load\(\)

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

### <a id="Flowthru_Data_Storage_MemoryStorageAdapter_1_Save__0_"></a> Save\(T\)

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

