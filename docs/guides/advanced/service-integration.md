# Using Flowthru as a Service Dependency

Register `IFlowthruService` into an existing .NET application and invoke pipelines programmatically — from API endpoints, background jobs, or message handlers.

## Prerequisites

- An existing .NET application with dependency injection (ASP.NET Core, worker service, etc.)
- A Flowthru pipeline project referenced as a project dependency or NuGet package
- Familiarity with [pipeline slicing](../slicing-pipelines.md)

## Registration

`AddFlowthru` is an `IServiceCollection` extension. It registers `IFlowthruService` as a singleton. Call it alongside your other service registrations:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFlowthru(flowthru =>
{
    flowthru
        .RegisterCatalog<WarehouseCatalog>()
        .RegisterPipeline<WarehouseCatalog>("Ingest", c => IngestPipeline.Create(c))
        .RegisterPipeline<WarehouseCatalog>("Transform", c => TransformPipeline.Create(c))
        .UseStorageStrategy<DatabaseStorageStrategy>();
});
```

`IFlowthruService` is then available anywhere DI resolves — controllers, minimal API handlers, `IHostedService` implementations, MediatR handlers, etc.

### Reusing the Host's Configuration

If your pipelines need configuration values, you have two options.

**Option A: Register the host's `IConfiguration` before `AddFlowthru` and bind parameters manually.** This is the recommended approach when the host already manages configuration (environment variables, Azure App Configuration, AWS SSM, etc.):

```csharp
var myParams = builder.Configuration
    .GetSection("Pipelines:Transform")
    .Get<TransformParams>()
    ?? throw new InvalidOperationException("Missing Transform pipeline config");

builder.Services.AddFlowthru(flowthru =>
{
    flowthru
        .RegisterCatalog<WarehouseCatalog>()
        .RegisterPipeline<WarehouseCatalog, TransformParams>(
            "Transform",
            (catalog, p) => TransformPipeline.Create(catalog, p),
            myParams);
});
```

**Option B: Use `UseConfiguration()` with a custom base path.** This works when config files are available on disk (e.g., deployed alongside the application):

```csharp
builder.Services.AddFlowthru(flowthru =>
{
    flowthru
        .UseConfiguration(opts => opts.BasePath = "/app/pipeline-config")
        .RegisterCatalog<WarehouseCatalog>()
        .RegisterPipelineWithConfiguration<WarehouseCatalog, TransformParams>(
            "Transform",
            (catalog, p) => TransformPipeline.Create(catalog, p),
            configurationSection: "Pipelines:Transform");
});
```

Option A avoids filesystem assumptions and keeps configuration concerns unified under the host.

## Invoking Pipelines

Inject `IFlowthruService` and call `ExecutePipelineAsync`. Pass an `ExecutionOptions` object to control slicing, dry runs, and error behavior:

```csharp
app.MapPost("/pipelines/run", async (
    PipelineRunRequest request,
    IFlowthruService flowthru,
    CancellationToken ct) =>
{
    var options = new ExecutionOptions
    {
        SliceStrategy = new FlowSliceStrategy
        {
            Pipelines = request.Pipelines is { Count: > 0 }
                ? request.Pipelines.ToHashSet()
                : null,
            ToData = request.ToData is { Count: > 0 }
                ? request.ToData.ToHashSet()
                : null,
        }
    };

    var result = await flowthru.ExecutePipelineAsync(
        options,
        exportMetadata: false,
        cancellationToken: ct);

    return result.Success
        ? Results.Ok(MapToResponse(result))
        : Results.Problem(
            detail: result.Exception?.Message ?? "Pipeline execution failed",
            statusCode: 500);
});
```

The `CancellationToken` from ASP.NET Core's request pipeline flows through to every node in the DAG. If the client disconnects or the request times out, in-flight nodes observe cancellation through their `FlowIO` effects.

### Request Model

A simple DTO for accepting slice parameters over HTTP:

```csharp
public record PipelineRunRequest
{
    public List<string>? Pipelines { get; init; }
    public List<string>? ToData { get; init; }
    public List<string>? FromData { get; init; }
    public List<string>? OnlyNodes { get; init; }
}
```

`FlowSliceStrategy` properties are `IReadOnlySet<string>?` — convert from lists at the boundary.

## Handling Results

`FlowResult` carries structured execution data. Map it to your application's response model rather than returning it directly:

```csharp
static PipelineRunResponse MapToResponse(FlowResult result)
{
    return new PipelineRunResponse
    {
        Success = result.Success,
        DurationMs = result.ExecutionTime.TotalMilliseconds,
        NodesExecuted = result.StepResults.Count,
        NodeSummaries = result.StepResults.Select(kvp => new NodeSummary
        {
            Name = kvp.Key,
            Success = kvp.Value.Success,
            DurationMs = kvp.Value.ExecutionTime.TotalMilliseconds,
            InputCount = kvp.Value.InputCount,
            OutputCount = kvp.Value.OutputCount,
            Error = kvp.Value.Exception?.Message,
        }).ToList(),
    };
}
```

For background jobs where there's no HTTP response, log the result or publish it to a message queue:

```csharp
public class PipelineBackgroundService : BackgroundService
{
    private readonly IFlowthruService _flowthru;
    private readonly ILogger<PipelineBackgroundService> _logger;

    public PipelineBackgroundService(IFlowthruService flowthru, ILogger<PipelineBackgroundService> logger)
    {
        _flowthru = flowthru;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var result = await _flowthru.ExecutePipelineAsync(
            cancellationToken: stoppingToken);

        if (result.Success)
        {
            _logger.LogInformation(
                "Pipeline completed: {NodeCount} nodes in {Duration}s",
                result.StepResults.Count,
                result.ExecutionTime.TotalSeconds);
        }
        else
        {
            _logger.LogError(result.Exception,
                "Pipeline failed after {Duration}s",
                result.ExecutionTime.TotalSeconds);
        }
    }
}
```

## Pre-Flight Validation

Use `ValidatePipelineAsync` to check external inputs (Layer 0 data sources) without running the pipeline. This is useful for health checks or readiness probes:

```csharp
app.MapGet("/health/pipeline/{name}", async (
    string name,
    IFlowthruService flowthru,
    CancellationToken ct) =>
{
    var validation = await flowthru.ValidatePipelineAsync(name, ct);
    return validation.IsValid
        ? Results.Ok()
        : Results.Problem(detail: string.Join("; ", validation.Errors));
});
```

## Lifetime Considerations

`IFlowthruService` is registered as a **singleton**. All registered pipelines are built eagerly in the service constructor. This means:

- **Cold start cost** is paid once — at application startup, not per request.
- **Catalog entries** are singletons scoped to the service instance. They are not request-scoped. If your storage adapters hold mutable state (e.g., EF Core contexts), ensure they manage their own scoping.
- **Multiple isolated pipeline sets** in one process would require multiple `IFlowthruService` instances, which the current registration model doesn't support. If you need this, build separate `ServiceProvider` instances.

For most applications — one set of pipelines, invoked by various triggers — the singleton model is appropriate.

## See Also

- [Pipeline slicing](../slicing-pipelines.md) — all slice strategies and how they compose
- [Container deployment](container-deployment.md) — deploying a pipeline as a standalone container instead of embedding it
