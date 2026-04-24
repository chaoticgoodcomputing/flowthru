# <a id="Flowthru_Extensions_GQL_Data_GqlQuery_3"></a> Class GqlQuery<TFilter, TResult, T\>

Namespace: [Flowthru.Extensions.GQL.Data](Flowthru.Extensions.GQL.Data.md)  
Assembly: Flowthru.Extensions.GQL.dll  

A deferred GQL query handle that supports a typed filter input.

```csharp
public sealed class GqlQuery<TFilter, TResult, T> : IEnumerable<T>, IEnumerable where TFilter : class where TResult : class where T : class
```

#### Type Parameters

`TFilter` 

The StrawberryShake-generated filter input type (e.g. <code>TypedCustomerFilterInput</code>).

`TResult` 

The StrawberryShake-generated result data type.

`T` 

The target element type surfaced to the step.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GqlQuery<TFilter, TResult, T\>](Flowthru.Extensions.GQL.Data.GqlQuery\-3.md)

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
Extends <xref href="Flowthru.Extensions.GQL.Data.GqlQuery%602" data-throw-if-not-resolved="false"></xref> with a <xref href="Flowthru.Extensions.GQL.Data.GqlQuery%603.WithFilter(%600)" data-throw-if-not-resolved="false"></xref> method.
The filter is initially <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> (unset) — the catalog declares the query
without a filter, and the step applies one before materializing.
</p>
<p>
<strong>Usage pattern in a step:</strong>
<pre><code class="lang-csharp">// Step receives the unfiltered handle from the catalog
public static IEnumerable&lt;NetSuiteCustomerSchema&gt; Create(
    (IList&lt;string&gt; activeOrgNames,
     GqlQuery&lt;TypedCustomerFilterInput, IGetCustomersResult, IGetCustomers_Nodes&gt; customers) input)
{
    var (orgNames, customers) = input;
    return customers
        .WithFilter(new TypedCustomerFilterInput {
            Companyname = new StringOperationFilterInput { In = orgNames }
        })
        .ToList()
        .Select(MapToSchema);
}</code></pre>
</p>

## Properties

### <a id="Flowthru_Extensions_GQL_Data_GqlQuery_3_Filter"></a> Filter

The current filter applied to the query. <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> when no filter has been set.

```csharp
public TFilter? Filter { get; }
```

#### Property Value

 TFilter?

## Methods

### <a id="Flowthru_Extensions_GQL_Data_GqlQuery_3_GetEnumerator"></a> GetEnumerator\(\)

Returns an enumerator that iterates through the collection.

```csharp
public IEnumerator<T> GetEnumerator()
```

#### Returns

 [IEnumerator](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerator\-1)<T\>

An enumerator that can be used to iterate through the collection.

### <a id="Flowthru_Extensions_GQL_Data_GqlQuery_3_ToList"></a> ToList\(\)

Executes the GQL query (with the current filter, if any) and returns results as a list.
This triggers network I/O.

```csharp
public List<T> ToList()
```

#### Returns

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<T\>

### <a id="Flowthru_Extensions_GQL_Data_GqlQuery_3_ToListAsync_System_Threading_CancellationToken_"></a> ToListAsync\(CancellationToken\)

Executes the GQL query (with the current filter, if any) and returns results as a list.
This triggers network I/O.

```csharp
public Task<List<T>> ToListAsync(CancellationToken ct = default)
```

#### Parameters

`ct` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<[List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<T\>\>

### <a id="Flowthru_Extensions_GQL_Data_GqlQuery_3_WithFilter__0_"></a> WithFilter\(TFilter\)

Returns a new query handle with the specified filter applied.
Does not trigger materialization — the query is still deferred.

```csharp
public GqlQuery<TFilter, TResult, T> WithFilter(TFilter filter)
```

#### Parameters

`filter` TFilter

The filter input to apply when the query is materialized.

#### Returns

 [GqlQuery](Flowthru.Extensions.GQL.Data.GqlQuery\-3.md)<TFilter, TResult, T\>

