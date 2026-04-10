# <a id="Flowthru_Core_Data_Storage_EFCoreSingleStorageAdapter_1"></a> Class EFCoreSingleStorageAdapter<T\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Extensions.EFCore.dll  

Storage adapter for single Entity Framework Core entities.
Stores exactly one row in a database table.

```csharp
public sealed class EFCoreSingleStorageAdapter<T> : IStorageAdapter<T> where T : class
```

#### Type Parameters

`T` 

Entity type (must be a class type configured as EF entity)

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EFCoreSingleStorageAdapter<T\>](Flowthru.Core.Data.Storage.EFCoreSingleStorageAdapter\-1.md)

#### Implements

IStorageAdapter<T\>

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
<strong>Save Semantics:</strong> Replace - removes all existing rows and inserts the new entity.
Ensures table contains exactly one row after save.
</p>
<p>
<strong>Load Semantics:</strong> Returns the single row from the table.
Throws if table contains zero or more than one row.
</p>
<p>
<strong>Exists Semantics:</strong> Returns true if table has exactly one row.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_EFCoreSingleStorageAdapter_1__ctor_Microsoft_EntityFrameworkCore_DbContext_System_Boolean_System_Boolean_System_Func_System_Linq_IQueryable__0__System_Linq_IQueryable__0___System_Func_Microsoft_EntityFrameworkCore_DbContext__0_System_Threading_CancellationToken_System_Threading_Tasks_Task__"></a> EFCoreSingleStorageAdapter\(DbContext, bool, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<DbContext, T, CancellationToken, Task\>?\)

Creates an adapter with an injected DbContext instance.

```csharp
public EFCoreSingleStorageAdapter(DbContext context, bool ownsContext, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<DbContext, T, CancellationToken, Task>? saveFunc = null)
```

#### Parameters

`context` [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)

DbContext to use for operations

`ownsContext` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If true, adapter disposes context after operations

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If true, empty tables pass validation (default: false)

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

Optional function to customize the query for the entity type

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), T, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

Optional function to customize the save operation

#### Remarks

To create a read-only catalog entry, use <code>.Constrain(traits =&gt; traits with { CanWrite = false })</code>
on the catalog entry after construction.

### <a id="Flowthru_Core_Data_Storage_EFCoreSingleStorageAdapter_1__ctor_System_Func_Microsoft_EntityFrameworkCore_DbContext__System_Boolean_System_Func_System_Linq_IQueryable__0__System_Linq_IQueryable__0___System_Func_Microsoft_EntityFrameworkCore_DbContext__0_System_Threading_CancellationToken_System_Threading_Tasks_Task__"></a> EFCoreSingleStorageAdapter\(Func<DbContext\>, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<DbContext, T, CancellationToken, Task\>?\)

Creates an adapter with a DbContext factory.

```csharp
public EFCoreSingleStorageAdapter(Func<DbContext> contextFactory, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<DbContext, T, CancellationToken, Task>? saveFunc = null)
```

#### Parameters

`contextFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)\>

Factory to create DbContext instances

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If true, empty tables pass validation (default: false)

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

Optional function to customize the query for the entity type

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), T, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

Optional function to customize the save operation

#### Remarks

To create a read-only catalog entry, use <code>.Constrain(traits =&gt; traits with { CanWrite = false })</code>
on the catalog entry after construction.

## Properties

### <a id="Flowthru_Core_Data_Storage_EFCoreSingleStorageAdapter_1_Traits"></a> Traits

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

### <a id="Flowthru_Core_Data_Storage_EFCoreSingleStorageAdapter_1_DefaultSave_Microsoft_EntityFrameworkCore_DbContext__0_System_Threading_CancellationToken_"></a> DefaultSave\(DbContext, T, CancellationToken\)

Gets a DbContext from either the injected instance or factory.

```csharp
public static Task DefaultSave(DbContext context, T data, CancellationToken ct)
```

#### Parameters

`context` [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)

`data` T

`ct` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)

### <a id="Flowthru_Core_Data_Storage_EFCoreSingleStorageAdapter_1_Exists"></a> Exists\(\)

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

### <a id="Flowthru_Core_Data_Storage_EFCoreSingleStorageAdapter_1_InspectDeep"></a> InspectDeep\(\)

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

### <a id="Flowthru_Core_Data_Storage_EFCoreSingleStorageAdapter_1_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

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

### <a id="Flowthru_Core_Data_Storage_EFCoreSingleStorageAdapter_1_Load"></a> Load\(\)

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

### <a id="Flowthru_Core_Data_Storage_EFCoreSingleStorageAdapter_1_Save__0_"></a> Save\(T\)

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

