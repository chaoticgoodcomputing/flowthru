# <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Query"></a> Class GqlItemFactory.Query

Namespace: [Flowthru.Extensions.GQL.Data](Flowthru.Extensions.GQL.Data.md)  
Assembly: Flowthru.Extensions.GQL.dll  

Factory methods for <xref href="Flowthru.Core.Data.Item%601" data-throw-if-not-resolved="false"></xref> backed by a deferred
<xref href="Flowthru.Extensions.GQL.Data.GqlQuery%602" data-throw-if-not-resolved="false"></xref> or <xref href="Flowthru.Extensions.GQL.Data.GqlQuery%603" data-throw-if-not-resolved="false"></xref> handle.

```csharp
public static class GqlItemFactory.Query
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GqlItemFactory.Query](Flowthru.Extensions.GQL.Data.GqlItemFactory.Query.md)

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
Entries created by this factory surface a <em>deferred</em> query handle — no network
calls are made when the catalog is constructed or during pre-flight (beyond a lightweight
connectivity probe). The step that consumes the entry decides when to materialize by
calling <code>ToList</code> / <code>ToListAsync</code>, or by using the handle as an
<xref href="System.Collections.Generic.IEnumerable%601" data-throw-if-not-resolved="false"></xref> directly.
</p>
<p>
Use the <strong>filtered</strong> overloads (<code>QueryFiltered</code>,
<code>PagedQueryFiltered</code>) when your GQL operation accepts a filter input type
(e.g. a HotChocolate <code>where</code> argument). The step applies the filter via
<xref href="Flowthru.Extensions.GQL.Data.GqlQuery%603.WithFilter(%600)" data-throw-if-not-resolved="false"></xref> before materializing — the catalog
entry itself is always declared without a filter.
</p>
<p>
Compare with <xref href="Flowthru.Extensions.GQL.Data.GqlItemFactory.Enumerable" data-throw-if-not-resolved="false"></xref>: those factories eagerly materialize the full
dataset inside the catalog layer. Use <code>Query</code> factory entries for remote sources
where either (a) the dataset is large and step-level filtering avoids pulling unnecessary
data, or (b) the general principle of deferring materialization decisions to the step
is preferred.
</p>

## Methods

### <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Query_NonPaged__2_System_String_System_Func_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___0____System_Func___0_System_Collections_Generic_IEnumerable___1___System_Boolean_"></a> NonPaged<TResult, T\>\(string, Func<CancellationToken, Task<IOperationResult<TResult\>\>\>, Func<TResult, IEnumerable<T\>?\>, bool\)

Creates a deferred non-paginated GQL catalog entry.
The server is expected to return all results in a single response.

```csharp
public static Item<GqlQuery<TResult, T>> NonPaged<TResult, T>(string label, Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc, Func<TResult, IEnumerable<T>?> selectData, bool allowEmptyData = false) where TResult : class where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog entry label used in the pipeline DAG and validation messages.

`queryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate that executes the StrawberryShake query operation.

`selectData` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TResult, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>?\>

Projects the result envelope to the collection of <code class="typeparamref">T</code>.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty collection is valid during pre-flight and at materialization time.

#### Returns

 Item<[GqlQuery](Flowthru.Extensions.GQL.Data.GqlQuery\-2.md)<TResult, T\>\>

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type.

`T` 

The target element type.

### <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Query_NonPaged__3_System_String_System_Func___0_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___1____System_Func___1_System_Collections_Generic_IEnumerable___2___System_Boolean_"></a> NonPaged<TFilter, TResult, T\>\(string, Func<TFilter?, CancellationToken, Task<IOperationResult<TResult\>\>\>, Func<TResult, IEnumerable<T\>?\>, bool\)

Creates a deferred non-paginated GQL catalog entry that accepts a filter input type.
The entry is declared without a filter; steps apply one via
<xref href="Flowthru.Extensions.GQL.Data.GqlQuery%603.WithFilter(%600)" data-throw-if-not-resolved="false"></xref> before materializing.

```csharp
public static Item<GqlQuery<TFilter, TResult, T>> NonPaged<TFilter, TResult, T>(string label, Func<TFilter?, CancellationToken, Task<IOperationResult<TResult>>> queryFunc, Func<TResult, IEnumerable<T>?> selectData, bool allowEmptyData = false) where TFilter : class where TResult : class where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog entry label.

`queryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<TFilter?, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate accepting <code>(filter, cancellationToken)</code>. Pass <code>filter</code> directly to the
StrawberryShake <code>ExecuteAsync</code> call's <code>where</code> argument.

`selectData` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TResult, [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<T\>?\>

Projects the result envelope to the collection of <code class="typeparamref">T</code>.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty result set is valid.

#### Returns

 Item<[GqlQuery](Flowthru.Extensions.GQL.Data.GqlQuery\-3.md)<TFilter, TResult, T\>\>

#### Type Parameters

`TFilter` 

The StrawberryShake-generated filter input type.

`TResult` 

The StrawberryShake-generated result data type.

`T` 

The target element type.

### <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Query_PagedQuery__2_System_String_System_Func_System_String_System_Int32_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___0____Flowthru_Extensions_GQL_Data_RelayPaginationStrategy___0___1__System_Int32_System_Boolean_"></a> PagedQuery<TResult, T\>\(string, Func<string?, int, CancellationToken, Task<IOperationResult<TResult\>\>\>, RelayPaginationStrategy<TResult, T\>, int, bool\)

Creates a deferred Relay cursor-paginated GQL catalog entry.

```csharp
public static Item<GqlQuery<TResult, T>> PagedQuery<TResult, T>(string label, Func<string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc, RelayPaginationStrategy<TResult, T> pagination, int pageSize = 100, bool allowEmptyData = false) where TResult : class where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog entry label used in the pipeline DAG and validation messages.

`pagedQueryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[string](https://learn.microsoft.com/dotnet/api/system.string)?, [int](https://learn.microsoft.com/dotnet/api/system.int32), [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate accepting <code>(cursor, pageSize, cancellationToken)</code> that executes the paginated query.

`pagination` [RelayPaginationStrategy](Flowthru.Extensions.GQL.Data.RelayPaginationStrategy\-2.md)<TResult, T\>

Relay pagination strategy created via <xref href="Flowthru.Extensions.GQL.Data.Pagination.Relay%60%602(System.Func%7b%60%600%2cSystem.Collections.Generic.IEnumerable%7b%60%601%7d%7d%2cSystem.Func%7b%60%600%2cFlowthru.Extensions.GQL.Data.PageInfo%7d)" data-throw-if-not-resolved="false"></xref>.

`pageSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of items per page. Defaults to 100.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty result set is valid.

#### Returns

 Item<[GqlQuery](Flowthru.Extensions.GQL.Data.GqlQuery\-2.md)<TResult, T\>\>

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type.

`T` 

The target element type.

### <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Query_PagedQuery__2_System_String_System_Func_System_Int32_System_Int32_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___0____Flowthru_Extensions_GQL_Data_OffsetPaginationStrategy___0___1__System_Int32_System_Boolean_"></a> PagedQuery<TResult, T\>\(string, Func<int, int, CancellationToken, Task<IOperationResult<TResult\>\>\>, OffsetPaginationStrategy<TResult, T\>, int, bool\)

Creates a deferred offset-paginated GQL catalog entry.

