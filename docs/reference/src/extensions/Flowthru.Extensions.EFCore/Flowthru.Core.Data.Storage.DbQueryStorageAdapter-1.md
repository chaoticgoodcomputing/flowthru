# <a id="Flowthru_Core_Data_Storage_DbQueryStorageAdapter_1"></a> Class DbQueryStorageAdapter<T\>

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Extensions.EFCore.dll  

Storage adapter that surfaces a deferred <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601" data-throw-if-not-resolved="false"></xref> handle for reading
and handles both fused server-side and materialised fallback saves.

```csharp
public sealed class DbQueryStorageAdapter<T> : IStorageAdapter<IEnumerable<T>>, IHasEfficientCount where T : class
```

#### Type Parameters

`T` 

Entity type. Must be a class registered in the underlying DbContext.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DbQueryStorageAdapter<T\>](Flowthru.Core.Data.Storage.DbQueryStorageAdapter\-1.md)

#### Implements

IStorageAdapter<IEnumerable<T\>\>, 
IHasEfficientCount

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
<strong>Read path:</strong> <xref href="Flowthru.Core.Data.Storage.DbQueryStorageAdapter%601.Load" data-throw-if-not-resolved="false"></xref> returns a <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601" data-throw-if-not-resolved="false"></xref> handle
typed as <xref href="System.Collections.Generic.IEnumerable%601" data-throw-if-not-resolved="false"></xref>. No database I/O occurs until a step iterates the
value or calls <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.ToListAsync(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref>.
</p>
<p>
<strong>Write path:</strong> <xref href="Flowthru.Core.Data.Storage.DbQueryStorageAdapter%601.Save(System.Collections.Generic.IEnumerable%7b%600%7d)" data-throw-if-not-resolved="false"></xref> inspects the incoming value:
</p>
<ul><li>
  <strong>Fused (same DB):</strong> if the value is a <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601" data-throw-if-not-resolved="false"></xref> whose
  <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.Scope" data-throw-if-not-resolved="false"></xref> matches this adapter's scope, a single-round-trip
  <code>DELETE</code> + <code>INSERT INTO … SELECT …</code> is executed entirely on the database server.
  No rows are transferred to the application host.
</li><li>
  <strong>Materialised fallback:</strong> if the value is a plain <xref href="System.Collections.Generic.IEnumerable%601" data-throw-if-not-resolved="false"></xref>
  (e.g., from a preprocessing step) or the scopes differ, the data is materialised and
  written with a <code>RemoveRange</code> + <code>AddRange</code> round-trip.
</li></ul>
<p>
<strong>Drop-in replacement:</strong> This adapter produces <code>IItem&lt;IEnumerable&lt;T&gt;&gt;</code>
entries, the same outer type as <xref href="Flowthru.Core.Data.Storage.EFCoreStorageAdapter%601" data-throw-if-not-resolved="false"></xref>. Changing a catalog entry
from <code>EFCoreItemFactory.Enumerable.EFCore</code> to <code>EFCoreItemFactory.Query.EFCore</code>
defers all reads without requiring any step code changes.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Storage_DbQueryStorageAdapter_1__ctor_Microsoft_EntityFrameworkCore_DbContext_System_Boolean_System_Func_System_Linq_IQueryable__0__System_Linq_IQueryable__0___System_Func_Microsoft_EntityFrameworkCore_DbContext_System_Collections_Generic_IEnumerable__0__System_Threading_CancellationToken_System_Threading_Tasks_Task__Flowthru_Extensions_EFCore_Data_DbScope_"></a> DbQueryStorageAdapter\(DbContext, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<DbContext, IEnumerable<T\>, CancellationToken, Task\>?, DbScope?\)

Creates an adapter with an injected <xref href="Microsoft.EntityFrameworkCore.DbContext" data-throw-if-not-resolved="false"></xref>.
The caller owns the context lifecycle; the adapter does not dispose it.

```csharp
public DbQueryStorageAdapter(DbContext context, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null, DbScope? scope = null)
```

#### Parameters

`context` [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

`scope` [DbScope](Flowthru.Extensions.EFCore.Data.DbScope.md)?

### <a id="Flowthru_Core_Data_Storage_DbQueryStorageAdapter_1__ctor_System_Func_Microsoft_EntityFrameworkCore_DbContext__System_Boolean_System_Func_System_Linq_IQueryable__0__System_Linq_IQueryable__0___System_Func_Microsoft_EntityFrameworkCore_DbContext_System_Collections_Generic_IEnumerable__0__System_Threading_CancellationToken_System_Threading_Tasks_Task__Flowthru_Extensions_EFCore_Data_DbScope_"></a> DbQueryStorageAdapter\(Func<DbContext\>, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<DbContext, IEnumerable<T\>, CancellationToken, Task\>?, DbScope?\)

Creates an adapter with a <xref href="Microsoft.EntityFrameworkCore.DbContext" data-throw-if-not-resolved="false"></xref> factory.
A fresh context is created and disposed per Load/Save operation.

```csharp
public DbQueryStorageAdapter(Func<DbContext> contextFactory, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null, DbScope? scope = null)
```

#### Parameters

`contextFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)\>

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

`scope` [DbScope](Flowthru.Extensions.EFCore.Data.DbScope.md)?

## Properties

### <a id="Flowthru_Core_Data_Storage_DbQueryStorageAdapter_1_Traits"></a> Traits

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

### <a id="Flowthru_Core_Data_Storage_DbQueryStorageAdapter_1_DefaultSave_Microsoft_EntityFrameworkCore_DbContext_System_Collections_Generic_IEnumerable__0__System_Threading_CancellationToken_"></a> DefaultSave\(DbContext, IEnumerable<T\>, CancellationToken\)

Default save strategy: removes all existing rows then inserts the new data.
Exposed so catalog engineers can reference it when composing custom save delegates.

```csharp
public static Task DefaultSave(DbContext context, IEnumerable<T> data, CancellationToken ct)
```

#### Parameters

`context` [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)

`data` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>

`ct` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)

### <a id="Flowthru_Core_Data_Storage_DbQueryStorageAdapter_1_Exists"></a> Exists\(\)

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

### <a id="Flowthru_Core_Data_Storage_DbQueryStorageAdapter_1_InspectDeep"></a> InspectDeep\(\)

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

### <a id="Flowthru_Core_Data_Storage_DbQueryStorageAdapter_1_InspectShallow_System_Int32_"></a> InspectShallow\(int\)

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

### <a id="Flowthru_Core_Data_Storage_DbQueryStorageAdapter_1_InspectTarget"></a> InspectTarget\(\)

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

### <a id="Flowthru_Core_Data_Storage_DbQueryStorageAdapter_1_Load"></a> Load\(\)

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

### <a id="Flowthru_Core_Data_Storage_DbQueryStorageAdapter_1_Save_System_Collections_Generic_IEnumerable__0__"></a> Save\(IEnumerable<T\>\)

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

