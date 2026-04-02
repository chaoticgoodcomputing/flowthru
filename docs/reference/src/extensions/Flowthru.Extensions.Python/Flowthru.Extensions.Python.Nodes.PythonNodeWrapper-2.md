# <a id="Flowthru_Extensions_Python_Nodes_PythonNodeWrapper_2"></a> Class PythonNodeWrapper<TInput, TOutput\>

Namespace: [](.md)  
Assembly: Flowthru.Extensions.Python.dll  

Thin wrapper that binds an <xref href="Flowthru.Extensions.Python.Execution.IPythonExecutor" data-throw-if-not-resolved="false"></xref> to a specific module/function pair,
exposing it as a typed <code>Func&lt;TInput, TOutput&gt;</code> for use with the pipeline builder.

```csharp
public sealed class PythonNodeWrapper<TInput, TOutput>
```

#### Type Parameters

`TInput` 

`TOutput` 

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PythonNodeWrapper<TInput, TOutput\>](.PythonNodeWrapper\-2.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

All marshalling (scalar, tabular, bytes, multi-I/O tuples) is delegated to the executor.

## Constructors

### <a id="Flowthru_Extensions_Python_Nodes_PythonNodeWrapper_2__ctor_Flowthru_Extensions_Python_Execution_IPythonExecutor_System_String_System_String_"></a> PythonNodeWrapper\(IPythonExecutor, string, string\)

```csharp
public PythonNodeWrapper(IPythonExecutor executor, string moduleName, string functionName)
```

#### Parameters

`executor` [IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

`moduleName` [string](https://learn.microsoft.com/dotnet/api/system.string)

`functionName` [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="Flowthru_Extensions_Python_Nodes_PythonNodeWrapper_2_GetTransform"></a> GetTransform\(\)

Gets the transformation function that invokes the Python node.

```csharp
public Func<TInput, TOutput> GetTransform()
```

#### Returns

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TInput, TOutput\>

A function that takes <code class="typeparamref">TInput</code> and returns <code class="typeparamref">TOutput</code>.

