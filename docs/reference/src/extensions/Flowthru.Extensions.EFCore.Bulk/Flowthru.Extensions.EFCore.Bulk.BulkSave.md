# <a id="Flowthru_Extensions_EFCore_Bulk_BulkSave"></a> Class BulkSave

Namespace: [Flowthru.Extensions.EFCore.Bulk](Flowthru.Extensions.EFCore.Bulk.md)  
Assembly: Flowthru.Extensions.EFCore.Bulk.dll  

Factory methods that produce <code>saveFunc</code> delegates for use with
<code>EFCoreItemFactory.Enumerable.EFCore</code>. Each method returns a
<code>Func&lt;TContext, IEnumerable&lt;T&gt;, CancellationToken, Task&gt;</code>
compatible with the existing catalog item factory signature.

```csharp
public static class BulkSave
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BulkSave](Flowthru.Extensions.EFCore.Bulk.BulkSave.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Examples

<pre><code class="lang-csharp">// In a catalog definition:
public IItem&lt;IEnumerable&lt;MyEntity&gt;&gt; OutputEntities =&gt;
    CreateItem(() =&gt; EFCoreItemFactory.Enumerable.EFCore&lt;MyEntity, MyDbContext&gt;(
        label: "OutputEntities",
        contextFactory: _factory,
        saveFunc: BulkSave.TruncateAndInsert&lt;MyEntity, MyDbContext&gt;()));</code></pre>

## Methods

### <a id="Flowthru_Extensions_EFCore_Bulk_BulkSave_Insert__2_Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions_"></a> Insert<T, TContext\>\(BulkSaveOptions?\)

Bulk insert rows. Does not modify or remove existing data.
Uses the provider's fastest bulk-load path (e.g. Npgsql binary COPY for PostgreSQL).

```csharp
public static Func<TContext, IEnumerable<T>, CancellationToken, Task> Insert<T, TContext>(BulkSaveOptions? options = null) where T : class where TContext : DbContext
```

#### Parameters

`options` [BulkSaveOptions](Flowthru.Extensions.EFCore.Bulk.BulkSaveOptions.md)?

Optional bulk operation configuration.

#### Returns

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<TContext, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>

A <code>saveFunc</code> delegate for use with <code>EFCoreItemFactory</code>.

#### Type Parameters

`T` 

The entity type.

`TContext` 

The DbContext type.

### <a id="Flowthru_Extensions_EFCore_Bulk_BulkSave_InsertOrUpdate__2_Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions_"></a> InsertOrUpdate<T, TContext\>\(BulkSaveOptions?\)

Bulk upsert: insert new rows and update existing rows matched by primary key.

```csharp
public static Func<TContext, IEnumerable<T>, CancellationToken, Task> InsertOrUpdate<T, TContext>(BulkSaveOptions? options = null) where T : class where TContext : DbContext
```

#### Parameters

`options` [BulkSaveOptions](Flowthru.Extensions.EFCore.Bulk.BulkSaveOptions.md)?

Optional bulk operation configuration.

#### Returns

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<TContext, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>

A <code>saveFunc</code> delegate for use with <code>EFCoreItemFactory</code>.

#### Type Parameters

`T` 

The entity type.

`TContext` 

The DbContext type.

### <a id="Flowthru_Extensions_EFCore_Bulk_BulkSave_InsertOrUpdateOrDelete__2_Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions_"></a> InsertOrUpdateOrDelete<T, TContext\>\(BulkSaveOptions?\)

Full sync: insert new rows, update existing rows, and delete rows not present
in the input data. Matched by primary key.

```csharp
public static Func<TContext, IEnumerable<T>, CancellationToken, Task> InsertOrUpdateOrDelete<T, TContext>(BulkSaveOptions? options = null) where T : class where TContext : DbContext
```

#### Parameters

`options` [BulkSaveOptions](Flowthru.Extensions.EFCore.Bulk.BulkSaveOptions.md)?

Optional bulk operation configuration.

#### Returns

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<TContext, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>

A <code>saveFunc</code> delegate for use with <code>EFCoreItemFactory</code>.

#### Type Parameters

`T` 

The entity type.

`TContext` 

The DbContext type.

### <a id="Flowthru_Extensions_EFCore_Bulk_BulkSave_TruncateAndInsert__2_Flowthru_Extensions_EFCore_Bulk_BulkSaveOptions_"></a> TruncateAndInsert<T, TContext\>\(BulkSaveOptions?\)

Truncate the target table, then bulk insert all rows.
This is a full-replacement strategy equivalent to the common pattern of
<code>TRUNCATE TABLE ... ; INSERT ...</code> but using the provider's bulk-load path.

```csharp
public static Func<TContext, IEnumerable<T>, CancellationToken, Task> TruncateAndInsert<T, TContext>(BulkSaveOptions? options = null) where T : class where TContext : DbContext
```

#### Parameters

`options` [BulkSaveOptions](Flowthru.Extensions.EFCore.Bulk.BulkSaveOptions.md)?

Optional bulk operation configuration.

#### Returns

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<TContext, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>

A <code>saveFunc</code> delegate for use with <code>EFCoreItemFactory</code>.

#### Type Parameters

`T` 

The entity type.

`TContext` 

The DbContext type.

