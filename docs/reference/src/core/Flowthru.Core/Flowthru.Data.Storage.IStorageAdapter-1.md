# <a id="Flowthru_Data_Storage_IStorageAdapter_1"></a> Interface IStorageAdapter<T\>

Namespace: [Flowthru.Data.Storage](Flowthru.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Interface for high-level storage operations - abstracts Load/Save with any storage implementation.

```csharp
public interface IStorageAdapter<T>
```

#### Type Parameters

`T` 

The data type (container with rows)

## Examples

<pre><code class="lang-csharp">// Composed storage adapter
var storage = new ComposedStorageAdapter&lt;IEnumerable&lt;CompanySchema&gt;, CompanySchema&gt;(
    medium: new FileStorageMedium("data.csv"),
    format: new CsvFormatSerializer&lt;CompanySchema&gt;(),
    container: new EnumerableContainerAdapter&lt;CompanySchema&gt;()
);

var loadResult = await storage.Load().Run();
loadResult.Match(
    Succ: data =&gt; Console.WriteLine($"Loaded {data.Count()} rows"),
    Fail: err =&gt; Console.WriteLine($"Load failed: {err}")
);

var saveResult = await storage.Save(companies).Run();</code></pre>

## Remarks

<p>
<strong>Responsibility:</strong> Provide simple Load/Save API regardless of underlying storage strategy.
</p>
<p>
<strong>Abstraction Layer:</strong>
</p>
<p>
This interface hides the complexity of:
- Medium selection (file vs memory)
- Format serialization (CSV vs JSON vs Parquet)
- Container adaptation (IEnumerable vs IDataView)
</p>
<p>
<strong>Implementation Strategies:</strong>
</p>
<ul><li><strong>Composed:</strong> <xref href="Flowthru.Data.Storage.ComposedStorageAdapter%602" data-throw-if-not-resolved="false"></xref> - composition of medium + format + container</li><li><strong>Custom:</strong> User-defined implementations (database, API, etc.)</li></ul>
<p>
<strong>Effect Types:</strong>
</p>
<p>
All operations return <xref href="Flowthru.Effects.FlowIO%601" data-throw-if-not-resolved="false"></xref> effects to represent:
- I/O operations that can fail
- Async execution
- Cancellation support
- Functional composition
</p>
<p>
<strong>Usage in Catalog Entries:</strong>
</p>
<p>
<xref href="Flowthru.Data.IItem%601" data-throw-if-not-resolved="false"></xref> delegates to this interface:
</p>
<pre><code class="lang-csharp">public class Item&lt;T&gt; : IItem&lt;T&gt;
{
    private readonly IStorageAdapter&lt;T&gt; _storage;

    public FlowIO&lt;T&gt; Load() =&gt; _storage.Load();
    public FlowIO&lt;FlowUnit&gt; Save(T data) =&gt; _storage.Save(data);
}</code></pre>

## Properties

### <a id="Flowthru_Data_Storage_IStorageAdapter_1_Traits"></a> Traits

Structural constraints and capabilities of this storage implementation.

```csharp
StorageTraits Traits { get; }
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

### <a id="Flowthru_Data_Storage_IStorageAdapter_1_Exists"></a> Exists\(\)

Checks if data exists at this storage location.

```csharp
FlowIO<bool> Exists()
```

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

Effect that produces true if data exists, false otherwise

#### Remarks

<p>
Delegates to the underlying medium's Exists check.
Used to determine if a catalog entry is a seed (Layer 0 input).
</p>

### <a id="Flowthru_Data_Storage_IStorageAdapter_1_InspectDeep"></a> InspectDeep\(\)

Performs deep validation by examining the entire dataset.

```csharp
FlowIO<ValidationResult> InspectDeep()
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

### <a id="Flowthru_Data_Storage_IStorageAdapter_1_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

Performs shallow validation by checking data availability and sampling a subset of data.

```csharp
FlowIO<ValidationResult> InspectShallow(int sampleSize)
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

### <a id="Flowthru_Data_Storage_IStorageAdapter_1_Load"></a> Load\(\)

Loads data from storage.

```csharp
FlowIO<T> Load()
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

### <a id="Flowthru_Data_Storage_IStorageAdapter_1_Save__0_"></a> Save\(T\)

Saves data to storage.

```csharp
FlowIO<FlowUnit> Save(T data)
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

