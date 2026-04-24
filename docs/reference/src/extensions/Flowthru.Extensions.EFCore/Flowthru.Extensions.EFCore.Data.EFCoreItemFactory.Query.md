# <a id="Flowthru_Extensions_EFCore_Data_EFCoreItemFactory_Query"></a> Class EFCoreItemFactory.Query

Namespace: [Flowthru.Extensions.EFCore.Data](Flowthru.Extensions.EFCore.Data.md)  
Assembly: Flowthru.Extensions.EFCore.dll  

Factory methods for <code>IItem&lt;IEnumerable&lt;T&gt;&gt;</code> entries backed by a deferred
<xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601" data-throw-if-not-resolved="false"></xref> handle.

```csharp
public static class EFCoreItemFactory.Query
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EFCoreItemFactory.Query](Flowthru.Extensions.EFCore.Data.EFCoreItemFactory.Query.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Entries created by this factory return a <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601" data-throw-if-not-resolved="false"></xref> when loaded — no rows are
fetched from the database until a step iterates the value or calls
<xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.ToListAsync(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref>. This makes the entries behaviorally lazy: pre-flight
only probes table existence; step bodies execute the query on demand.
</p>
<p>
The outer catalog type is <code>IItem&lt;IEnumerable&lt;T&gt;&gt;</code>, identical to
<xref href="Flowthru.Extensions.EFCore.Data.EFCoreItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref> entries, so changing a catalog entry from
<code>EFCoreItemFactory.Enumerable.EFCore</code> to <code>EFCoreItemFactory.Query.EFCore</code>
defers reads without requiring any step code changes.
</p>
<p>
<strong>Save behaviour:</strong> Steps that return a <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601" data-throw-if-not-resolved="false"></xref> to a query
catalog entry trigger a server-side fused INSERT-FROM-SELECT when source and destination
share the same <xref href="Flowthru.Extensions.EFCore.Data.DbScope" data-throw-if-not-resolved="false"></xref>. Steps that return a plain <code>IEnumerable&lt;T&gt;</code>
(e.g. preprocessing steps) use the standard RemoveRange + AddRange path.
</p>
<p>
Compare with <xref href="Flowthru.Extensions.EFCore.Data.EFCoreItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>: those factories eagerly materialise the full dataset
inside the catalog layer. Use <code>Query</code> factory entries when the dataset is large and
step-level filtering should avoid pulling unnecessary rows, or when the general principle
of pushing the materialisation decision to the step is preferred.
</p>

## Methods

### <a id="Flowthru_Extensions_EFCore_Data_EFCoreItemFactory_Query_EFCore__1_System_String_Microsoft_EntityFrameworkCore_DbContext_System_Boolean_System_Func_System_Linq_IQueryable___0__System_Linq_IQueryable___0___System_Func_Microsoft_EntityFrameworkCore_DbContext_System_Collections_Generic_IEnumerable___0__System_Threading_CancellationToken_System_Threading_Tasks_Task__Flowthru_Extensions_EFCore_Data_DbScope_"></a> EFCore<T\>\(string, DbContext, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<DbContext, IEnumerable<T\>, CancellationToken, Task\>?, DbScope?\)

Creates a deferred EFCore catalog entry using an injected <xref href="Microsoft.EntityFrameworkCore.DbContext" data-throw-if-not-resolved="false"></xref>.
The caller owns the context lifecycle.

```csharp
public static Item<IEnumerable<T>> EFCore<T>(string label, DbContext context, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null, DbScope? scope = null) where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution.

`context` [DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)

DbContext instance; caller manages lifecycle.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty table passes validation.

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

Optional query transformation applied before the handle is returned.

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

Optional save delegate. Defaults to RemoveRange + AddRange when <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>.

`scope` [DbScope](Flowthru.Extensions.EFCore.Data.DbScope.md)?

Database scope used for the fused save path.
Defaults to <xref href="Flowthru.Extensions.EFCore.Data.DbScope.Inferred(System.Object)" data-throw-if-not-resolved="false"></xref> keyed on <code class="paramref">context</code>.

#### Returns

 Item<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

#### Type Parameters

`T` 

