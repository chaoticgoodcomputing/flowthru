# <a id="Flowthru_Extensions_Python_Execution_SubprocessPythonExecutor"></a> Class SubprocessPythonExecutor

Namespace: [Flowthru.Extensions.Python.Execution](Flowthru.Extensions.Python.Execution.md)  
Assembly: Flowthru.Extensions.Python.dll  

Subprocess Python executor — spawns an isolated Python worker process per instance.

```csharp
public sealed class SubprocessPythonExecutor : IPythonExecutor, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SubprocessPythonExecutor](Flowthru.Extensions.Python.Execution.SubprocessPythonExecutor.md)

#### Implements

[IPythonExecutor](Flowthru.Extensions.Python.Execution.IPythonExecutor.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Each <code>SubprocessPythonExecutor</code> owns one child Python process. Isolation is at the
OS process boundary: separate interpreter, <code>sys.modules</code>, venv, and memory space.
No Python.NET, no GIL management on the C# side.
</p>
<p>
<strong>Protocol:</strong> newline-delimited JSON over stdin/stdout.
Tabular data is exchanged as base64-encoded Apache Arrow IPC buffers.
Dtype coercion specs are serialized from <xref href="Flowthru.Extensions.Python.Marshalling.ArrowSchemaMapper.BuildDtypeSpecDictionary%60%601" data-throw-if-not-resolved="false"></xref>.
</p>
<p>
The worker script (<code>flowthru_worker.py</code>) must be present in
<xref href="System.AppContext.BaseDirectory" data-throw-if-not-resolved="false"></xref>.
</p>

## Constructors

### <a id="Flowthru_Extensions_Python_Execution_SubprocessPythonExecutor__ctor_Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions_Microsoft_Extensions_Logging_ILogger_Flowthru_Extensions_Python_Execution_SubprocessPythonExecutor__"></a> SubprocessPythonExecutor\(PythonRuntimeOptions, ILogger<SubprocessPythonExecutor\>\)

Initializes a new instance of the <xref href="Flowthru.Extensions.Python.Execution.SubprocessPythonExecutor" data-throw-if-not-resolved="false"></xref> class with the specified options and logger.
The Python worker process is started lazily upon the first call to <xref href="Flowthru.Extensions.Python.Execution.SubprocessPythonExecutor.Invoke%60%602(System.String%2cSystem.String%2c%60%600)" data-throw-if-not-resolved="false"></xref> or <xref href="Flowthru.Extensions.Python.Execution.SubprocessPythonExecutor.ValidateStep(System.String%2cSystem.String)" data-throw-if-not-resolved="false"></xref>.

```csharp
public SubprocessPythonExecutor(PythonRuntimeOptions options, ILogger<SubprocessPythonExecutor> logger)
```

#### Parameters

`options` [PythonRuntimeOptions](Flowthru.Extensions.Python.Runtime.PythonRuntimeOptions.md)

The Python runtime options.

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger\-1)<[SubprocessPythonExecutor](Flowthru.Extensions.Python.Execution.SubprocessPythonExecutor.md)\>

The logger instance.

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

## Methods

### <a id="Flowthru_Extensions_Python_Execution_SubprocessPythonExecutor_Dispose"></a> Dispose\(\)

Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.

```csharp
public void Dispose()
```

### <a id="Flowthru_Extensions_Python_Execution_SubprocessPythonExecutor_Invoke__2_System_String_System_String___0_"></a> Invoke<TInput, TOutput\>\(string, string, TInput\)

Invokes a Python function, marshalling input and output to/from C# types.

```csharp
public TOutput Invoke<TInput, TOutput>(string moduleName, string functionName, TInput input)
```

#### Parameters

`moduleName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted module name (e.g., <code>"Flows.DataScience.train_model"</code>).
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

### <a id="Flowthru_Extensions_Python_Execution_SubprocessPythonExecutor_ValidateStep_System_String_System_String_"></a> ValidateStep\(string, string\)

Validates that a Python step exists and satisfies Flowthru's <code>@step</code> contract.

```csharp
public void ValidateStep(string moduleName, string functionName)
```

#### Parameters

`moduleName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Dotted Python module name.

`functionName` [string](https://learn.microsoft.com/dotnet/api/system.string)

Python function name within the module.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if the module is not importable, the function is missing, or the
<code>@step</code> decorator is absent.

