# <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Enumerable"></a> Class GqlItemFactory.Enumerable

Namespace: [Flowthru.Extensions.GQL.Data](Flowthru.Extensions.GQL.Data.md)  
Assembly: Flowthru.Extensions.GQL.dll  

Factory methods for <xref href="Flowthru.Core.Data.Item%601" data-throw-if-not-resolved="false"></xref> backed by a collection GraphQL query.

```csharp
public static class GqlItemFactory.Enumerable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GqlItemFactory.Enumerable](Flowthru.Extensions.GQL.Data.GqlItemFactory.Enumerable.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Enumerable_PagedQuery__2_System_String_System_Func_System_String_System_Int32_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___0____Flowthru_Extensions_GQL_Data_RelayPaginationStrategy___0___1__System_Int32_System_Boolean_"></a> PagedQuery<TResult, T\>\(string, Func<string?, int, CancellationToken, Task<IOperationResult<TResult\>\>\>, RelayPaginationStrategy<TResult, T\>, int, bool\)

Creates a Relay cursor-paginated collection catalog entry.
The adapter iterates pages until <code>HasNextPage</code> is <code>false</code>, yielding
a flat <code>IEnumerable&lt;T&gt;</code> to the pipeline.

```csharp
public static Item<IEnumerable<T>> PagedQuery<TResult, T>(string label, Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc, RelayPaginationStrategy<TResult, T> pagination, int pageSize = 100, bool allowEmptyData = false) where TResult : class where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog entry label used in the pipeline DAG and validation messages.

`pagedQueryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[string](https://learn.microsoft.com/dotnet/api/system.string)?, [int](https://learn.microsoft.com/dotnet/api/system.int32), [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate accepting <code>(cursor, pageSize, cancellationToken)</code>. Map <code>cursor</code> to
the GraphQL <code>after</code> argument and <code>pageSize</code> to <code>first</code>.

`pagination` [RelayPaginationStrategy](Flowthru.Extensions.GQL.Data.RelayPaginationStrategy\-2.md)<TResult, T\>

Relay pagination strategy created via <xref href="Flowthru.Extensions.GQL.Data.Pagination.Relay%60%602(System.Func%7b%60%600%2cSystem.Collections.Generic.IEnumerable%7b%60%601%7d%7d%2cSystem.Func%7b%60%600%2cFlowthru.Extensions.GQL.Data.PageInfo%7d)" data-throw-if-not-resolved="false"></xref>.

`pageSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Items to fetch per page. Defaults to 100.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty result set is valid during pre-flight inspection.
Defaults to <code>false</code>.

#### Returns

 Item<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type (e.g. <code>IGetSessionsResult</code>).

`T` 

The target element type (e.g. <code>GetSessions_Session</code>).

### <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Enumerable_PagedQuery__2_System_String_System_Func_System_Int32_System_Int32_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___0____Flowthru_Extensions_GQL_Data_OffsetPaginationStrategy___0___1__System_Int32_System_Boolean_"></a> PagedQuery<TResult, T\>\(string, Func<int, int, CancellationToken, Task<IOperationResult<TResult\>\>\>, OffsetPaginationStrategy<TResult, T\>, int, bool\)

Creates an offset-paginated collection catalog entry.
The adapter advances the offset until all items (per <code>getTotal</code>) are fetched
or a page returns no items, yielding a flat <code>IEnumerable&lt;T&gt;</code> to the pipeline.

```csharp
public static Item<IEnumerable<T>> PagedQuery<TResult, T>(string label, Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc, OffsetPaginationStrategy<TResult, T> pagination, int pageSize = 100, bool allowEmptyData = false) where TResult : class where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog entry label used in the pipeline DAG and validation messages.

`pagedQueryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[int](https://learn.microsoft.com/dotnet/api/system.int32), [int](https://learn.microsoft.com/dotnet/api/system.int32), [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate accepting <code>(offset, limit, cancellationToken)</code>. Map directly to the
GraphQL <code>skip</code>/<code>take</code> (or equivalent) arguments.

`pagination` [OffsetPaginationStrategy](Flowthru.Extensions.GQL.Data.OffsetPaginationStrategy\-2.md)<TResult, T\>

Offset pagination strategy created via <xref href="Flowthru.Extensions.GQL.Data.Pagination.Offset%60%602(System.Func%7b%60%600%2cSystem.Collections.Generic.IEnumerable%7b%60%601%7d%7d%2cSystem.Func%7b%60%600%2cSystem.Nullable%7bSystem.Int32%7d%7d)" data-throw-if-not-resolved="false"></xref>.

`pageSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Items to fetch per page. Defaults to 100.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty result set is valid during pre-flight inspection.
Defaults to <code>false</code>.

#### Returns

 Item<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type (e.g. <code>IGetProductsResult</code>).

`T` 

The target element type (e.g. <code>GetProducts_Product</code>).

### <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Enumerable_Query__2_System_String_System_Func_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___0____System_Func___0_System_Collections_Generic_IEnumerable___1___System_Boolean_"></a> Query<TResult, T\>\(string, Func<CancellationToken, Task<IOperationResult<TResult\>\>\>, Func<TResult, IEnumerable<T\>?\>, bool\)

Creates a non-paginated collection catalog entry from a StrawberryShake query.
The server is expected to return all results in a single response.

```csharp
public static Item<IEnumerable<T>> Query<TResult, T>(string label, Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc, Func<TResult, IEnumerable<T>?> selectData, bool allowEmptyData = false) where TResult : class where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog entry label used in the pipeline DAG and validation messages.

`queryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate that executes the StrawberryShake query operation.

`selectData` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TResult, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>?\>

Projects the result data envelope to the collection of <code class="typeparamref">T</code>.
Return <code>null</code> to yield empty (subject to <code class="paramref">allowEmptyData</code>).

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty or null result collection is valid during pre-flight inspection.
Defaults to <code>false</code>.

#### Returns

 Item<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>\>

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type (e.g. <code>IGetUsersResult</code>).

`T` 

The target element type (e.g. <code>GetUsers_User</code>).