Entity type (must be a class configured in the DbContext).

### <a id="Flowthru_Extensions_EFCore_Data_EFCoreItemFactory_Query_EFCore__1_System_String_System_Func_Microsoft_EntityFrameworkCore_DbContext__System_Boolean_System_Func_System_Linq_IQueryable___0__System_Linq_IQueryable___0___System_Func_Microsoft_EntityFrameworkCore_DbContext_System_Collections_Generic_IEnumerable___0__System_Threading_CancellationToken_System_Threading_Tasks_Task__Flowthru_Extensions_EFCore_Data_DbScope_"></a> EFCore<T\>\(string, Func<DbContext\>, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<DbContext, IEnumerable<T\>, CancellationToken, Task\>?, DbScope?\)

Creates a deferred EFCore catalog entry using an untyped <xref href="Microsoft.EntityFrameworkCore.DbContext" data-throw-if-not-resolved="false"></xref> factory.
A fresh context is created and disposed per operation.

```csharp
public static Item<IEnumerable<T>> EFCore<T>(string label, Func<DbContext> contextFactory, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null, DbScope? scope = null) where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution.

`contextFactory` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext)\>

Factory that creates a new <xref href="Microsoft.EntityFrameworkCore.DbContext" data-throw-if-not-resolved="false"></xref> per operation.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty table passes validation.

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

Optional query transformation applied before the handle is returned.

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

Optional save delegate. Defaults to RemoveRange + AddRange when <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>.

`scope` [DbScope](Flowthru.Extensions.EFCore.Data.DbScope.md)?

Database scope used for the fused save path.
Defaults to <xref href="Flowthru.Extensions.EFCore.Data.DbScope.Inferred(System.Object)" data-throw-if-not-resolved="false"></xref> keyed on <code class="paramref">contextFactory</code>.

#### Returns

 Item<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

#### Type Parameters

`T` 

Entity type (must be a class configured in the DbContext).

### <a id="Flowthru_Extensions_EFCore_Data_EFCoreItemFactory_Query_EFCore__2_System_String_Microsoft_EntityFrameworkCore_IDbContextFactory___1__System_Boolean_System_Func_System_Linq_IQueryable___0__System_Linq_IQueryable___0___System_Func___1_System_Collections_Generic_IEnumerable___0__System_Threading_CancellationToken_System_Threading_Tasks_Task__Flowthru_Extensions_EFCore_Data_DbScope_"></a> EFCore<T, TContext\>\(string, IDbContextFactory<TContext\>, bool, Func<IQueryable<T\>, IQueryable<T\>\>?, Func<TContext, IEnumerable<T\>, CancellationToken, Task\>?, DbScope?\)

Creates a deferred EFCore catalog entry using <xref href="Microsoft.EntityFrameworkCore.IDbContextFactory%601" data-throw-if-not-resolved="false"></xref> —
the idiomatic EFCore pattern for per-operation context isolation and concurrent step safety.

```csharp
public static Item<IEnumerable<T>> EFCore<T, TContext>(string label, IDbContextFactory<TContext> contextFactory, bool allowEmptyData = false, Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null, Func<TContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null, DbScope? scope = null) where T : class where TContext : DbContext
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique catalog label for DAG resolution.

`contextFactory` [IDbContextFactory](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.idbcontextfactory\-1)<TContext\>

EFCore context factory; a fresh context is created per Load/Save operation.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty table passes validation.

`queryCustomizer` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>, [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<T\>\>?

Optional query transformation applied before the handle is returned.

`saveFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<TContext, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)\>?

Optional save delegate receiving the concrete <code class="typeparamref">TContext</code>.
Defaults to RemoveRange + AddRange when <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>.

`scope` [DbScope](Flowthru.Extensions.EFCore.Data.DbScope.md)?

Database scope used for the fused save path.
Defaults to <xref href="Flowthru.Extensions.EFCore.Data.DbScope.Inferred(System.Object)" data-throw-if-not-resolved="false"></xref> keyed on <code class="paramref">contextFactory</code>.

#### Returns

 Item<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

#### Type Parameters

`T` 

Entity type.

`TContext` 

Concrete DbContext type.

