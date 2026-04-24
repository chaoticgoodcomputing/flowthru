# <a id="Flowthru_Extensions_EFCore_Data_DbQuery_1"></a> Class DbQuery<T\>

Namespace: [Flowthru.Extensions.EFCore.Data](Flowthru.Extensions.EFCore.Data.md)  
Assembly: Flowthru.Extensions.EFCore.dll  

A deferred EF Core query handle — analogous to <code>TypedFrame&lt;T&gt;</code> in the Spark extension.

```csharp
public sealed class DbQuery<T> : IEnumerable<T>, IEnumerable where T : class
```

#### Type Parameters

`T` 

The entity type. Must be a class registered in the underlying DbContext.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DbQuery<T\>](Flowthru.Extensions.EFCore.Data.DbQuery\-1.md)

#### Implements

[IEnumerable<T\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1), 
[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.ienumerable)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
<code>DbQuery&lt;T&gt;</code> captures all query configuration at catalog construction time but does
<em>not</em> execute any database calls until explicitly materialized. The catalog declares
<em>what</em> to query; steps decide <em>when</em> to materialize via
<xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.ToListAsync(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref> or by iterating the <xref href="System.Collections.Generic.IEnumerable%601" data-throw-if-not-resolved="false"></xref> interface.
</p>
<p>
<strong>Materialization boundaries:</strong>
</p>
<ul><li>
  Explicit: call <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.ToListAsync(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref> in your step transform.
</li><li>
  Implicit: <code>DbQuery&lt;T&gt;</code> implements <xref href="System.Collections.Generic.IEnumerable%601" data-throw-if-not-resolved="false"></xref>, so
  LINQ operators and <code>foreach</code> trigger synchronous materialization automatically.
  Explicit calls are preferred for readability — they make the database boundary visible.
</li></ul>
<p>
<strong>Fluent composition:</strong> Use <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.Where(System.Linq.Expressions.Expression%7bSystem.Func%7b%600%2cSystem.Boolean%7d%7d)" data-throw-if-not-resolved="false"></xref>, <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.OrderBy%60%601(System.Linq.Expressions.Expression%7bSystem.Func%7b%600%2c%60%600%7d%7d)" data-throw-if-not-resolved="false"></xref>,
<xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.Take(System.Int32)" data-throw-if-not-resolved="false"></xref>, <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.Skip(System.Int32)" data-throw-if-not-resolved="false"></xref> to refine the query without triggering execution.
Each method returns a new <code>DbQuery&lt;T&gt;</code> with the composed expression tree.
</p>
<p>
<strong>Type-changing projection:</strong> Use <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.Project%60%601(System.Func%7bMicrosoft.EntityFrameworkCore.DbContext%2cSystem.Linq.IQueryable%7b%60%600%7d%7d)" data-throw-if-not-resolved="false"></xref> to build a
deferred query of a different entity type on the same database and scope. This enables
steps to construct a derived <code>DbQuery&lt;TResult&gt;</code> that can be saved by a
<xref href="Flowthru.Core.Data.Storage.DbQueryStorageAdapter%601" data-throw-if-not-resolved="false"></xref> using the fused
INSERT-FROM-SELECT path.
</p>
<p>
<strong>Save semantics:</strong> <code>DbQuery&lt;T&gt;</code> values passed to
<xref href="Flowthru.Core.Data.Storage.DbQueryStorageAdapter%601.Save(System.Collections.Generic.IEnumerable%7b%600%7d)" data-throw-if-not-resolved="false"></xref> trigger a
server-side fused DELETE + INSERT-FROM-SELECT when source and destination share the
same <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.Scope" data-throw-if-not-resolved="false"></xref>. All other cases fall back to full materialization.
</p>

## Methods

### <a id="Flowthru_Extensions_EFCore_Data_DbQuery_1_GetEnumerator"></a> GetEnumerator\(\)

Returns an enumerator that iterates through the collection.

```csharp
public IEnumerator<T> GetEnumerator()
```

#### Returns

 [IEnumerator](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerator\-1)<T\>

An enumerator that can be used to iterate through the collection.

#### Remarks

Triggers synchronous materialization. Prefer <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.ToListAsync(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref> in async step
transforms to avoid blocking a thread during database I/O.

### <a id="Flowthru_Extensions_EFCore_Data_DbQuery_1_OrderBy__1_System_Linq_Expressions_Expression_System_Func__0___0___"></a> OrderBy<TKey\>\(Expression<Func<T, TKey\>\>\)

Orders ascending by <code class="paramref">keySelector</code>. Returns a new handle.

```csharp
public DbQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
```

#### Parameters

`keySelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<T, TKey\>\>

#### Returns

 [DbQuery](Flowthru.Extensions.EFCore.Data.DbQuery\-1.md)<T\>

#### Type Parameters

`TKey` 

### <a id="Flowthru_Extensions_EFCore_Data_DbQuery_1_OrderByDescending__1_System_Linq_Expressions_Expression_System_Func__0___0___"></a> OrderByDescending<TKey\>\(Expression<Func<T, TKey\>\>\)

Orders descending by <code class="paramref">keySelector</code>. Returns a new handle.

```csharp
public DbQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
```

#### Parameters

`keySelector` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<T, TKey\>\>

#### Returns

 [DbQuery](Flowthru.Extensions.EFCore.Data.DbQuery\-1.md)<T\>

#### Type Parameters

`TKey` 

### <a id="Flowthru_Extensions_EFCore_Data_DbQuery_1_Project__1_System_Func_Microsoft_EntityFrameworkCore_DbContext_System_Linq_IQueryable___0___"></a> Project<TResult\>\(Func<DbContext, IQueryable<TResult\>\>\)

Builds a deferred query of a different entity type on the same database and scope.

```csharp
public DbQuery<TResult> Project<TResult>(Func<DbContext, IQueryable<TResult>> buildProjection) where TResult : class
```

#### Parameters

`buildProjection` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[DbContext](https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontext), [IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1)<TResult\>\>

Function that builds the <xref href="System.Linq.IQueryable%601" data-throw-if-not-resolved="false"></xref> for a given context.
The context is the same database as this handle.

#### Returns

 [DbQuery](Flowthru.Extensions.EFCore.Data.DbQuery\-1.md)<TResult\>

#### Type Parameters

`TResult` 

The target entity type.

#### Remarks

<p>
Use this in step transforms when you need to construct a derived query (e.g., a JOIN
across multiple tables) that should be saved using the fused INSERT-FROM-SELECT path:
</p>
<pre><code class="lang-csharp">return shuttles.Project&lt;ModelInputSchema&gt;(ctx =&gt;
    from s in ctx.Set&lt;ShuttleSchema&gt;()
    join c in ctx.Set&lt;CompanySchema&gt;() on s.CompanyId equals c.Id
    select new ModelInputSchema { ... });</code></pre>
<p>
The returned <code>DbQuery&lt;TResult&gt;</code> inherits the <xref href="Flowthru.Extensions.EFCore.Data.DbQuery%601.Scope" data-throw-if-not-resolved="false"></xref> and context
factory of this handle, so it will match a <code>DbQueryStorageAdapter&lt;TResult&gt;</code>
configured against the same database.
</p>

### <a id="Flowthru_Extensions_EFCore_Data_DbQuery_1_Skip_System_Int32_"></a> Skip\(int\)

Skips the first <code class="paramref">count</code> rows. Returns a new handle.

```csharp
public DbQuery<T> Skip(int count)
```

#### Parameters

`count` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [DbQuery](Flowthru.Extensions.EFCore.Data.DbQuery\-1.md)<T\>

### <a id="Flowthru_Extensions_EFCore_Data_DbQuery_1_Take_System_Int32_"></a> Take\(int\)

Limits the number of rows returned. Returns a new handle.

```csharp
public DbQuery<T> Take(int count)
```

#### Parameters

`count` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [DbQuery](Flowthru.Extensions.EFCore.Data.DbQuery\-1.md)<T\>

### <a id="Flowthru_Extensions_EFCore_Data_DbQuery_1_ToListAsync_System_Threading_CancellationToken_"></a> ToListAsync\(CancellationToken\)

Executes the query and returns all matching rows as a list.
Applies <code>AsNoTracking()</code> automatically.

```csharp
public Task<List<T>> ToListAsync(CancellationToken cancellationToken = default)
```

#### Parameters

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<T\>\>

### <a id="Flowthru_Extensions_EFCore_Data_DbQuery_1_Where_System_Linq_Expressions_Expression_System_Func__0_System_Boolean___"></a> Where\(Expression<Func<T, bool\>\>\)

Filters the query. Returns a new handle; does not execute.

```csharp
public DbQuery<T> Where(Expression<Func<T, bool>> predicate)
```

#### Parameters

`predicate` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression\-1)<[Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<T, [bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>\>

#### Returns

 [DbQuery](Flowthru.Extensions.EFCore.Data.DbQuery\-1.md)<T\>

