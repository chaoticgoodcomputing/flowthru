# <a id="Microsoft_Extensions_DependencyInjection_FlowthruServiceCollectionExtensions"></a> Class FlowthruServiceCollectionExtensions

Namespace: [Microsoft.Extensions.DependencyInjection](Microsoft.Extensions.DependencyInjection.md)  
Assembly: Flowthru.Core.dll  

Extension methods for registering Flowthru services with the DI container.

```csharp
public static class FlowthruServiceCollectionExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowthruServiceCollectionExtensions](Microsoft.Extensions.DependencyInjection.FlowthruServiceCollectionExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Microsoft_Extensions_DependencyInjection_FlowthruServiceCollectionExtensions_AddFlowthru_Microsoft_Extensions_DependencyInjection_IServiceCollection_System_Action_Flowthru_Services_FlowthruServiceBuilder__"></a> AddFlowthru\(IServiceCollection, Action<FlowthruServiceBuilder\>\)

Registers Flowthru service with the DI container.

```csharp
public static IServiceCollection AddFlowthru(this IServiceCollection services, Action<FlowthruServiceBuilder> configure)
```

#### Parameters

`services` [IServiceCollection](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection)

The service collection

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[FlowthruServiceBuilder](Flowthru.Services.FlowthruServiceBuilder.md)\>

Action to configure the Flowthru service

#### Returns

 [IServiceCollection](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection)

The service collection for method chaining

#### Remarks

<p>
This extension method provides a clean API for registering Flowthru
in any .NET application with dependency injection.
</p>
<p>
<strong>Example Usage:</strong>
<pre><code class="lang-csharp">// In Program.cs or Startup.cs
services.AddFlowthru(flowthru =&gt;
{
    flowthru.RegisterCatalog&lt;MyCatalog&gt;();
    flowthru.RegisterPipelines(catalog =&gt; new Dictionary&lt;string, Pipeline&gt;
    {
        ["my_pipeline"] = MyPipeline.Create((MyCatalog)catalog)
    });
    flowthru.ConfigureMetadata(meta =&gt;
    {
        meta.WithOutputDirectory("metadata")
            .AddProvider&lt;MermaidMetadataProvider, MermaidMetadataProviderBuilder&gt;();
    });
});

// Then inject IFlowthruService anywhere
public class MyController
{
    private readonly IFlowthruService _flowthru;

    public MyController(IFlowthruService flowthru)
    {
        _flowthru = flowthru;
    }

    public async Task&lt;IActionResult&gt; RunPipeline(string name)
    {
        var request = new PipelineExecutionRequest { FlowName = name };
        var result = await _flowthru.ExecutePipelineAsync(request);
        return Ok(result);
    }
}</code></pre>
</p>

