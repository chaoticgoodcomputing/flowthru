# <a id="Flowthru_Extensions_GQL_Data_OffsetPaginationStrategy_2"></a> Class OffsetPaginationStrategy<TResult, T\>

Namespace: [Flowthru.Extensions.GQL.Data](Flowthru.Extensions.GQL.Data.md)  
Assembly: Flowthru.Extensions.GQL.dll  

Offset-based pagination strategy. Calls the query function with advancing offsets
until all items indicated by <code>getTotal</code> have been fetched.

```csharp
public sealed class OffsetPaginationStrategy<TResult, T> : PaginationStrategy<TResult, T> where TResult : class where T : class
```

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type.

`T` 

The target element type.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PaginationStrategy<TResult, T\>](Flowthru.Extensions.GQL.Data.PaginationStrategy\-2.md) ← 
[OffsetPaginationStrategy<TResult, T\>](Flowthru.Extensions.GQL.Data.OffsetPaginationStrategy\-2.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

