# <a id="Flowthru_Extensions_GQL_Data_RelayPaginationStrategy_2"></a> Class RelayPaginationStrategy<TResult, T\>

Namespace: [Flowthru.Extensions.GQL.Data](Flowthru.Extensions.GQL.Data.md)  
Assembly: Flowthru.Extensions.GQL.dll  

Relay cursor-based pagination strategy. Calls the query function with advancing
cursors until <code>HasNextPage</code> is false.

```csharp
public sealed class RelayPaginationStrategy<TResult, T> : PaginationStrategy<TResult, T> where TResult : class where T : class
```

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type.

`T` 

The target element type.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PaginationStrategy<TResult, T\>](Flowthru.Extensions.GQL.Data.PaginationStrategy\-2.md) ← 
[RelayPaginationStrategy<TResult, T\>](Flowthru.Extensions.GQL.Data.RelayPaginationStrategy\-2.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

