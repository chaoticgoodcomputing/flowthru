# <a id="Flowthru_Extensions_Python_Execution_IPythonExecutor"></a> Interface IPythonExecutor

Namespace: [Flowthru.Extensions.Python.Execution](Flowthru.Extensions.Python.Execution.md)  
Assembly: Flowthru.Extensions.Python.dll  

Abstraction for executing Python code.

```csharp
public interface IPythonExecutor
```

## Remarks

<p>
Decouples the execution strategy from Python node wiring.
Two implementations ship out of the box:
<ul><li><xref href="Flowthru.Extensions.Python.Execution.PythonNetExecutor" data-throw-if-not-resolved="false"></xref> — in-process via Python.NET (opt-in)</li><li><xref href="Flowthru.Extensions.Python.Execution.SubprocessPythonExecutor" data-throw-if-not-resolved="false"></xref> — isolated child process per service (default)</li></ul>
</p>
<p>
<strong>Isolation contract:</strong>
Two FlowthruServices using <xref href="Flowthru.Extensions.Python.Execution.SubprocessPythonExecutor" data-throw-if-not-resolved="false"></xref> do not share Python state.
Each executor spawns its own Python worker process with its own venv, <code>sys.path</code>,
<code>sys.modules</code>, and GIL — complete isolation at the cost of IPC marshalling overhead.
</p>
<p>
All implementations must handle:
<ul><li>Module import and caching</li><li>Function resolution and invocation</li><li>Argument marshalling (C# ↔ Python) — scalar, tabular (Arrow IPC), and raw bytes</li><li>Error propagation (Python exceptions → <xref href="System.InvalidOperationException" data-throw-if-not-resolved="false"></xref>)</li></ul>
</p>

## Methods

### <a id="Flowthru_Extensions_Python_Execution_IPythonExecutor_Invoke__2_System_String_System_String___0_"></a> Invoke<TInput, TOutput\>\(string, string, TInput\)

Invokes a Python function, marshalling input and output to/from C# types.

```csharp
TOutput Invoke<TInput, TOutput>(string moduleName, string functionName, TInput input)
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

### <a id="Flowthru_Extensions_Python_Execution_IPythonExecutor_ValidateNode_System_String_System_String_"></a> ValidateNode\(string, string\)

Validates that a Python node exists and satisfies Flowthru's <code>@node</code> contract.

```csharp
void ValidateNode(string moduleName, string functionName)
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

