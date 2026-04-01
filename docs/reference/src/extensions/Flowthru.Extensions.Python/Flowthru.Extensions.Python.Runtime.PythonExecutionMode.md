# <a id="Flowthru_Extensions_Python_Runtime_PythonExecutionMode"></a> Enum PythonExecutionMode

Namespace: [Flowthru.Extensions.Python.Runtime](Flowthru.Extensions.Python.Runtime.md)  
Assembly: Flowthru.Extensions.Python.dll  

Controls how Python node execution is isolated between FlowthruService instances.

```csharp
public enum PythonExecutionMode
```

## Fields

`InProcess = 0` 

Executes Python nodes in the same process via Python.NET.
Fast (no IPC overhead), but all services share one Python interpreter,
<code>sys.modules</code>, and GIL. Use when co-hosted pipelines are known to be compatible.



`Subprocess = 1` 

Executes Python nodes in an isolated child process per FlowthruService.
Each service gets its own Python interpreter, venv, <code>sys.path</code>, and module cache.
Default for multi-service scenarios.



