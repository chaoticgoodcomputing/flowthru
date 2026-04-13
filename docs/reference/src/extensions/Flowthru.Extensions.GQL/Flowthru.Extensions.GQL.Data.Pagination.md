# <a id="Flowthru_Extensions_GQL_Data_Pagination"></a> Class Pagination

Namespace: [Flowthru.Extensions.GQL.Data](Flowthru.Extensions.GQL.Data.md)  
Assembly: Flowthru.Extensions.GQL.dll  

Factory for creating pagination strategies for paginated GQL catalog entries.

```csharp
public static class Pagination
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Pagination](Flowthru.Extensions.GQL.Data.Pagination.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Extensions_GQL_Data_Pagination_Offset__2_System_Func___0_System_Collections_Generic_IEnumerable___1___System_Func___0_System_Nullable_System_Int32___"></a> Offset<TResult, T\>\(Func<TResult, IEnumerable<T\>?\>, Func<TResult, int?\>\)

Creates an offset-based pagination strategy.

```csharp
public static OffsetPaginationStrategy<TResult, T> Offset<TResult, T>(Func<TResult, IEnumerable<T>?> getItems, Func<TResult, int?> getTotal) where TResult : class where T : class
```

#### Parameters

`getItems` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TResult, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>?\>

Selects the item collection from the result page.

`getTotal` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TResult, [int](https://learn.microsoft.com/dotnet/api/system.int32)?\>

Selects the total item count from the result (used to determine
    when to stop fetching pages). Return <code>null</code> to stop after the first empty page.

#### Returns

 [OffsetPaginationStrategy](Flowthru.Extensions.GQL.Data.OffsetPaginationStrategy\-2.md)<TResult, T\>

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type.

`T` 

The target element type.

### <a id="Flowthru_Extensions_GQL_Data_Pagination_Relay__2_System_Func___0_System_Collections_Generic_IEnumerable___1___System_Func___0_Flowthru_Extensions_GQL_Data_PageInfo__"></a> Relay<TResult, T\>\(Func<TResult, IEnumerable<T\>?\>, Func<TResult, PageInfo?\>\)

Creates a Relay cursor-based pagination strategy.

```csharp
public static RelayPaginationStrategy<TResult, T> Relay<TResult, T>(Func<TResult, IEnumerable<T>?> getNodes, Func<TResult, PageInfo?> getPageInfo) where TResult : class where T : class
```

#### Parameters

`getNodes` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TResult, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>?\>

Selects the item collection from the result page.

`getPageInfo` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TResult, [PageInfo](Flowthru.Extensions.GQL.Data.PageInfo.md)?\>

Selects the <xref href="Flowthru.Extensions.GQL.Data.PageInfo" data-throw-if-not-resolved="false"></xref> from the result page.

#### Returns

 [RelayPaginationStrategy](Flowthru.Extensions.GQL.Data.RelayPaginationStrategy\-2.md)<TResult, T\>

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type.

`T` 

The target element type.

