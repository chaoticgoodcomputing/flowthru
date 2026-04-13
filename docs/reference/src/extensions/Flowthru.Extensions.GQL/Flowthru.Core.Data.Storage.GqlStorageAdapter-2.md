# <a id="Flowthru_Core_Data_Storage_GqlStorageAdapter_2"></a> Class GqlStorageAdapter<TResult, T\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Extensions.GQL.dll  

Storage adapter for a single-item GraphQL query using a StrawberryShake client.

```csharp
public sealed class GqlStorageAdapter<TResult, T> : IStorageAdapter<T> where TResult : class where T : class
```

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type (e.g. <code>IGetCurrentUserResult</code>).
Must satisfy the <code>class</code> constraint imposed by <xref href="StrawberryShake.IOperationResult%601" data-throw-if-not-resolved="false"></xref>.

`T` 

The target type surfaced to the Flowthru catalog entry (e.g. <code>GetCurrentUser_User</code>).
Selected from <code class="typeparamref">TResult</code> via the <code>selectData</code> delegate.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GqlStorageAdapter<TResult, T\>](Flowthru.Core.Data.Storage.GqlStorageAdapter\-2.md)

#### Implements

IStorageAdapter<T\>

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Examples

<pre><code class="lang-csharp">// Read-only single-item query
var adapter = new GqlStorageAdapter&lt;IGetCurrentUserResult, GetCurrentUser_Me&gt;(
    label: "current-user",
    queryFunc: ct =&gt; _client.GetCurrentUser.ExecuteAsync(ct),
    selectData: r =&gt; r.Me!
);

// With mutation support
var adapter = new GqlStorageAdapter&lt;IGetCurrentUserResult, GetCurrentUser_Me&gt;(
    label: "current-user",
    queryFunc: ct =&gt; _client.GetCurrentUser.ExecuteAsync(ct),
    selectData: r =&gt; r.Me!,
    mutationFunc: (data, ct) =&gt; _client.UpdateCurrentUser.ExecuteAsync(data.Name, ct)
);</code></pre>

## Remarks

<p>
<strong>Design Rationale:</strong>
</p>
<p>
This is a specialized adapter that directly implements <xref href="Flowthru.Core.Data.Storage.IStorageAdapter%601" data-throw-if-not-resolved="false"></xref>
rather than the Medium→Format→Container composition pattern. GraphQL inherently
couples transport (HTTP/WebSocket), serialization (JSON), and schema in the generated
client — decomposing them would fight StrawberryShake's architecture.
</p>
<p>
<strong>StrawberryShake Boundary:</strong>
</p>
<p>
This extension does not own or configure the StrawberryShake client — the caller
brings their own configured client (registered via DI). The extension wraps operation
delegate invocations in <xref href="Flowthru.Core.Effects.FlowIO%601" data-throw-if-not-resolved="false"></xref> effects, mapping GQL errors to
structured <xref href="Flowthru.Core.Data.Validation.ValidationResult" data-throw-if-not-resolved="false"></xref> or <xref href="Flowthru.Core.Effects.FlowIO" data-throw-if-not-resolved="false"></xref> failures.
</p>
<p>
<strong>Mutation Support:</strong>
</p>
<p>
Providing a <code>mutationFunc</code> enables <xref href="Flowthru.Core.Data.Storage.GqlStorageAdapter%602.Save(%601)" data-throw-if-not-resolved="false"></xref>. When omitted,
<code>StorageTraits.CanWrite</code> is set to <code>false</code> and <xref href="Flowthru.Core.Data.Storage.GqlStorageAdapter%602.Save(%601)" data-throw-if-not-resolved="false"></xref> fails fast.
</p>
<p>
<strong>Pre-flight Validation:</strong>
</p>
<p>
<xref href="Flowthru.Core.Data.Storage.GqlStorageAdapter%602.InspectShallow(System.Int32)" data-throw-if-not-resolved="false"></xref> executes the full query against the live endpoint to
validate reachability, authentication, and schema compatibility before any pipeline step
runs. For single-item queries the query itself is the minimal probe.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_GqlStorageAdapter_2__ctor_System_String_System_Func_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult__0____System_Func__0__1__System_Boolean_"></a> GqlStorageAdapter\(string, Func<CancellationToken, Task<IOperationResult<TResult\>\>\>, Func<TResult, T\>, bool\)

Creates a read-only single-item GQL adapter.

```csharp
public GqlStorageAdapter(string label, Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc, Func<TResult, T> selectData, bool allowEmptyData = false)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

The catalog entry label, used in validation error messages.

`queryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate that executes the StrawberryShake query operation.

`selectData` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TResult, T\>

Projects the result data type to the target type <code class="typeparamref">T</code>.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, a <code>null</code> <xref href="StrawberryShake.IOperationResult%601.Data" data-throw-if-not-resolved="false"></xref> is treated
as valid during inspection. Defaults to <code>false</code>.

### <a id="Flowthru_Core_Data_Storage_GqlStorageAdapter_2__ctor_System_String_System_Func_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult__0____System_Func__0__1__System_Func__1_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___System_Boolean_"></a> GqlStorageAdapter\(string, Func<CancellationToken, Task<IOperationResult<TResult\>\>\>, Func<TResult, T\>, Func<T, CancellationToken, Task<IOperationResult\>\>?, bool\)

Creates a read-write single-item GQL adapter.

```csharp
public GqlStorageAdapter(string label, Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc, Func<TResult, T> selectData, Func<T, CancellationToken, Task<IOperationResult>>? mutationFunc, bool allowEmptyData = false)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

The catalog entry label, used in validation error messages.

`queryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate that executes the StrawberryShake query operation.

`selectData` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TResult, T\>

Projects the result data type to the target type <code class="typeparamref">T</code>.

`mutationFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<T, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult\>\>?

Delegate that executes the StrawberryShake mutation operation for <xref href="Flowthru.Core.Data.Storage.GqlStorageAdapter%602.Save(%601)" data-throw-if-not-resolved="false"></xref>.
When provided, <code>StorageTraits.CanWrite</code> is set to <code>true</code>.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, a <code>null</code> <xref href="StrawberryShake.IOperationResult%601.Data" data-throw-if-not-resolved="false"></xref> is treated
as valid during inspection. Defaults to <code>false</code>.

## Properties

### <a id="Flowthru_Core_Data_Storage_GqlStorageAdapter_2_Traits"></a> Traits

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

### <a id="Flowthru_Core_Data_Storage_GqlStorageAdapter_2_Exists"></a> Exists\(\)

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

### <a id="Flowthru_Core_Data_Storage_GqlStorageAdapter_2_InspectDeep"></a> InspectDeep\(\)

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

### <a id="Flowthru_Core_Data_Storage_GqlStorageAdapter_2_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

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

### <a id="Flowthru_Core_Data_Storage_GqlStorageAdapter_2_Load"></a> Load\(\)

Loads data from storage.

```csharp
public FlowIO<T> Load()
```

#### Returns

 FlowIO<T\>

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

### <a id="Flowthru_Core_Data_Storage_GqlStorageAdapter_2_Save__1_"></a> Save\(T\)

Saves data to storage.

```csharp
public FlowIO<FlowUnit> Save(T data)
```

#### Parameters

`data` T

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

