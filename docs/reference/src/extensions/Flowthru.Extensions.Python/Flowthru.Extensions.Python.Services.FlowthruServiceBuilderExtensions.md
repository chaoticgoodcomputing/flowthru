# <a id="Flowthru_Extensions_Python_Services_FlowthruServiceBuilderExtensions"></a> Class FlowthruServiceBuilderExtensions

Namespace: [Flowthru.Extensions.Python.Services](Flowthru.Extensions.Python.Services.md)  
Assembly: Flowthru.Extensions.Python.dll  

Extension methods for integrating Python support with <xref href="Flowthru.Core.Services.IFlowthruBuilder" data-throw-if-not-resolved="false"></xref>.

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

### <a id="Flowthru_Extensions_Python_Services_FlowthruServiceBuilderExtensions_UsePython_Flowthru_Core_Services_IFlowthruBuilder_"></a> UsePython\(IFlowthruBuilder\)

Registers the Python runtime with configuration bound from <code>Flowthru:Python</code>.

```csharp
public static IFlowthruBuilder UsePython(this IFlowthruBuilder builder)
```

#### Parameters

`builder` IFlowthruBuilder

The Flowthru service builder.

#### Returns

 IFlowthruBuilder

The builder for method chaining

#### Remarks

<p>
Platform defaults are applied after configuration binding:
<ul><li>Python DLL: <code>PYTHONNET_PYDLL</code> → <code>.venv/</code> via <code>uv sync</code> → <code>VIRTUAL_ENV</code></li><li>Virtual environment: <code>.venv/</code> in output directory</li><li>Module search paths: output directory</li></ul>
</p>
<p>
<strong>Example (auto-detection):</strong>
<pre><code class="lang-csharp">services.AddFlowthru(configuration, flowthru =&gt;
{
    flowthru
        .RegisterCatalog&lt;MyCatalog&gt;()
        .UsePython();
});</code></pre>
</p>

### <a id="Flowthru_Extensions_Python_Services_FlowthruServiceBuilderExtensions_UsePython_Flowthru_Core_Services_IFlowthruBuilder_System_Action_Flowthru_Extensions_Python_Runtime_PythonRuntimeOptions__"></a> UsePython\(IFlowthruBuilder, Action<PythonRuntimeOptions\>\)

Registers the Python runtime with code-first configuration overrides.

```csharp
public static IFlowthruBuilder UsePython(this IFlowthruBuilder builder, Action<PythonRuntimeOptions> configure)
```

#### Parameters

`builder` IFlowthruBuilder

The Flowthru service builder.

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[PythonRuntimeOptions](Flowthru.Extensions.Python.Runtime.PythonRuntimeOptions.md)\>

Action to override Python options after config-file binding.

#### Returns

 IFlowthruBuilder

The builder for method chaining

#### Remarks

<p>
The <code class="paramref">configure</code> callback runs after <code>Flowthru:Python</code> section
binding and platform env-var defaults, so it can selectively override specific values.
</p>
<p>
<strong>Example (explicit configuration):</strong>
<pre><code class="lang-csharp">services.AddFlowthru(configuration, flowthru =&gt;
{
    flowthru
        .RegisterCatalog&lt;MyCatalog&gt;()
        .UsePython(python =&gt;
        {
            python.PythonDll = "/usr/lib/x86_64-linux-gnu/libpython3.12.so";
            python.ModuleSearchPaths.Add("Flows");
        });
});</code></pre>
</p>

