# <a id="Flowthru_Extensions_EFCore_Data_EFCoreCatalogEntries_Enumerable"></a> Class EFCoreCatalogEntries.Enumerable

Namespace: [Flowthru.Extensions.EFCore.Data](Flowthru.Extensions.EFCore.Data.md)  
Assembly: Flowthru.Extensions.EFCore.dll  

```csharp
public static class EFCoreCatalogEntries.Enumerable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EFCoreCatalogEntries.Enumerable](Flowthru.Extensions.EFCore.Data.EFCoreCatalogEntries.Enumerable.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Extensions_EFCore_Data_EFCoreCatalogEntries_Enumerable_EFCore__1_System_String_Microsoft_EntityFrameworkCore_DbContext_System_Boolean_System_Func_System_Linq_IQueryable___0__System_Linq_IQueryable___0___System_Func_Microsoft_EntityFrameworkCore_DbContext_System_Collections_Generic_IEnumerable___0__System_Threading_CancellationToken_System_Threading_Tasks_Task__"></a> EFCore<T\>\(string, DbContext, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<DbContext, IEnumerable<T\>, CancellationToken, Task\>?\)

Creates an Entity Framework Core catalog entry for database-backed collections.

```csharp
public static CatalogEntry<IEnumerable<T>> EFCore<T>(string label, DbContext context, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null) where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`context` [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)

DbContext instance (caller owns lifecycle)

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If true, empty tables pass validation (default: false)

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

#### Returns

 CatalogEntry<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

Catalog entry for EFCore database storage

#### Type Parameters

`T` 

Entity type (must be a class configured in DbContext)

#### Examples

<pre><code class="lang-csharp">// In catalog
public static partial class DataCatalog
{
  public static ICatalogEntry&lt;IEnumerable&lt;Company&gt;&gt; Companies(DbContext db) =&gt;
    CatalogEntries.Enumerable.EFCore&lt;Company&gt;("companies", db);
}

// In pipeline
var pipeline = new PipelineBuilder("CompanyPipeline")
  .AddNode("load_companies", catalog =&gt; new LoadCompaniesNode(
    outputs: catalog.Companies(db)
  ))
  .Build();</code></pre>

#### Remarks

<p>
<strong>Use Case:</strong> Read/write entities from relational databases using EF Core
</p>
<p>
<strong>DbContext Lifecycle:</strong> Caller provides DbContext and manages its lifecycle.
Use this overload when DbContext comes from DI container or is shared across operations.
</p>
<p>
<strong>Read-Only Entries:</strong>
To create a read-only catalog entry, apply a constraint:
<code>.Constrain(traits =&gt; traits with { CanWrite = false })</code>
</p>
<p>
<strong>Empty Data Validation:</strong>
By default (allowEmptyData: false), empty tables fail pre-flight validation.
Set allowEmptyData: true for tables that may legitimately be empty.
</p>

### <a id="Flowthru_Extensions_EFCore_Data_EFCoreCatalogEntries_Enumerable_EFCore__1_System_String_System_Func_Microsoft_EntityFrameworkCore_DbContext__System_Boolean_System_Func_System_Linq_IQueryable___0__System_Linq_IQueryable___0___System_Func_Microsoft_EntityFrameworkCore_DbContext_System_Collections_Generic_IEnumerable___0__System_Threading_CancellationToken_System_Threading_Tasks_Task__"></a> EFCore<T\>\(string, Func<DbContext\>, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<DbContext, IEnumerable<T\>, CancellationToken, Task\>?\)

Creates an Entity Framework Core catalog entry with a DbContext factory.

```csharp
public static CatalogEntry<IEnumerable<T>> EFCore<T>(string label, Func<DbContext> contextFactory, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null) where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`contextFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)\>

Factory function to create DbContext instances per operation

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If true, empty tables pass validation (default: false)

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

#### Returns

 CatalogEntry<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

Catalog entry for EFCore database storage

#### Type Parameters

`T` 

Entity type (must be a class configured in DbContext)

#### Examples