```csharp
public static Item<GqlQuery<TResult, T>> PagedQuery<TResult, T>(string label, Func<int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc, OffsetPaginationStrategy<TResult, T> pagination, int pageSize = 100, bool allowEmptyData = false) where TResult : class where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog entry label used in the pipeline DAG and validation messages.

`pagedQueryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[int](https://learn.microsoft.com/dotnet/api/system.int32), [int](https://learn.microsoft.com/dotnet/api/system.int32), [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate accepting <code>(offset, limit, cancellationToken)</code> that executes the paginated query.

`pagination` [OffsetPaginationStrategy](Flowthru.Extensions.GQL.Data.OffsetPaginationStrategy\-2.md)<TResult, T\>

Offset pagination strategy created via <xref href="Flowthru.Extensions.GQL.Data.Pagination.Offset%60%602(System.Func%7b%60%600%2cSystem.Collections.Generic.IEnumerable%7b%60%601%7d%7d%2cSystem.Func%7b%60%600%2cSystem.Nullable%7bSystem.Int32%7d%7d)" data-throw-if-not-resolved="false"></xref>.

`pageSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of items per page. Defaults to 100.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty result set is valid.

#### Returns

 Item<[GqlQuery](Flowthru.Extensions.GQL.Data.GqlQuery\-2.md)<TResult, T\>\>

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type.

`T` 

The target element type.

### <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Query_PagedQuery__3_System_String_System_Func___0_System_String_System_Int32_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___1____Flowthru_Extensions_GQL_Data_RelayPaginationStrategy___1___2__System_Int32_System_Boolean_"></a> PagedQuery<TFilter, TResult, T\>\(string, Func<TFilter?, string?, int, CancellationToken, Task<IOperationResult<TResult\>\>\>, RelayPaginationStrategy<TResult, T\>, int, bool\)

Creates a deferred Relay cursor-paginated GQL catalog entry that accepts a filter input type.

```csharp
public static Item<GqlQuery<TFilter, TResult, T>> PagedQuery<TFilter, TResult, T>(string label, Func<TFilter?, string?, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc, RelayPaginationStrategy<TResult, T> pagination, int pageSize = 100, bool allowEmptyData = false) where TFilter : class where TResult : class where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog entry label.

`pagedQueryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-5)<TFilter?, [string](https://learn.microsoft.com/dotnet/api/system.string)?, [int](https://learn.microsoft.com/dotnet/api/system.int32), [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate accepting <code>(filter, cursor, pageSize, cancellationToken)</code>.

`pagination` [RelayPaginationStrategy](Flowthru.Extensions.GQL.Data.RelayPaginationStrategy\-2.md)<TResult, T\>

Relay pagination strategy.

`pageSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of items per page. Defaults to 100.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty result set is valid.

#### Returns

 Item<[GqlQuery](Flowthru.Extensions.GQL.Data.GqlQuery\-3.md)<TFilter, TResult, T\>\>

#### Type Parameters

`TFilter` 

The StrawberryShake-generated filter input type.

`TResult` 

The StrawberryShake-generated result data type.

`T` 

The target element type.

### <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Query_PagedQuery__3_System_String_System_Func___0_System_Int32_System_Int32_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___1____Flowthru_Extensions_GQL_Data_OffsetPaginationStrategy___1___2__System_Int32_System_Boolean_"></a> PagedQuery<TFilter, TResult, T\>\(string, Func<TFilter?, int, int, CancellationToken, Task<IOperationResult<TResult\>\>\>, OffsetPaginationStrategy<TResult, T\>, int, bool\)

Creates a deferred offset-paginated GQL catalog entry that accepts a filter input type.

```csharp
public static Item<GqlQuery<TFilter, TResult, T>> PagedQuery<TFilter, TResult, T>(string label, Func<TFilter?, int, int, CancellationToken, Task<IOperationResult<TResult>>> pagedQueryFunc, OffsetPaginationStrategy<TResult, T> pagination, int pageSize = 100, bool allowEmptyData = false) where TFilter : class where TResult : class where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog entry label.

`pagedQueryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-5)<TFilter?, [int](https://learn.microsoft.com/dotnet/api/system.int32), [int](https://learn.microsoft.com/dotnet/api/system.int32), [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate accepting <code>(filter, offset, limit, cancellationToken)</code>.

`pagination` [OffsetPaginationStrategy](Flowthru.Extensions.GQL.Data.OffsetPaginationStrategy\-2.md)<TResult, T\>

Offset pagination strategy.

`pageSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of items per page. Defaults to 100.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, an empty result set is valid.

#### Returns

 Item<[GqlQuery](Flowthru.Extensions.GQL.Data.GqlQuery\-3.md)<TFilter, TResult, T\>\>

#### Type Parameters

`TFilter` 

The StrawberryShake-generated filter input type.

`TResult` 

The StrawberryShake-generated result data type.

`T` 

The target element type.

