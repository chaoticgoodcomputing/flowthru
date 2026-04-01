# <a id="Flowthru_Extensions_Python_Execution_PythonNetExecutor"></a> Class PythonNetExecutor

Namespace: [Flowthru.Extensions.Python.Execution](Flowthru.Extensions.Python.Execution.md)  
Assembly: Flowthru.Extensions.Python.dll  

In-process Python executor using Python.NET.

```csharp
public sealed class PythonNetExecutor : IPythonExecutor
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PythonNetExecutor](Flowthru.Extensions.Python.Execution.PythonNetExecutor.md)

#### Implements

[IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Executes Python functions within the same process via Python.NET's embedded runtime.
All marshalling (scalar, tabular Arrow IPC, bytes, multi-I/O tuples) is handled
internally — callers interact only with strongly-typed C# values.
</p>
<p>
<strong>Isolation caveat:</strong>
<code>PythonEngine</code> is process-global. Multiple <code>PythonNetExecutor</code> instances share
the same interpreter, <code>sys.modules</code>, and GIL. Use <xref href="Flowthru.Extensions.Python.Execution.SubprocessPythonExecutor" data-throw-if-not-resolved="false"></xref>
when true per-service isolation is required.
</p>
<p>
Thread-safety: GIL acquisition serialises all Python execution.
</p>

## Constructors

### <a id="Flowthru_Extensions_Python_Execution_PythonNetExecutor__ctor_Flowthru_Extensions_Python_Runtime_PythonRuntime_Microsoft_Extensions_Logging_ILogger_Flowthru_Extensions_Python_Execution_PythonNetExecutor__"></a> PythonNetExecutor\(PythonRuntime, ILogger<PythonNetExecutor\>\)

```csharp
public PythonNetExecutor(PythonRuntime runtime, ILogger<PythonNetExecutor> logger)
```

#### Parameters

`runtime` [PythonRuntime](Flowthru.Extensions.Python.Runtime.PythonRuntime.md)

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger\-1)<[PythonNetExecutor](Flowthru.Extensions.Python.Execution.PythonNetExecutor.md)\>

## Methods

### <a id="Flowthru_Extensions_Python_Execution_PythonNetExecutor_Invoke__2_System_String_System_String___0_"></a> Invoke<TInput, TOutput\>\(string, string, TInput\)

Invokes a Python function, marshalling input and output to/from C# types.

```csharp
public TOutput Invoke<TInput, TOutput>(string moduleName, string functionName, TInput input)
```

#### Parameters

`moduleName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted module name (e.g., <code>"Pipelines.DataScience.train_model"</code>).
Must be resolvable via the executor's configured <code>sys.path</code>.

`functionName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module.

`input` TInput

Typed input value.

#### Returns

 TOutput

Typed output value returned by the Python function.

#### Type Parameters

`TInput` 

C# input type. May be a scalar, <code>IEnumerable&lt;TSchema&gt;</code> (tabular),
<code>byte[]</code> (raw bytes), or a <code>ValueTuple</code> of any of those (multi-input).

`TOutput` 

C# output type. Same range as <code class="typeparamref">TInput</code>.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if the module cannot be imported, the function cannot be resolved,
or marshalling fails.

### <a id="Flowthru_Extensions_Python_Execution_PythonNetExecutor_ValidateNode_System_String_System_String_"></a> ValidateNode\(string, string\)

Validates that a Python node exists and satisfies Flowthru's <code>@node</code> contract.

```csharp
public void ValidateNode(string moduleName, string functionName)
```

#### Parameters

`moduleName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name.

`functionName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if the module is not importable, the function is missing, or the
<code>@node</code> decorator is absent.