<pre><code class="lang-csharp">// In catalog
public static partial class DataCatalog
{
  private static AppDbContext CreateDbContext() =&gt;
    new AppDbContext(new DbContextOptionsBuilder&lt;AppDbContext&gt;()
      .UseSqlServer(connectionString)
      .Options);

  public static ICatalogEntry&lt;IEnumerable&lt;Company&gt;&gt; Companies() =&gt;
    CatalogEntries.Enumerable.EFCore&lt;Company&gt;("companies", CreateDbContext);
}</code></pre>

#### Remarks

<p>
<strong>Use Case:</strong> When DbContext should be created fresh for each Load/Save operation
</p>
<p>
<strong>DbContext Lifecycle:</strong> Adapter creates DbContext via factory and disposes it
after each operation. Use this overload for scoped DbContext patterns.
</p>
<p>
<strong>Read-Only Entries:</strong>
To create a read-only catalog entry, apply a constraint:
<code>.Constrain(traits =&gt; traits with { CanWrite = false })</code>
</p>

### <a id="Flowthru_Extensions_EFCore_Data_EFCoreCatalogEntries_Enumerable_EFCore__2_System_String_System_Func___1__System_Boolean_System_Func_System_Linq_IQueryable___0__System_Linq_IQueryable___0___System_Func___1_System_Collections_Generic_IEnumerable___0__System_Threading_CancellationToken_System_Threading_Tasks_Task__"></a> EFCore<T, TContext\>\(string, Func<TContext\>, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<TContext, IEnumerable<T\>, CancellationToken, Task\>?\)

Creates an EFCore catalog entry with a typed DbContext factory.
The concrete <code class="typeparamref">TContext</code> flows through to the save delegate,
eliminating any downcast inside the delegate body.

```csharp
public static CatalogEntry<IEnumerable<T>> EFCore<T, TContext>(string label, Func<TContext> contextFactory, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<TContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null) where T : class where TContext : DbContext
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`contextFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<TContext\>

Typed factory; called per Load/Save operation

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If true, empty tables pass validation (default: false)

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

Optional query transformation applied before materialization (e.g. Include, Where, OrderBy)

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<TContext, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

Optional save delegate receiving the concrete <code class="typeparamref">TContext</code>.
    Defaults to RemoveRange + AddRange when null.

#### Returns

 CatalogEntry<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

Catalog entry for EFCore database storage

#### Type Parameters

`T` 

Entity type

`TContext` 

Concrete DbContext type

### <a id="Flowthru_Extensions_EFCore_Data_EFCoreCatalogEntries_Enumerable_EFCore__2_System_String_Microsoft_EntityFrameworkCore_IDbContextFactory___1__System_Boolean_System_Func_System_Linq_IQueryable___0__System_Linq_IQueryable___0___System_Func___1_System_Collections_Generic_IEnumerable___0__System_Threading_CancellationToken_System_Threading_Tasks_Task__"></a> EFCore<T, TContext\>\(string, IDbContextFactory<TContext\>, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<TContext, IEnumerable<T\>, CancellationToken, Task\>?\)

Creates an EFCore catalog entry using <xref href="Microsoft.EntityFrameworkCore.IDbContextFactory%601" data-throw-if-not-resolved="false"></xref> —
the idiomatic EFCore pattern for per-operation context isolation and concurrent node safety.

```csharp
public static CatalogEntry<IEnumerable<T>> EFCore<T, TContext>(string label, IDbContextFactory<TContext> contextFactory, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<TContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null) where T : class where TContext : DbContext
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`contextFactory` [IDbContextFactory](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.idbcontextfactory\-1)<TContext\>

EFCore context factory; a fresh context is created per Load/Save operation

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If true, empty tables pass validation (default: false)

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

Optional query transformation applied before materialization

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<TContext, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

Optional save delegate receiving the concrete <code class="typeparamref">TContext</code>.
    Defaults to RemoveRange + AddRange when null.

#### Returns

 CatalogEntry<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

Catalog entry for EFCore database storage

#### Type Parameters

`T` 

Entity type

`TContext` 

Concrete DbContext type

