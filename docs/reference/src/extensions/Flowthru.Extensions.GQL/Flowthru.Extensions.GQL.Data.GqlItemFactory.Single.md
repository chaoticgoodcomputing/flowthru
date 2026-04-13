# <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Single"></a> Class GqlItemFactory.Single

Namespace: [Flowthru.Extensions.GQL.Data](Flowthru.Extensions.GQL.Data.md)  
Assembly: Flowthru.Extensions.GQL.dll  

Factory methods for <xref href="Flowthru.Core.Data.Item%601" data-throw-if-not-resolved="false"></xref> backed by a single-item GraphQL query.

```csharp
public static class GqlItemFactory.Single
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GqlItemFactory.Single](Flowthru.Extensions.GQL.Data.GqlItemFactory.Single.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Single_Query__2_System_String_System_Func_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___0____System_Func___0___1__System_Boolean_"></a> Query<TResult, T\>\(string, Func<CancellationToken, Task<IOperationResult<TResult\>\>\>, Func<TResult, T\>, bool\)

Creates a read-only single-item catalog entry from a StrawberryShake query.

```csharp
public static Item<T> Query<TResult, T>(string label, Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc, Func<TResult, T> selectData, bool allowEmptyData = false) where TResult : class where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog entry label used in the pipeline DAG and validation messages.

`queryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate that executes the StrawberryShake query operation.

`selectData` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TResult, T\>

Projects the result data envelope to the target type.
Use a null-forgiving operator (<code>r =&gt; r.Me!</code>) when the field is non-null by schema contract.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, a null <xref href="StrawberryShake.IOperationResult%601.Data" data-throw-if-not-resolved="false"></xref> is treated as
valid during pre-flight inspection. Defaults to <code>false</code>.

#### Returns

 Item<T\>

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type (e.g. <code>IGetCurrentUserResult</code>).

`T` 

The target type surfaced to the catalog entry (e.g. <code>GetCurrentUser_Me</code>).

### <a id="Flowthru_Extensions_GQL_Data_GqlItemFactory_Single_Query__2_System_String_System_Func_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___0____System_Func___0___1__System_Func___1_System_Threading_CancellationToken_System_Threading_Tasks_Task_StrawberryShake_IOperationResult___System_Boolean_"></a> Query<TResult, T\>\(string, Func<CancellationToken, Task<IOperationResult<TResult\>\>\>, Func<TResult, T\>, Func<T, CancellationToken, Task<IOperationResult\>\>, bool\)

Creates a read-write single-item catalog entry from a StrawberryShake query and mutation.

```csharp
public static Item<T> Query<TResult, T>(string label, Func<CancellationToken, Task<IOperationResult<TResult>>> queryFunc, Func<TResult, T> selectData, Func<T, CancellationToken, Task<IOperationResult>> mutationFunc, bool allowEmptyData = false) where TResult : class where T : class
```

#### Parameters

`label` [string](https://learn.microsoft.com/dotnet/api/system.string)

Catalog entry label used in the pipeline DAG and validation messages.

`queryFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult<TResult\>\>\>

Delegate that executes the StrawberryShake query operation.

`selectData` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TResult, T\>

Projects the result data envelope to the target type.

`mutationFunc` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<T, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<IOperationResult\>\>

Delegate that executes the StrawberryShake mutation when the catalog entry is saved.
Enables <code>StorageTraits.CanWrite = true</code> on the resulting entry.

`allowEmptyData` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

If <code>true</code>, a null <xref href="StrawberryShake.IOperationResult%601.Data" data-throw-if-not-resolved="false"></xref> is treated as
valid during pre-flight inspection. Defaults to <code>false</code>.

#### Returns

 Item<T\>

#### Type Parameters

`TResult` 

The StrawberryShake-generated result data type (e.g. <code>IGetCurrentUserResult</code>).

`T` 

The target type surfaced to the catalog entry (e.g. <code>GetCurrentUser_Me</code>).

