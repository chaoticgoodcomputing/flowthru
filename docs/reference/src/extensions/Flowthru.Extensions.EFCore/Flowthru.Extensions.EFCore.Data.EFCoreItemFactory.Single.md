# <a id="Flowthru_Extensions_EFCore_Data_EFCoreItemFactory_Single"></a> Class EFCoreItemFactory.Single

Namespace: [Flowthru.Extensions.EFCore.Data](Flowthru.Extensions.EFCore.Data.md)  
Assembly: Flowthru.Extensions.EFCore.dll  

```csharp
public static class EFCoreItemFactory.Single
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EFCoreItemFactory.Single](Flowthru.Extensions.EFCore.Data.EFCoreItemFactory.Single.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Extensions_EFCore_Data_EFCoreItemFactory_Single_EFCore__1_System_String_Microsoft_EntityFrameworkCore_DbContext_System_Boolean_System_Func_System_Linq_IQueryable___0__System_Linq_IQueryable___0___System_Func_Microsoft_EntityFrameworkCore_DbContext___0_System_Threading_CancellationToken_System_Threading_Tasks_Task__"></a> EFCore<T\>\(string, DbContext, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<DbContext, T, CancellationToken, Task\>?\)

Creates an Entity Framework Core catalog entry for single database-backed entities.

```csharp
public static Item<T> EFCore<T>(string label, DbContext context, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<DbContext, T, CancellationToken, Task>? saveFunc = null) where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`context` [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)

DbContext instance (caller owns lifecycle)

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), T, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

#### Returns

 Item<T\>

Catalog entry for EFCore single entity storage

#### Type Parameters

`T` 

Entity type (must be a class configured in DbContext)

#### Examples

<pre><code class="lang-csharp">// In catalog
public IItem&lt;ModelMetrics&gt; Metrics(DbContext db) =&gt;
  ItemFactory.Single.EFCore&lt;ModelMetrics&gt;("metrics", db);

// In pipeline
var pipeline = new FlowBuilder("MetricsPipeline")
  .AddStep("save_metrics", catalog =&gt; new SaveMetricsStep(
    outputs: catalog.Metrics(db)
  ))
  .Build();</code></pre>

#### Remarks

<p>
<strong>Use Case:</strong> Store single entities (models, metrics, configs) in database
</p>
<p>
<strong>Implementation:</strong> Stores entity in a table, expects exactly one row on Load.
Save replaces the single row (clear table, insert new row).
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

### <a id="Flowthru_Extensions_EFCore_Data_EFCoreItemFactory_Single_EFCore__1_System_String_System_Func_Microsoft_EntityFrameworkCore_DbContext__System_Boolean_System_Func_System_Linq_IQueryable___0__System_Linq_IQueryable___0___System_Func_Microsoft_EntityFrameworkCore_DbContext___0_System_Threading_CancellationToken_System_Threading_Tasks_Task__"></a> EFCore<T\>\(string, Func<DbContext\>, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<DbContext, T, CancellationToken, Task\>?\)

Creates an Entity Framework Core catalog entry for single database-backed entities using a factory.

```csharp
public static Item<T> EFCore<T>(string label, Func<DbContext> contextFactory, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<DbContext, T, CancellationToken, Task>? saveFunc = null) where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`contextFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)\>

Factory that creates DbContext instances (adapter owns lifecycle)

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), T, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

#### Returns

 Item<T\>

Catalog entry for EFCore single entity storage

#### Type Parameters

`T` 

Entity type (must be a class configured in DbContext)

#### Examples

<pre><code class="lang-csharp">// In catalog with factory
private readonly IServiceProvider _serviceProvider;

public IItem&lt;ModelMetrics&gt; Metrics =&gt;
  ItemFactory.Single.EFCore&lt;ModelMetrics&gt;(
    "metrics",
    () =&gt; _serviceProvider.GetRequiredService&lt;MyDbContext&gt;()
  );</code></pre>

#### Remarks

<p>
<strong>Use Case:</strong> Store single entities when you want adapter to manage DbContext lifecycle
</p>
<p>
<strong>DbContext Lifecycle:</strong> Adapter creates and disposes DbContext per operation.
Use this overload when operations should be isolated or when DbContext is expensive to keep alive.
</p>
<p>
<strong>Read-Only Entries:</strong>
To create a read-only catalog entry, apply a constraint:
<code>.Constrain(traits =&gt; traits with { CanWrite = false })</code>
</p>

### <a id="Flowthru_Extensions_EFCore_Data_EFCoreItemFactory_Single_EFCore__2_System_String_System_Func___1__System_Boolean_System_Func_System_Linq_IQueryable___0__System_Linq_IQueryable___0___System_Func___1___0_System_Threading_CancellationToken_System_Threading_Tasks_Task__"></a> EFCore<T, TContext\>\(string, Func<TContext\>, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<TContext, T, CancellationToken, Task\>?\)

Creates a single-entity EFCore catalog entry with a typed DbContext factory.
The concrete <code class="typeparamref">TContext</code> flows through to the save delegate,
eliminating any downcast inside the delegate body.

```csharp
public static Item<T> EFCore<T, TContext>(string label, Func<TContext> contextFactory, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<TContext, T, CancellationToken, Task>? saveFunc = null) where T : class where TContext : DbContext
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`contextFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<TContext\>

Typed factory; called per Load/Save operation

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If true, an empty table passes validation (default: false)

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

Optional query transformation applied before SingleAsync

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<TContext, T, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

Optional save delegate receiving the concrete <code class="typeparamref">TContext</code>.
    Defaults to clear-and-insert when null.

#### Returns

 Item<T\>

Catalog entry for EFCore single entity storage

#### Type Parameters

`T` 

Entity type

`TContext` 

Concrete DbContext type

### <a id="Flowthru_Extensions_EFCore_Data_EFCoreItemFactory_Single_EFCore__2_System_String_Microsoft_EntityFrameworkCore_IDbContextFactory___1__System_Boolean_System_Func_System_Linq_IQueryable___0__System_Linq_IQueryable___0___System_Func___1___0_System_Threading_CancellationToken_System_Threading_Tasks_Task__"></a> EFCore<T, TContext\>\(string, IDbContextFactory<TContext\>, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<TContext, T, CancellationToken, Task\>?\)

Creates a single-entity EFCore catalog entry using <xref href="Microsoft.EntityFrameworkCore.IDbContextFactory%601" data-throw-if-not-resolved="false"></xref>.

```csharp
public static Item<T> EFCore<T, TContext>(string label, IDbContextFactory<TContext> contextFactory, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<TContext, T, CancellationToken, Task>? saveFunc = null) where T : class where TContext : DbContext
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution

`contextFactory` [IDbContextFactory](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.idbcontextfactory\-1)<TContext\>

EFCore context factory; a fresh context is created per Load/Save operation

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If true, an empty table passes validation (default: false)

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

Optional query transformation applied before SingleAsync

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<TContext, T, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

Optional save delegate receiving the concrete <code class="typeparamref">TContext</code>.
    Defaults to clear-and-insert when null.

#### Returns

 Item<T\>

Catalog entry for EFCore single entity storage

#### Type Parameters

`T` 

Entity type

`TContext` 

Concrete DbContext type

