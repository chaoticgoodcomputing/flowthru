# <a id="Flowthru_Extensions_Python_Runtime_PythonRuntime"></a> Class PythonRuntime

Namespace: [Flowthru.Extensions.Python.Runtime](Flowthru.Extensions.Python.Runtime.md)  
Assembly: Flowthru.Extensions.Python.dll  

Manages the Python runtime lifecycle and GIL context.

```csharp
public sealed class PythonRuntime : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PythonRuntime](Flowthru.Extensions.Python.Runtime.PythonRuntime.md)

#### Implements

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
Wraps Python.NET's <xref href="Python.Runtime.PythonEngine" data-throw-if-not-resolved="false"></xref> initialization and shutdown.
Registered as a singleton in DI — only one Python runtime per application.
</p>
<p>
Thread-safety: Python.NET manages GIL acquisition internally.
Use <xref href="Flowthru.Extensions.Python.Runtime.PythonRuntime.AcquireGil" data-throw-if-not-resolved="false"></xref> to ensure thread-safe access to Python objects.
</p>

## Constructors

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntime__ctor_Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions_Microsoft_Extensions_Logging_ILogger_Flowthru_Extensions_Python_Runtime_PythonRuntime__"></a> PythonRuntime\(PythonRuntimeOptions, ILogger<PythonRuntime\>\)

Initializes a new instance of <xref href="Flowthru.Extensions.Python.Runtime.PythonRuntime" data-throw-if-not-resolved="false"></xref>.

```csharp
public PythonRuntime(PythonRuntimeOptions options, ILogger<PythonRuntime> logger)
```

#### Parameters

`options` [PythonRuntimeOptions](Flowthru.Extensions.Python.Runtime.PythonRuntimeOptions.md)

`logger` [ILogger](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger\-1)<[PythonRuntime](Flowthru.Extensions.Python.Runtime.PythonRuntime.md)\>

## Methods

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntime_AcquireGil"></a> AcquireGil\(\)

Acquires the Python GIL (Global Interpreter Lock).

```csharp
public IDisposable AcquireGil()
```

#### Returns

 [IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

A disposable GIL token. Dispose to release the GIL.

#### Remarks

<p>
Use this to bracket any Python.NET interop code:
<pre><code class="lang-csharp">using (runtime.AcquireGil())
{
    dynamic module = Py.Import("my_module");
    var result = module.my_function(42);
}</code></pre>
</p>
<p>
Python.NET's GIL management is thread-safe — multiple threads can acquire/release
the GIL, but only one thread executes Python code at a time.
</p>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if <xref href="Flowthru.Extensions.Python.Runtime.PythonRuntime.Initialize" data-throw-if-not-resolved="false"></xref> has not been called.

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntime_Dispose"></a> Dispose\(\)

Disposes the Python runtime wrapper.

```csharp
public void Dispose()
```

#### Remarks

<p>
Does not call <code>PythonEngine.Shutdown()</code> — Python.NET initializes globally once per process,
and explicit shutdown during process teardown is redundant. The OS reclaims all resources on exit.
</p>
<p>
Additionally, <code>PythonEngine.Shutdown()</code> attempts to serialize runtime state using
<code>BinaryFormatter</code>, which has been removed in .NET 10+ (see https://aka.ms/binaryformatter).
Since the serialized state is never restored, shutdown is both unnecessary and incompatible.
</p>
<p>
After disposal, <xref href="Flowthru.Extensions.Python.Runtime.PythonRuntime.AcquireGil" data-throw-if-not-resolved="false"></xref> will throw <xref href="System.ObjectDisposedException" data-throw-if-not-resolved="false"></xref>.
</p>

### <a id="Flowthru_Extensions_Python_Runtime_PythonRuntime_Initialize"></a> Initialize\(\)

Initializes the Python runtime if not already initialized.

```csharp
public void Initialize()
```

#### Remarks

<p>
Idempotent — safe to call multiple times.
Applies configuration from <xref href="Flowthru.Extensions.Python.Runtime.PythonRuntimeOptions" data-throw-if-not-resolved="false"></xref> on first call.
</p>
<p>
Sets:
<ul><li><code>PYTHONNET_PYDLL</code> from resolved DLL path</li><li><code>PYTHONHOME</code> from resolved venv path (if applicable)</li><li><code>sys.path</code> from resolved module search paths</li></ul>
</p>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if Python runtime cannot be initialized (missing DLL, ABI mismatch, etc.).

