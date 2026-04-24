# <a id="Flowthru_Core_Data_Storage_GqlQueryStorageAdapter_3"></a> Class GqlQueryStorageAdapter<TFilter, TResult, T\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Extensions.GQL.dll  

Storage adapter that holds a GqlQuery&lt;TFilter,TResult,T&gt; handle.

```csharp
public sealed class GqlQueryStorageAdapter<TFilter, TResult, T> : IStorageAdapter<GqlQuery<TFilter, TResult, T>> where TFilter : class where TResult : class where T : class
```

#### Type Parameters

`TFilter` 

`TResult` 

`T` 

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GqlQueryStorageAdapter<TFilter, TResult, T\>](Flowthru.Core.Data.Storage.GqlQueryStorageAdapter\-3.md)

#### Implements

IStorageAdapter<GqlQuery<TFilter, TResult, T\>\>

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

Identical to <xref href="Flowthru.Core.Data.Storage.GqlQueryStorageAdapter%602" data-throw-if-not-resolved="false"></xref> but for the filtered variant.
The pre-flight probe is executed with a <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> filter to validate connectivity
independently of any runtime-supplied filter value.

## Constructors

### <a id="Flowthru_Core_Data_Storage_GqlQueryStorageAdapter_3__ctor_Flowthru_Extensions_GQL_Data_GqlQuery__0__1__2__"></a> GqlQueryStorageAdapter\(GqlQuery<TFilter, TResult, T\>\)

```csharp
public GqlQueryStorageAdapter(GqlQuery<TFilter, TResult, T> query)
```

#### Parameters

`query` [GqlQuery](Flowthru.Extensions.GQL.Data.GqlQuery\-3.md)<TFilter, TResult, T\>

The pre-built deferred filtered query handle.

## Properties

### <a id="Flowthru_Core_Data_Storage_GqlQueryStorageAdapter_3_Traits"></a> Traits

Structural constraints and capabilities of this storage implementation.

```csharp
public StorageTraits Traits { get; }
```

#### Property Value

 StorageTraits

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

### <a id="Flowthru_Core_Data_Storage_GqlQueryStorageAdapter_3_Exists"></a> Exists\(\)

Checks if data exists at this storage location.

```csharp
public FlowIO<bool> Exists()
```

#### Returns

 FlowIO<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

Effect that produces true if data exists, false otherwise

#### Remarks

<p>
Delegates to the underlying medium's Exists check.
Used to determine if a catalog entry is a seed (Layer 0 input).
</p>

### <a id="Flowthru_Core_Data_Storage_GqlQueryStorageAdapter_3_InspectDeep"></a> InspectDeep\(\)

Performs deep validation by examining the entire dataset.

```csharp
public FlowIO<ValidationResult> InspectDeep()
```

#### Returns

 FlowIO<ValidationResult\>

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

### <a id="Flowthru_Core_Data_Storage_GqlQueryStorageAdapter_3_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

Performs shallow validation by checking data availability and sampling a subset of data.

```csharp
public FlowIO<ValidationResult> InspectShallow(int sampleSize)
```

#### Parameters

`sampleSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of rows/records to sample for validation

#### Returns

 FlowIO<ValidationResult\>

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

### <a id="Flowthru_Core_Data_Storage_GqlQueryStorageAdapter_3_InspectTarget"></a> InspectTarget\(\)

Validates that this storage location is accessible as a write destination.

```csharp
public FlowIO<ValidationResult> InspectTarget()
```

#### Returns

 FlowIO<ValidationResult\>

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

### <a id="Flowthru_Core_Data_Storage_GqlQueryStorageAdapter_3_Load"></a> Load\(\)

Loads data from storage.

```csharp
public FlowIO<GqlQuery<TFilter, TResult, T>> Load()
```

#### Returns

 FlowIO<[GqlQuery](Flowthru.Extensions.GQL.Data.GqlQuery\-3.md)<TFilter, TResult, T\>\>

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

### <a id="Flowthru_Core_Data_Storage_GqlQueryStorageAdapter_3_Save_Flowthru_Extensions_GQL_Data_GqlQuery__0__1__2__"></a> Save\(GqlQuery<TFilter, TResult, T\>\)

Saves data to storage.

```csharp
public FlowIO<FlowUnit> Save(GqlQuery<TFilter, TResult, T> data)
```

#### Parameters

`data` [GqlQuery](Flowthru.Extensions.GQL.Data.GqlQuery\-3.md)<TFilter, TResult, T\>

The data to save

#### Returns

 FlowIO<FlowUnit\>

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

