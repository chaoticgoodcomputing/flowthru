# <a id="Flowthru_Extensions_GQL_Data_GqlQuery_2"></a> Class GqlQuery<TResult, T\>

Namespace: [Flowthru.Extensions.GQL.Data](Flowthru.Extensions.GQL.Data.md)  
Assembly: Flowthru.Extensions.GQL.dll  

A deferred GQL query handle — analogous to <code>TypedFrame&lt;T&gt;</code> in the Spark extension.

```csharp
public sealed class GqlQuery<TResult, T> : IEnumerable<T>, IEnumerable where TResult : class where T : class
```

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type (e.g. <code>IGetCompaniesResult</code>).

`T` 

The target element type surfaced to the step (e.g. <code>IGetCompanies_Companies</code>).

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GqlQuery<TResult, T\>](Flowthru.Extensions.GQL.Data.GqlQuery\-2.md)

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
<code>GqlQuery&lt;TResult, T&gt;</code> captures all query configuration (client delegate, pagination
strategy, page size) at catalog construction time but does <em>not</em> execute any network
calls until explicitly materialized. The catalog declares <em>what</em> to query and
<em>how</em> to paginate; steps decide <em>when</em> to materialize via
<xref href="Flowthru.Extensions.GQL.Data.GqlQuery%602.ToListAsync(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref> or <xref href="Flowthru.Extensions.GQL.Data.GqlQuery%602.ToList" data-throw-if-not-resolved="false"></xref>.
</p>
<p>
<strong>Materialization boundaries:</strong>
</p>
<ul><li>
  Explicit: call <xref href="Flowthru.Extensions.GQL.Data.GqlQuery%602.ToListAsync(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref> or <xref href="Flowthru.Extensions.GQL.Data.GqlQuery%602.ToList" data-throw-if-not-resolved="false"></xref> in your step transform.
</li><li>
  Implicit: <code>GqlQuery&lt;TResult, T&gt;</code> implements <xref href="System.Collections.Generic.IEnumerable%601" data-throw-if-not-resolved="false"></xref>, so
  LINQ operators and <code>foreach</code> trigger materialization automatically. Explicit calls
  are preferred for readability — they make the network boundary visible in step code.
</li></ul>
<p>
<strong>Filtered variant:</strong> When your GQL operation accepts a filter input type
(e.g. a HotChocolate <code>where</code> argument), use
<xref href="Flowthru.Extensions.GQL.Data.GqlQuery%603" data-throw-if-not-resolved="false"></xref> instead. It adds a
<xref href="Flowthru.Extensions.GQL.Data.GqlQuery%603.WithFilter(%600)" data-throw-if-not-resolved="false"></xref> method that returns a new handle
with the filter applied, without triggering materialization.
</p>

## Methods

### <a id="Flowthru_Extensions_GQL_Data_GqlQuery_2_GetEnumerator"></a> GetEnumerator\(\)

Returns an enumerator that iterates through the collection.

```csharp
public IEnumerator<T> GetEnumerator()
```

#### Returns

 [IEnumerator](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerator\-1)<T\>

An enumerator that can be used to iterate through the collection.

#### Remarks

Triggers materialization. Prefer <xref href="Flowthru.Extensions.GQL.Data.GqlQuery%602.ToList" data-throw-if-not-resolved="false"></xref> or <xref href="Flowthru.Extensions.GQL.Data.GqlQuery%602.ToListAsync(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref> for
explicit control over when network I/O occurs.

### <a id="Flowthru_Extensions_GQL_Data_GqlQuery_2_ToList"></a> ToList\(\)

Executes the GQL query (including all pagination pages) and returns the results as a list.
This is the primary materialization point — calling this triggers network I/O.

```csharp
public List<T> ToList()
```

#### Returns

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<T\>

### <a id="Flowthru_Extensions_GQL_Data_GqlQuery_2_ToListAsync_System_Threading_CancellationToken_"></a> ToListAsync\(CancellationToken\)

Executes the GQL query (including all pagination pages) and returns the results as a list.
This is the primary materialization point — calling this triggers network I/O.

```csharp
public Task<List<T>> ToListAsync(CancellationToken ct = default)
```

#### Parameters

`ct` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<T\>\>

