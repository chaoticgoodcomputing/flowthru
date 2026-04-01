# <a id="Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions"></a> Class PythonRuntimeOptions

Namespace: [Flowthru.Extensions.Python.Runtime](Flowthru.Extensions.Python.Runtime.md)  
Assembly: Flowthru.Extensions.Python.dll  

Configuration options for the Python runtime.

```csharp
public sealed class PythonRuntimeOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PythonRuntimeOptions](Flowthru.Extensions.Python.Runtime.PythonRuntimeOptions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Follows the .NET Options pattern for environment-specific configuration.
Resolution order mirrors <xref href="Flowthru.Configuration.FlowthruConfigurationOptions" data-throw-if-not-resolved="false"></xref>.
</p>
<p>
<strong>Developer workflow:</strong>
Run <code>uv sync</code> in your project directory during development to create <code>.venv/</code>.
During build, <code>pyproject.toml</code>, <code>uv.lock</code>, and <code>.python-version</code> are copied
to the output directory. On first run, the application automatically executes <code>uv sync --frozen</code>
in the output directory to materialize <code>.venv/</code> in-place.
</p>
<p>
<strong>Auto-detection hierarchy:</strong>
<ol><li>Explicit value set via <code>UsePython(opts =&gt; opts.PythonDll = "...")</code></li><li>Environment variable (<code>PYTHONNET_PYDLL</code> for containers/CI)</li><li>Explicit <code>VenvPath</code> override</li><li>Auto-initialization via <code>uv sync --frozen</code> in output directory</li><li>Fallback to <code>VIRTUAL_ENV</code> if set (compatibility with <code>uv run</code>)</li></ol>
</p>

## Properties

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions_ExecutionMode"></a> ExecutionMode

Controls whether Python nodes run in the same process or an isolated child process.
Defaults to <xref href="Flowthru.Extensions.Python.Runtime.PythonExecutionMode.Subprocess" data-throw-if-not-resolved="false"></xref> for per-service isolation.
Set to <xref href="Flowthru.Extensions.Python.Runtime.PythonExecutionMode.InProcess" data-throw-if-not-resolved="false"></xref> to opt in to shared-interpreter mode.

```csharp
public PythonExecutionMode ExecutionMode { get; set; }
```

#### Property Value

 [PythonExecutionMode](Flowthru.Extensions.Python.Runtime.PythonExecutionMode.md)

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions_ModuleSearchPaths"></a> ModuleSearchPaths

Directories to add to Python's <code>sys.path</code> for module resolution.

```csharp
public List<string> ModuleSearchPaths { get; set; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Remarks

<p>
If empty, resolved in order:
<ol><li><code>FLOWTHRU_PYTHON_PATH</code> environment variable (colon/semicolon-separated)</li><li>Project root (directory containing <code>.csproj</code>)</li></ol>
</p>
<p>
Python nodes at <code>Pipelines/DataScience/Nodes/train_model.py</code> are referenced as
<code>"Pipelines.DataScience.Nodes.train_model"</code> when the project root is in <code>sys.path</code>.
</p>

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions_PythonDll"></a> PythonDll

Path to the Python shared library (e.g., libpython3.12.so, python312.dll).

```csharp
public string? PythonDll { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

<p>
If not set, resolved in order:
<ol><li><code>PYTHONNET_PYDLL</code> environment variable (explicit override)</li><li>Explicit <code>VenvPath</code> override</li><li>Auto-materialized <code>.venv/</code> via <code>uv sync --frozen</code> in output directory</li><li><code>VIRTUAL_ENV</code> environment variable (compatibility with <code>uv run</code>)</li></ol>
</p>
<p>
Container deployments typically set <code>PYTHONNET_PYDLL</code> to point to system Python.
Local development and deployables use <code>uv sync</code> to create <code>.venv/</code> in-place.
</p>

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions_UvPath"></a> UvPath

Path to the <code>uv</code> executable for virtual environment initialization.

```csharp
public string UvPath { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

<p>
Defaults to <code>"uv"</code> (PATH lookup).
Set this to an absolute path for non-standard installations.
</p>
<p>
Used by auto-initialization when <code>pyproject.toml</code> and <code>uv.lock</code> exist in
the output directory. To disable auto-initialization entirely, set <code>VenvPath</code>
explicitly or set <code>PYTHONNET_PYDLL</code> to point to system Python.
</p>

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions_VenvPath"></a> VenvPath

Path to the Python virtual environment (e.g., <code>.venv/</code>).

```csharp
public string? VenvPath { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

<p>
If not set, resolved in order:
<ol><li>Auto-materialized via <code>uv sync --frozen</code> in output directory</li><li><code>VIRTUAL_ENV</code> environment variable</li><li>None (uses system Python packages)</li></ol>
</p>
<p>
Setting this property explicitly skips <code>uv sync</code> auto-initialization.
Useful for pre-built containers or custom venv management.
</p>

## Methods

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions_GetResolvedModuleSearchPaths"></a> GetResolvedModuleSearchPaths\(\)

Gets the resolved Python module search paths.

```csharp
public List<string> GetResolvedModuleSearchPaths()
```

#### Returns

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Remarks

Returns configured module search paths, or the executing assembly's base directory if none specified.
Python automatically includes site-packages from <code>VIRTUAL_ENV</code> when set.

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions_GetResolvedPythonDll"></a> GetResolvedPythonDll\(\)

Gets the resolved Python DLL path using the auto-detection hierarchy.

```csharp
public string GetResolvedPythonDll()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

<p>
Flowthru uses <code>uv</code> to manage Python environments. <code>pyproject.toml</code>, <code>uv.lock</code>,
and <code>.python-version</code> are copied to the output directory during build. On first run,
the runtime executes <code>uv sync --frozen</code> to materialize <code>.venv/</code> in-place.
</p>
<p>
Attempts resolution in order: explicit value → <code>PYTHONNET_PYDLL</code> → explicit <code>VenvPath</code> →
<code>uv sync</code> auto-init → <code>VIRTUAL_ENV</code>.
Throws <xref href="System.InvalidOperationException" data-throw-if-not-resolved="false"></xref> if no Python runtime is found.
</p>

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions_GetResolvedPythonExe"></a> GetResolvedPythonExe\(\)

Gets the Python executable path for subprocess execution.

```csharp
public string GetResolvedPythonExe()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

<p>
Resolves in the same order as <xref href="Flowthru.Extensions.Python.Runtime.PythonRuntimeOptions.GetResolvedPythonDll" data-throw-if-not-resolved="false"></xref> but returns the interpreter
binary rather than the shared library. Used by <code>SubprocessPythonExecutor</code> to spawn
the worker process.
</p>
<p>
Falls back to <code>python3</code> (Unix) or <code>python</code> (Windows) on PATH if no venv is found.
</p>

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions_GetResolvedVenvPath"></a> GetResolvedVenvPath\(\)

Gets the resolved virtual environment path.

```csharp
public string? GetResolvedVenvPath()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

<p>
Checks in order:
<ol><li>Explicit <code>VenvPath</code> property</li><li>Auto-materialized <code>.venv/</code> via <code>uv sync</code> in output directory</li><li><code>VIRTUAL_ENV</code> environment variable</li></ol>
</p>
<p>
Returns <code>null</code> if no virtual environment is configured.
</p>

