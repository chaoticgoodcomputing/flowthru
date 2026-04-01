# <a id="Flowthru_Extensions_Python_Services_FlowthruServiceBuilderExtensions"></a> Class FlowthruServiceBuilderExtensions

Namespace: [Flowthru.Extensions.Python.Services](Flowthru.Extensions.Python.Services.md)  
Assembly: Flowthru.Extensions.Python.dll  

Extension methods for integrating Python support with <xref href="Flowthru.Services.FlowthruServiceBuilder" data-throw-if-not-resolved="false"></xref>.

```csharp
public static class FlowthruServiceBuilderExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowthruServiceBuilderExtensions](Flowthru.Extensions.Python.Services.FlowthruServiceBuilderExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Extensions_Python_Services_FlowthruServiceBuilderExtensions_UsePython_Flowthru_Services_FlowthruServiceBuilder_"></a> UsePython\(FlowthruServiceBuilder\)

Registers Python runtime and executor with default configuration.

```csharp
public static FlowthruServiceBuilder UsePython(this FlowthruServiceBuilder builder)
```

#### Parameters

`builder` FlowthruServiceBuilder

The Flowthru service builder

#### Returns

 FlowthruServiceBuilder

The builder for method chaining

#### Remarks

<p>
Uses auto-detection for all configuration:
<ul><li>Python DLL: <code>PYTHONNET_PYDLL</code> → <code>.venv/</code> → system Python</li><li>Virtual environment: <code>FLOWTHRU_PYTHON_VENV</code> → <code>.venv/</code> → none</li><li>Module search paths: <code>FLOWTHRU_PYTHON_PATH</code> → project root</li></ul>
</p>
<p>
<strong>Example (auto-detection):</strong>
<pre><code class="lang-csharp">services.AddFlowthru(flowthru =&gt;
{
    flowthru
        .RegisterCatalog&lt;MyCatalog&gt;()
        .UsePython();  // Auto-detects .venv/, project root, etc.
});</code></pre>
</p>

### <a id="Flowthru_Extensions_Python_Services_FlowthruServiceBuilderExtensions_UsePython_Flowthru_Services_FlowthruServiceBuilder_System_Action_Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions__"></a> UsePython\(FlowthruServiceBuilder, Action<PythonRuntimeOptions\>\)

Registers Python runtime and executor with custom configuration.

```csharp
public static FlowthruServiceBuilder UsePython(this FlowthruServiceBuilder builder, Action<PythonRuntimeOptions> configure)
```

#### Parameters

`builder` FlowthruServiceBuilder

The Flowthru service builder

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[PythonRuntimeOptions](Flowthru.Extensions.Python.Runtime.PythonRuntimeOptions.md)\>

Action to configure Python runtime options

#### Returns

 FlowthruServiceBuilder

The builder for method chaining

#### Remarks

<p>
Explicit configuration overrides auto-detection.
Use this for:
<ul><li>Container deployments with non-standard Python paths</li><li>Custom module search paths</li><li>Multiple Python versions (explicit DLL path)</li></ul>
</p>
<p>
<strong>Example (explicit configuration):</strong>
<pre><code class="lang-csharp">services.AddFlowthru(flowthru =&gt;
{
    flowthru
        .RegisterCatalog&lt;MyCatalog&gt;()
        .UsePython(python =&gt;
        {
            python.PythonDll = "/usr/lib/x86_64-linux-gnu/libpython3.12.so";
            python.ModuleSearchPaths.Add("Pipelines");
            python.ModuleSearchPaths.Add("SharedNodes");
        });
});</code></pre>
</p>
<p>
<strong>Example (environment-variable driven, for containers):</strong>
<pre><code class="lang-csharp">services.AddFlowthru(flowthru =&gt;
{
    flowthru
        .RegisterCatalog&lt;MyCatalog&gt;()
        .UsePython(python =&gt;
        {
            // Reads PYTHONNET_PYDLL, FLOWTHRU_PYTHON_VENV, FLOWTHRU_PYTHON_PATH
            // Auto-detection still active for unset properties
        });
});</code></pre>
</p>

