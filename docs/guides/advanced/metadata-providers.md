# Custom Metadata Providers

This guide shows how to create custom metadata providers that receive pipeline metadata automatically during execution.

## Prerequisites

You should already understand:
- Basic Flowthru pipeline structure (see [Anatomy of a Pipeline](../../explanation/anatomy-of-a-pipeline.md))
- Dependency injection with `AddFlowthru()`

## Creating a Provider

Implement `IMetadataProvider` with two members:

1. `string Name` — Display name for logs
2. `void Consume(DagMetadata dag)` — Process the metadata

Configure everything your provider needs (URLs, file paths, formats) in the constructor. The `Consume()` method receives metadata but no configuration parameters.

### Example: Dashboard Provider

```csharp
using Flowthru.Meta.Models;
using Flowthru.Meta.Providers;

public class DashboardMetadataProvider : IMetadataProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _dashboardUrl;

    public DashboardMetadataProvider(HttpClient httpClient, string dashboardUrl)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _dashboardUrl = dashboardUrl ?? throw new ArgumentNullException(nameof(dashboardUrl));
    }

    public string Name => "Dashboard";

    public void Consume(DagMetadata dag)
    {
        var payload = new
        {
            FlowName = dag.FlowName,
            StepCount = dag.Steps.Count,
            EdgeCount = dag.Edges.Count,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        _httpClient.PostAsync(_dashboardUrl, content).Wait();
    }
}
```

### Example: Test Capturing Provider

Capture metadata in memory for test assertions:

```csharp
using Flowthru.Meta.Models;
using Flowthru.Meta.Providers;

public class CapturingMetadataProvider : IMetadataProvider
{
    public DagMetadata? CapturedDag { get; private set; }
    public string Name => "CapturingMetadataProvider";

    public void Consume(DagMetadata dag) => CapturedDag = dag;
    
    public void Reset() => CapturedDag = null;
}
```

## Registering Providers

### Built-In Providers (JSON and Mermaid)

Built-in providers use generic registration with two type parameters

```csharp
using Flowthru.Meta;
using Flowthru.Meta.Providers;

services.AddFlowthru(flowthru =>
{
    flowthru.RegisterCatalog(_ => new MyCatalog());
    flowthru.RegisterPipelines(_ => myPipelines);
    
    flowthru.ConfigureMetadata(meta =>
    {
        meta.AddProvider<JsonMetadataProvider, JsonMetadataProviderBuilder>(json => json
            .WithOutputDirectory("metadata")
            .WithFilenameTemplate("dag-{FlowName}-{Timestamp}")
            .WithTimestamp("yyyyMMdd-HHmmss")
            .UseCompactFormat());
            
        meta.AddProvider<MermaidMetadataProvider, MermaidMetadataProviderBuilder>(mermaid => mermaid
            .WithOutputDirectory("metadata")
            .WithDirection(MermaidFlowchartDirection.LeftToRight));
    });
});
```

### Custom Providers

Construct your provider and pass the instance directly:

```csharp
flowthru.ConfigureMetadata(meta =>
{
    var dashboardProvider = new DashboardMetadataProvider(httpClient, "https://dashboard.example.com");
    meta.AddProvider(dashboardProvider);
});
```

## Running Providers

Providers execute automatically when `exportMetadata: true`:

```csharp
var result = await flowthruService.ExecutePipelineAsync(
    options: null,
    exportMetadata: true
);
```

If one provider fails, others continue — check logs for per-provider status.

## Retrieving Metadata Without Execution

Use `GetDagMetadata()` to retrieve metadata without running the pipeline:

```csharp
var service = serviceProvider.GetRequiredService<IFlowthruService>();

// Get merged DAG of all pipelines
var allPipelinesDag = service.GetDagMetadata();

// Get DAG for specific pipeline
var singlePipelineDag = service.GetDagMetadata(pipelineName: "DataEngineering");

// Get DAG with slicing applied
var slicedDag = service.GetDagMetadata(
    sliceStrategy: new FlowSliceStrategy
    {
        ToSteps = new HashSet<string> { "TransformStep" }
    }
);
```

Useful for tooling, tests, or debugging pipeline structure before execution.

## Configuring Built-In Providers

### JSON Provider

```csharp
meta.AddProvider<JsonMetadataProvider, JsonMetadataProviderBuilder>(json => json
    .WithOutputDirectory("metadata")
    .WithFilenameTemplate("dag-{FlowName}-{Timestamp}")
    .WithTimestamp("yyyyMMdd-HHmmss")
    .UseCompactFormat());
```

### Mermaid Provider

```csharp
meta.AddProvider<MermaidMetadataProvider, MermaidMetadataProviderBuilder>(mermaid => mermaid
    .WithOutputDirectory("metadata")
    .WithDirection(MermaidFlowchartDirection.LeftToRight)
    .WithActiveStepColor("#90EE90")
    .WithActiveDataColor("#ADD8E6"));
```
