# How to Add Logging and Dependencies

Flowthru uses **property injection** to maintain parameterless constructors while supporting optional dependencies.

## Add Logging to Nodes

```csharp
using Microsoft.Extensions.Logging;

public class ProcessDataNode : NodeBase<InputData, OutputData>
{
    // Optional property injection
    public ILogger<ProcessDataNode>? Logger { get; set; }
    
    protected override async Task<IEnumerable<OutputData>> Transform(
        IEnumerable<InputData> input)
    {
        Logger?.LogInformation("Processing {Count} records", input.Count());
        
        // Transform logic...
        
        Logger?.LogInformation("Completed processing");
        return result;
    }
}
```

The `FlowthruApplication` automatically injects loggers into all nodes. No manual configuration needed.

## Add Custom Services

```csharp
public class EnrichDataNode : NodeBase<RawData, EnrichedData>
{
    public ILogger<EnrichDataNode>? Logger { get; set; }
    public IExternalApiClient? ApiClient { get; set; }
    
    protected override async Task<IEnumerable<EnrichedData>> Transform(
        IEnumerable<RawData> input)
    {
        if (ApiClient == null)
            throw new InvalidOperationException("ApiClient not injected");
        
        // Use ApiClient to enrich data...
        
        return enriched;
    }
}
```

Register services in the application builder:

```csharp
FlowthruApplication.Create(args, builder =>
{
    builder
        .UseCatalog(new MyCatalog())
        .RegisterPipelines<MyPipelineRegistry>()
        .ConfigureServices(services =>
        {
            services.AddSingleton<IExternalApiClient, ExternalApiClient>();
        });
});
```

**Why Property Injection?**
- ✅ Maintains parameterless constructor (required for node factory)
- ✅ Dependencies are optional (nullable properties)
- ✅ Compatible with all execution modes (sequential, parallel, distributed)
- ✅ Testable: inject mocks directly via properties
