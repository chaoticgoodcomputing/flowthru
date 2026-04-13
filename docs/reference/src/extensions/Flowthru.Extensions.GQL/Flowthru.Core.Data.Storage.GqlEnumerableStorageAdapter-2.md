# <a id="Flowthru_Core_Data_Storage_GqlEnumerableStorageAdapter_2"></a> Class GqlEnumerableStorageAdapter<TResult, T\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Extensions.GQL.dll  

Storage adapter for a collection GraphQL query using a StrawberryShake client.
Supports both non-paginated queries (server returns all results in one response) and
paginated queries via <xref href="Flowthru.Extensions.GQL.Data.RelayPaginationStrategy%602" data-throw-if-not-resolved="false"></xref> or
<xref href="Flowthru.Extensions.GQL.Data.OffsetPaginationStrategy%602" data-throw-if-not-resolved="false"></xref>.

```csharp
public sealed class GqlEnumerableStorageAdapter<TResult, T> : IStorageAdapter<IEnumerable<T>> where TResult : class where T : class
```

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type (e.g. <code>IGetSessionsResult</code>).

`T` 

The target element type surfaced to the Flowthru catalog entry (e.g. <code>GetSessions_Session</code>).

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GqlEnumerableStorageAdapter<TResult, T\>](Flowthru.Core.Data.Storage.GqlEnumerableStorageAdapter\-2.md)

#### Implements

IStorageAdapter<IEnumerable<T\>\>

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Examples

<pre><code class="lang-csharp">// Non-paginated
var adapter = new GqlEnumerableStorageAdapter&lt;IGetUsersResult, GetUsers_User&gt;(
    label: "users",
    queryFunc: ct =&gt; _client.GetUsers.ExecuteAsync(ct),
    selectData: r =&gt; r.Users ?? Enumerable.Empty&lt;GetUsers_User&gt;()
);

// Relay paginated
var adapter = new GqlEnumerableStorageAdapter&lt;IGetSessionsResult, GetSessions_Session&gt;(
    label: "sessions",
    pagedQueryFunc: (cursor, pageSize, ct) =&gt;
        _client.GetSessions.ExecuteAsync(first: pageSize, after: cursor, cancellationToken: ct),
    pagination: Pagination.Relay&lt;IGetSessionsResult, GetSessions_Session&gt;(
        getNodes: r =&gt; r.Sessions?.Nodes,
        getPageInfo: r =&gt; r.Sessions?.PageInfo is { } pi
            ? new PageInfo(pi.HasNextPage, pi.EndCursor)
            : null
    ),
    pageSize: 100
);</code></pre>

## Remarks

<p>
<strong>Non-paginated mode:</strong> Provide a <code>queryFunc</code> that accepts only a
<xref href="System.Threading.CancellationToken" data-throw-if-not-resolved="false"></xref>. Results are loaded in a single request.
</p>
<p>
<strong>Relay paginated mode:</strong> Provide a <code>queryFunc</code> accepting <code>(cursor, pageSize, ct)</code>
and a <xref href="Flowthru.Extensions.GQL.Data.RelayPaginationStrategy%602" data-throw-if-not-resolved="false"></xref>. The adapter iterates pages until
<code>HasNextPage</code> is false, concatenating nodes into a flat <code>IEnumerable&lt;T&gt;</code>.
</p>
<p>
<strong>Offset paginated mode:</strong> Provide a <code>queryFunc</code> accepting <code>(offset, limit, ct)</code>
and an <xref href="Flowthru.Extensions.GQL.Data.OffsetPaginationStrategy%602" data-throw-if-not-resolved="false"></xref>. The adapter advances the offset until
all items reported by <code>getTotal</code> have been fetched (or a page returns no items).
</p>
<p>
<strong>Pre-flight Validation:</strong>
</p>
<p>
<xref href="Flowthru.Core.Data.Storage.GqlEnumerableStorageAdapter%602.InspectShallow(System.Int32)" data-throw-if-not-resolved="false"></xref> executes a minimal one-item probe (<code>pageSize=1</code> / <code>limit=1</code>
for paginated modes) to validate endpoint reachability, authentication, and schema compatibility
before any pipeline step runs. <xref href="Flowthru.Core.Data.Storage.GqlEnumerableStorageAdapter%602.InspectDeep" data-throw-if-not-resolved="false"></xref> executes the full pagination loop.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_GqlEnumerableStorageAdapter_2__ctor_System_String_System_Func_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult__0____System_Func__0_System_Collections_Generic_IEnumerable__1___System_Boolean_"></a> GqlEnumerableStorageAdapter\(string, Func<CancellationToken, Task<IOperationResult<TResult\>\>\>, Func<TResult, IEnumerable<T\>?\>, bool\)

Creates a non-paginated collection adapter.

```csharp
public GqlEnumerableStorageAdapter(string label, Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc, Func<TResult, IEnumerable<T>?> selectData, bool allowEmptyData = false)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

The catalog entry label, used in validation error messages.

`queryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate that executes the StrawberryShake query operation.

