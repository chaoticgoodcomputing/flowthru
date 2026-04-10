# <a id="Flowthru_Core_Data_Storage_EFCoreStorageAdapter_1"></a> Class EFCoreStorageAdapter<T\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Extensions.EFCore.dll  

Storage adapter for Entity Framework Core database access.

```csharp
public sealed class EFCoreStorageAdapter<T> : IStorageAdapter<IEnumerable<T>> where T : class
```

#### Type Parameters

`T` 

Entity type (must be a class configured in DbContext)

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EFCoreStorageAdapter<T\>](Flowthru.Core.Data.Storage.EFCoreStorageAdapter\-1.md)

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

<pre><code class="lang-csharp">// Injected DbContext (from DI container)
var adapter = new EFCoreStorageAdapter&lt;Company&gt;(dbContext);
var entry = new Item&lt;IEnumerable&lt;Company&gt;&gt;("companies", adapter);

// Factory-based DbContext (created per operation)
var adapter = new EFCoreStorageAdapter&lt;Company&gt;(() =&gt; new AppDbContext(options));
var entry = new Item&lt;IEnumerable&lt;Company&gt;&gt;("companies", adapter);

// Read-only mode (apply constraint at catalog level)
var entry = new Item&lt;IEnumerable&lt;Company&gt;&gt;("companies", adapter)
  .Constrain(traits =&gt; traits with { CanWrite = false });

// Allow empty tables during validation
var adapter = new EFCoreStorageAdapter&lt;Company&gt;(dbContext, allowEmptyData: true);</code></pre>

## Remarks

<p>
<strong>Design Rationale:</strong>
</p>
<p>
This is a <em>specialized adapter</em> that directly implements IStorageAdapter&lt;T&gt;
rather than using the Medium→Format→Container composition pattern. This design choice
reflects that EFCore inherently couples:
</p>
<ul><li><strong>WHERE:</strong> Connection string + database engine</li><li><strong>HOW:</strong> Entity mapping + LINQ-to-SQL translation</li><li><strong>WHAT:</strong> DbSet&lt;T&gt; query interface</li></ul>
<p>
Attempting to decompose these concerns would fight EFCore's architecture.
</p>
<p>
<strong>DbContext Lifecycle:</strong>
</p>
<p>
Supports two modes:
</p>
<ul><li><strong>Injected:</strong> DbContext provided by caller (e.g., from DI container).
Caller owns lifecycle, adapter does NOT dispose.</li><li><strong>Factory:</strong> DbContext created via factory function on each operation.
Adapter owns lifecycle, disposes after operation.</li></ul>
<p>
<strong>Storage Traits:</strong>
</p>
<ul><li>RequiresNetwork: true (database access requires network/connection)</li><li>IsTransactional: true (supports rollback via EF Core transactions)</li><li>CanStream: true (supports streaming queries via IAsyncEnumerable)</li><li>CanWrite: true by default; constrain at catalog level for read-only entries</li></ul>
<p>
<strong>Empty Data Validation:</strong>
</p>
<p>
By default, empty tables are considered invalid during pre-flight validation.
Set <code>allowEmptyData: true</code> when creating the catalog entry to allow empty tables.
This is useful for scenarios where a table may legitimately be empty (e.g., audit logs,
optional lookups, or incremental data pipelines).
</p>
<p>
<strong>Pre-flight Validation:</strong>
</p>
<p>
The Exists() operation checks table existence. For auto-migration scenarios,
consider running migrations in a dedicated pipeline setup step before data processing.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_EFCoreStorageAdapter_1__ctor_Microsoft_EntityFrameworkCore_DbContext_System_Boolean_System_Func_System_Linq_IQueryable__0__System_Linq_IQueryable__0___System_Func_Microsoft_EntityFrameworkCore_DbContext_System_Collections_Generic_IEnumerable__0__System_Threading_CancellationToken_System_Threading_Tasks_Task__"></a> EFCoreStorageAdapter\(DbContext, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<DbContext, IEnumerable<T\>, CancellationToken, Task\>?\)

Creates an adapter with an injected DbContext.

```csharp
public EFCoreStorageAdapter(DbContext context, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null)
```

#### Parameters

`context` [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)

DbContext instance (caller owns lifecycle)

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If true, empty tables are considered valid during validation

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

Optional function to customize the query for the entity type

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

Optional function to customize the save operation

#### Remarks

To create a read-only catalog entry, use <code>.Constrain(traits =&gt; traits with { CanWrite = false })</code>
on the catalog entry after construction.

### <a id="Flowthru_Core_Data_Storage_EFCoreStorageAdapter_1__ctor_System_Func_Microsoft_EntityFrameworkCore_DbContext__System_Boolean_System_Func_System_Linq_IQueryable__0__System_Linq_IQueryable__0___System_Func_Microsoft_EntityFrameworkCore_DbContext_System_Collections_Generic_IEnumerable__0__System_Threading_CancellationToken_System_Threading_Tasks_Task__"></a> EFCoreStorageAdapter\(Func<DbContext\>, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<DbContext, IEnumerable<T\>, CancellationToken, Task\>?\)

Creates an adapter with a DbContext factory.

```csharp
public EFCoreStorageAdapter(Func<DbContext> contextFactory, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null)
```

#### Parameters

`contextFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)\>

Factory function to create DbContext instances

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If true, empty tables are considered valid during validation

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

Optional function to customize the query for the entity type

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

Optional function to customize the save operation

#### Remarks

To create a read-only catalog entry, use <code>.Constrain(traits =&gt; traits with { CanWrite = false })</code>
on the catalog entry after construction.

## Properties

### <a id="Flowthru_Core_Data_Storage_EFCoreStorageAdapter_1_Traits"></a> Traits

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

### <a id="Flowthru_Core_Data_Storage_EFCoreStorageAdapter_1_DefaultSave_Microsoft_EntityFrameworkCore_DbContext_System_Collections_Generic_IEnumerable__0__System_Threading_CancellationToken_"></a> DefaultSave\(DbContext, IEnumerable<T\>, CancellationToken\)

Default save strategy: replaces all rows with the new data.
Reference this explicitly when composing with a custom save delegate
(e.g., "use default load but custom save").

```csharp
public static Task DefaultSave(DbContext context, IEnumerable<T> data, CancellationToken ct)
```

#### Parameters

`context` [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)

`data` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>

`ct` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)

### <a id="Flowthru_Core_Data_Storage_EFCoreStorageAdapter_1_Exists"></a> Exists\(\)

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

### <a id="Flowthru_Core_Data_Storage_EFCoreStorageAdapter_1_InspectDeep"></a> InspectDeep\(\)

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

### <a id="Flowthru_Core_Data_Storage_EFCoreStorageAdapter_1_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

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

### <a id="Flowthru_Core_Data_Storage_EFCoreStorageAdapter_1_Load"></a> Load\(\)

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

### <a id="Flowthru_Core_Data_Storage_EFCoreStorageAdapter_1_Save_System_Collections_Generic_IEnumerable__0__"></a> Save\(IEnumerable<T\>\)

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

