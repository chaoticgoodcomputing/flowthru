# <a id="Flowthru_Core_Data_Storage_ComposedStorageAdapter_2"></a> Class ComposedStorageAdapter<TContainer, TRow\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Composed storage adapter that delegates to medium, format, and container layers.

```csharp
public sealed class ComposedStorageAdapter<TContainer, TRow> : IStorageAdapter<TContainer> where TRow : notnull
```

#### Type Parameters

`TContainer` 

The in-memory container type (IEnumerable, IDataView, Seq)

`TRow` 

The row schema type

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ComposedStorageAdapter<TContainer, TRow\>](Flowthru.Core.Data.Storage.ComposedStorageAdapter\-2.md)

#### Implements

[IStorageAdapter<TContainer\>](Flowthru.Core.Data.Storage.IStorageAdapter\-1.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Examples

<pre><code class="lang-csharp">// CSV file with IEnumerable container
var csvEnum = new ComposedStorageAdapter&lt;IEnumerable&lt;Company&gt;, Company&gt;(
    medium: new FileStorageMedium("data.csv"),
    format: new CsvFormatSerializer&lt;Company&gt;(),
    container: new EnumerableContainerAdapter&lt;Company&gt;()
);

// Same CSV file with IDataView container
var csvDataView = new ComposedStorageAdapter&lt;IDataView, Company&gt;(
    medium: new FileStorageMedium("data.csv"),
    format: new CsvFormatSerializer&lt;Company&gt;(),
    container: new DataViewContainerAdapter&lt;Company&gt;(mlContext)
);</code></pre>

## Remarks

<p>
<strong>Composition Pattern:</strong>
</p>
<p>
This class composes three independent concerns:
</p>
<pre><code class="lang-csharp">Medium (WHERE)    → Format (HOW)         → Container (WHAT)
File/Memory/Net   → CSV/JSON/Parquet     → IEnumerable/IDataView/Seq</code></pre>
<p>
<strong>Multiplicative Flexibility:</strong>
</p>
<p>
With M mediums, F formats, and C containers, you get M × F × C combinations
with only M + F + C implementations.
</p>
<p>
Example: 3 mediums × 4 formats × 3 containers = 36 combinations with 10 implementations.
</p>
<p>
<strong>Capability Implementation:</strong>
</p>
<p>
This adapter can optionally implement capability interfaces based on the
underlying medium and format capabilities. Capabilities are implemented
as explicit interface implementations to avoid polluting the base interface.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_ComposedStorageAdapter_2__ctor_Flowthru_Core_Data_Storage_IStorageMedium_Flowthru_Core_Data_Storage_IFormatSerializer__1__Flowthru_Core_Data_Storage_IContainerAdapter__0__1__"></a> ComposedStorageAdapter\(IStorageMedium, IFormatSerializer<TRow\>, IContainerAdapter<TContainer, TRow\>\)

Creates a new composed storage adapter.

```csharp
public ComposedStorageAdapter(IStorageMedium medium, IFormatSerializer<TRow> format, IContainerAdapter<TContainer, TRow> container)
```

#### Parameters

`medium` [IStorageMedium](Flowthru.Core.Data.Storage.IStorageMedium.md)

The storage medium (file, memory, etc.)

`format` [IFormatSerializer](Flowthru.Core.Data.Storage.IFormatSerializer\-1.md)<TRow\>

The format serializer (CSV, JSON, etc.)

`container` [IContainerAdapter](Flowthru.Core.Data.Storage.IContainerAdapter\-2.md)<TContainer, TRow\>

The container adapter (IEnumerable, IDataView, etc.)

## Properties

### <a id="Flowthru_Core_Data_Storage_ComposedStorageAdapter_2_Traits"></a> Traits

Structural constraints and capabilities of this storage implementation.

```csharp
public StorageTraits Traits { get; }
```

#### Property Value

 [StorageTraits](Flowthru.Core.Data.Capabilities.StorageTraits.md)

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

### <a id="Flowthru_Core_Data_Storage_ComposedStorageAdapter_2_Exists"></a> Exists\(\)

Checks if data exists at this storage location.

```csharp
public FlowIO<bool> Exists()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

Effect that produces true if data exists, false otherwise

#### Remarks

<p>
Delegates to the underlying medium's Exists check.
Used to determine if a catalog entry is a seed (Layer 0 input).
</p>

### <a id="Flowthru_Core_Data_Storage_ComposedStorageAdapter_2_InspectDeep"></a> InspectDeep\(\)

Performs deep validation by examining the entire dataset.

```csharp
public FlowIO<ValidationResult> InspectDeep()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

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

### <a id="Flowthru_Core_Data_Storage_ComposedStorageAdapter_2_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

Performs shallow validation by checking data availability and sampling a subset of data.

```csharp
public FlowIO<ValidationResult> InspectShallow(int sampleSize)
```

#### Parameters

`sampleSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of rows/records to sample for validation

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

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

### <a id="Flowthru_Core_Data_Storage_ComposedStorageAdapter_2_InspectTarget"></a> InspectTarget\(\)

Validates that this storage location is accessible as a write destination.

```csharp
public FlowIO<ValidationResult> InspectTarget()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

Effect producing validation result

#### Remarks

<p>
<strong>Semantic Intent:</strong> Validate that the destination can accept writes
before any pipeline step executes. This is distinct from <xref href="Flowthru.Core.Data.Storage.IStorageAdapter%601.InspectShallow(System.Int32)" data-throw-if-not-resolved="false"></xref>,
which validates that readable data exists.
</p>
<p>
<strong>Typical Checks:</strong>
</p>
<ul><li>File adapters: Parent directory exists and process has write permission</li><li>Database adapters: Target table exists, schema is compatible, connection is valid</li><li>Read-only adapters (<code>CanWrite = false</code>): Return success trivially</li><li>Memory / null adapters: Return success trivially</li></ul>
<p>
<strong>When Called:</strong> During pre-flight validation, after external inputs are
inspected and before any step executes. Skipped if <code>Traits.CanInspect = false</code>
or if explicitly disabled via <code>ValidationOptions.SkipTargetInspection()</code>.
</p>

### <a id="Flowthru_Core_Data_Storage_ComposedStorageAdapter_2_Load"></a> Load\(\)

Loads data from storage.

```csharp
public FlowIO<TContainer> Load()
```

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<TContainer\>

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

### <a id="Flowthru_Core_Data_Storage_ComposedStorageAdapter_2_Save__0_"></a> Save\(TContainer\)

Saves data to storage.

```csharp
public FlowIO<FlowUnit> Save(TContainer data)
```

#### Parameters

`data` TContainer

The data to save

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<[FlowUnit](Flowthru.Core.Effects.FlowUnit.md)\>

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