`selectData` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TResult, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>?\>

Projects the result data type to the collection of <code class="typeparamref">T</code>.
Return <code>null</code> to yield an empty collection (subject to <code class="paramref">allowEmptyData</code>).

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty result set is valid.

### <a id="Flowthru_Core_Data_Storage_GqlEnumerableStorageAdapter_2__ctor_System_String_System_Func_System_String_System_Int32_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult__0____Flowthru_Extensions_GQL_Data_RelayPaginationStrategy__0__1__System_Int32_System_Boolean_"></a> GqlEnumerableStorageAdapter\(string, Func<string?, int, CancellationToken, Task<IOperationResult<TResult\>\>\>, RelayPaginationStrategy<TResult, T\>, int, bool\)

Creates a Relay cursor-paginated collection adapter.

```csharp
public GqlEnumerableStorageAdapter(string label, Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc, RelayPaginationStrategy<TResult, T> pagination, int pageSize = 100, bool allowEmptyData = false)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

The catalog entry label, used in validation error messages.

`pagedQueryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[string](https://learn.microsoft.com/dotnet/api/system.string)?, [int](https://learn.microsoft.com/dotnet/api/system.int32), [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate accepting <code>(cursor, pageSize, cancellationToken)</code> that executes the
StrawberryShake paginated query operation. Pass the cursor as the GraphQL <code>after</code>
argument and the pageSize as <code>first</code>.

`pagination` [RelayPaginationStrategy](Flowthru.Extensions.GQL.Data.RelayPaginationStrategy\-2.md)<TResult, T\>

Relay pagination strategy created via <xref href="Flowthru.Extensions.GQL.Data.Pagination.Relay%60%602(System.Func%7b%60%600%2cSystem.Collections.Generic.IEnumerable%7b%60%601%7d%7d%2cSystem.Func%7b%60%600%2cFlowthru.Extensions.GQL.Data.PageInfo%7d)" data-throw-if-not-resolved="false"></xref>.

`pageSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of items to request per page. Defaults to 100.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty result set is valid.

### <a id="Flowthru_Core_Data_Storage_GqlEnumerableStorageAdapter_2__ctor_System_String_System_Func_System_Int32_System_Int32_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult__0____Flowthru_Extensions_GQL_Data_OffsetPaginationStrategy__0__1__System_Int32_System_Boolean_"></a> GqlEnumerableStorageAdapter\(string, Func<int, int, CancellationToken, Task<IOperationResult<TResult\>\>\>, OffsetPaginationStrategy<TResult, T\>, int, bool\)

Creates an offset-paginated collection adapter.

```csharp
public GqlEnumerableStorageAdapter(string label, Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc, OffsetPaginationStrategy<TResult, T> pagination, int pageSize = 100, bool allowEmptyData = false)
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

The catalog entry label, used in validation error messages.

`pagedQueryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[int](https://learn.microsoft.com/dotnet/api/system.int32), [int](https://learn.microsoft.com/dotnet/api/system.int32), [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate accepting <code>(offset, limit, cancellationToken)</code> that executes the
StrawberryShake paginated query operation.

`pagination` [OffsetPaginationStrategy](Flowthru.Extensions.GQL.Data.OffsetPaginationStrategy\-2.md)<TResult, T\>

Offset pagination strategy created via <xref href="Flowthru.Extensions.GQL.Data.Pagination.Offset%60%602(System.Func%7b%60%600%2cSystem.Collections.Generic.IEnumerable%7b%60%601%7d%7d%2cSystem.Func%7b%60%600%2cSystem.Nullable%7bSystem.Int32%7d%7d)" data-throw-if-not-resolved="false"></xref>.

`pageSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of items to request per page. Defaults to 100.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty result set is valid.

## Properties

### <a id="Flowthru_Core_Data_Storage_GqlEnumerableStorageAdapter_2_Traits"></a> Traits

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

### <a id="Flowthru_Core_Data_Storage_GqlEnumerableStorageAdapter_2_Exists"></a> Exists\(\)

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

### <a id="Flowthru_Core_Data_Storage_GqlEnumerableStorageAdapter_2_InspectDeep"></a> InspectDeep\(\)

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

### <a id="Flowthru_Core_Data_Storage_GqlEnumerableStorageAdapter_2_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

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

### <a id="Flowthru_Core_Data_Storage_GqlEnumerableStorageAdapter_2_Load"></a> Load\(\)

Loads data from storage.

```csharp
public FlowIO<IEnumerable<T>> Load()
```

#### Returns

 FlowIO<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

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

### <a id="Flowthru_Core_Data_Storage_GqlEnumerableStorageAdapter_2_Save_System_Collections_Generic_IEnumerable__1__"></a> Save\(IEnumerable<T\>\)

Saves data to storage.

```csharp
public FlowIO<FlowUnit> Save(IEnumerable<T> data)
```

#### Parameters

`data` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>

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

