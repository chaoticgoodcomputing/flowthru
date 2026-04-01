# Deploying a Pipeline in a Container

Deploy a Flowthru pipeline as a standalone container image for execution in environments like AWS Lambda, Azure Container Apps, or Kubernetes Jobs.

## Prerequisites

- A working Flowthru pipeline project (see the [starter tutorial](../../tutorials/) if you need one)
- Docker or a compatible container runtime
- Familiarity with [pipeline slicing](../slicing-pipelines.md)

## The Entry Point

A containerized pipeline replaces `FlowthruCli` with a minimal `Program.cs` that owns its own DI container. The key difference: no CLI argument parsing, no filesystem-based configuration.

```csharp
using Flowthru.Pipelines;
using Flowthru.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

services.AddLogging(logging =>
    logging.AddJsonConsole()); // Structured logging for container runtimes

services.AddFlowthru(flowthru =>
{
    flowthru
        .RegisterCatalog<MyCatalog>()
        .RegisterPipeline<MyCatalog>("Ingest", catalog => IngestPipeline.Create(catalog))
        .RegisterPipeline<MyCatalog>("Transform", catalog => TransformPipeline.Create(catalog));

    // UseStorageStrategy swaps all catalog entries to a non-filesystem medium
    flowthru.UseStorageStrategy<CloudStorageStrategy>();
});

await using var provider = services.BuildServiceProvider();
var flowthru = provider.GetRequiredService<IFlowthruService>();

// Build execution options from environment variables
var options = new ExecutionOptions
{
    SliceStrategy = BuildSliceStrategy()
};

using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(14)); // Lambda-safe margin
var result = await flowthru.ExecutePipelineAsync(options, cancellationToken: cts.Token);

return result.Success ? 0 : 1;

// --- helpers ---

static PipelineSliceStrategy BuildSliceStrategy()
{
    static HashSet<string>? ParseCsv(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new(value.Split(',', StringSplitOptions.RemoveEmptyEntries));

    return new PipelineSliceStrategy
    {
        Pipelines  = ParseCsv(Environment.GetEnvironmentVariable("FLOWTHRU_PIPELINES")),
        FromNodes  = ParseCsv(Environment.GetEnvironmentVariable("FLOWTHRU_FROM_NODES")),
        ToNodes    = ParseCsv(Environment.GetEnvironmentVariable("FLOWTHRU_TO_NODES")),
        FromData   = ParseCsv(Environment.GetEnvironmentVariable("FLOWTHRU_FROM_DATA")),
        ToData     = ParseCsv(Environment.GetEnvironmentVariable("FLOWTHRU_TO_DATA")),
        OnlyNodes  = ParseCsv(Environment.GetEnvironmentVariable("FLOWTHRU_ONLY_NODES")),
    };
}
```

This pattern keeps the container image generic — slice parameters arrive at runtime via environment variables, making the same image reusable across different pipeline runs.

## Configuration Without the Filesystem

`UseConfiguration()` probes for JSON/YAML files relative to the working directory. In a container, configuration typically comes from environment variables, mounted secrets, or cloud-native config services. Skip `UseConfiguration()` entirely and register your own `IConfiguration`:

```csharp
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables("FLOWTHRU_")
    .AddJsonStream(GetRemoteConfigStream()) // SSM, Secrets Manager, etc.
    .Build();

services.AddSingleton<IConfiguration>(configuration);

services.AddFlowthru(flowthru =>
{
    // No UseConfiguration() call — the IConfiguration above is already registered
    flowthru
        .RegisterCatalog<MyCatalog>()
        .RegisterPipeline<MyCatalog>("Ingest", catalog => IngestPipeline.Create(catalog));
});
```

If your pipelines use `RegisterPipelineWithConfiguration<TCatalog, TParams>()`, that method requires `UseConfiguration()` to have been called first. In that case, bind the parameters yourself and use `RegisterPipeline` with an explicit parameter object instead:

```csharp
var myParams = configuration.GetSection("MyPipeline").Get<MyParams>()
    ?? throw new InvalidOperationException("Missing MyPipeline config section");

flowthru.RegisterPipeline<MyCatalog, MyParams>(
    "MyPipeline",
    (catalog, parameters) => MyPipeline.Create(catalog, parameters),
    myParams);
```

## Cancellation

Flowthru threads `CancellationToken` through the pipeline execution graph into every node's `FlowIO` effects and storage medium operations. In a container, wire the token to whatever timeout mechanism your runtime provides:

| Runtime              | Token Source                                                    |
| -------------------- | --------------------------------------------------------------- |
| AWS Lambda           | `context.RemainingTime` → `CancellationTokenSource(TimeSpan)`   |
| Kubernetes Job       | `SIGTERM` → `Console.CancelKeyPress` or `AppDomain.ProcessExit` |
| Azure Container Apps | `IHostApplicationLifetime.ApplicationStopping`                  |

For a raw console entry point (no `IHost`), handle `SIGTERM`:

```csharp
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
AppDomain.CurrentDomain.ProcessExit += (_, _) => cts.Cancel();

var result = await flowthru.ExecutePipelineAsync(options, cancellationToken: cts.Token);
```

## Dockerfile

A standard .NET publish Dockerfile works. The only Flowthru-specific consideration is that no config files need to be copied if you've externalized configuration as described above.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish MyPipeline.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "MyPipeline.dll"]
```

Then invoke with slice parameters:

```bash
docker run \
  -e FLOWTHRU_PIPELINES=Ingest \
  -e FLOWTHRU_TO_DATA=cleaned_orders \
  -e CONNECTION_STRING="Host=..." \
  my-pipeline:latest
```

## Inspecting Results

`ExecutePipelineAsync` returns a `PipelineResult` — a structured value object with `Success`, `ExecutionTime`, per-node `NodeResults` (including individual timing, I/O counts, and exceptions), and an optional top-level `Exception`. Serialize it to structured output for your runtime's observability:

```csharp
var result = await flowthru.ExecutePipelineAsync(options, cancellationToken: cts.Token);

if (!result.Success)
{
    logger.LogError("Pipeline failed after {Duration}s: {Error}",
        result.ExecutionTime.TotalSeconds,
        result.Exception?.Message);

    foreach (var (name, node) in result.NodeResults.Where(n => !n.Value.Success))
    {
        logger.LogError("  Node {Node} failed: {Error}", name, node.Exception?.Message);
    }
}
```

## See Also

- [Pipeline slicing](../slicing-pipelines.md) — all slice strategies and how they compose
- [Service integration](service-integration.md) — embedding Flowthru in an existing .NET host instead of a standalone container
