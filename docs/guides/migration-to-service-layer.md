# Migration Guide: FlowthruApplication → FlowthruCliBuilder

**Status:** Current as of Flowthru 0.2.0  
**Last Updated:** 2026-02-20

---

## Overview

Flowthru has transitioned to a service-based architecture that separates CLI concerns from business logic. The old `FlowthruApplication` API is now **deprecated** in favor of `FlowthruCliBuilder` + `IFlowthruService`.

### Why Migrate?

The new architecture provides:
- **Dependency Injection** - Use Flowthru in web apps, Azure Functions, background services
- **Testability** - Mock services and pipelines in unit tests
- **Separation of Concerns** - CLI layer is thin, business logic is injectable
- **Consistency** - One recommended way to build Flowthru applications

---

## Quick Migration

### Before (Deprecated)

```csharp
using Flowthru.Application;

public static async Task<int> Main(string[] args)
{
    var app = FlowthruApplication.Create(args, builder =>
    {
        builder.UseConfiguration();
        builder.UseCatalog(new MyCatalog("Data"));
        builder
            .RegisterPipeline<MyCatalog>("my_pipeline", MyPipeline.Create)
            .WithDescription("Pipeline description");
    });

    return await app.RunAsync();
}
```

### After (Recommended)

```csharp
using Flowthru.Cli;
using Microsoft.Extensions.Logging;

public static async Task<int> Main(string[] args)
{
    var cli = FlowthruCliBuilder
        .Create(flowthru =>
        {
            flowthru.UseConfiguration();
            flowthru.UseCatalog(_ => new MyCatalog("Data"));
            flowthru
                .RegisterPipeline<MyCatalog>("my_pipeline", MyPipeline.Create)
                .WithDescription("Pipeline description");
        })
        .ConfigureLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        })
        .Build();

    return await cli.RunAsync(args);
}
```

---

## API Mapping

| Old API (FlowthruApplicationBuilder)                | New API (FlowthruServiceBuilder)                     |
| --------------------------------------------------- | ---------------------------------------------------- |
| `builder.UseConfiguration()`                        | `flowthru.UseConfiguration()`                        |
| `builder.UseCatalog(new Catalog())`                 | `flowthru.UseCatalog(_ => new Catalog())`            |
| `builder.UseCatalog<Catalog>()`                     | `flowthru.UseCatalog<Catalog>()`                     |
| `builder.RegisterPipeline<T>()`                     | `flowthru.RegisterPipeline<T>()`                     |
| `builder.RegisterPipelineWithConfiguration<T, P>()` | `flowthru.RegisterPipelineWithConfiguration<T, P>()` |
| `builder.WithDescription()`                         | `flowthru.WithDescription()`                         |
| `builder.WithTags()`                                | `flowthru.WithTags()`                                |
| `builder.ConfigureLogging()`                        | `.ConfigureLogging()` (on builder)                   |

---

## Key Differences

### 1. Catalog Registration

**Old:** Direct instance or type registration
```csharp
builder.UseCatalog(new MyCatalog("Data"));
```

**New:** Use factory for instances (ensures proper DI lifecycle)
```csharp
flowthru.UseCatalog(_ => new MyCatalog("Data"));
// OR use type registration for DI-injected catalogs
flowthru.UseCatalog<MyCatalog>();
```

### 2. Logging Configuration

**Old:** Logging configured inside builder action
```csharp
FlowthruApplication.Create(args, builder => {
    builder.ConfigureLogging(logging => logging.AddConsole());
});
```

**New:** Logging configured via fluent chain
```csharp
FlowthruCliBuilder
    .Create(flowthru => { /* ... */ })
    .ConfigureLogging(logging => logging.AddConsole())
    .Build();
```

### 3. Build Step Required

**Old:** Application created and returned directly
```csharp
var app = FlowthruApplication.Create(args, builder => { ... });
```

**New:** Explicit Build() step
```csharp
var cli = FlowthruCliBuilder.Create(flowthru => { ... }).Build();
```

---

## Using IFlowthruService in Non-CLI Scenarios

The service layer enables usage outside CLI applications:

### ASP.NET Core

```csharp
// Startup.cs or Program.cs
services.AddFlowthru(flowthru =>
{
    flowthru.UseCatalog<MyCatalog>();
    flowthru.RegisterPipeline<MyCatalog>("pipeline", Pipeline.Create);
});

// In a controller
public class PipelineController : ControllerBase
{
    private readonly IFlowthruService _flowthru;

    public PipelineController(IFlowthruService flowthru)
    {
        _flowthru = flowthru;
    }

    [HttpPost("run/{name}")]
    public async Task<IActionResult> RunPipeline(string name)
    {
        var request = new PipelineExecutionRequest { PipelineName = name };
        var result = await _flowthru.ExecutePipelineAsync(request);
        return Ok(result);
    }
}
```

### Azure Functions

```csharp
// Startup.cs
public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddFlowthru(flowthru =>
        {
            flowthru.UseCatalog<MyCatalog>();
            flowthru.RegisterPipeline<MyCatalog>("pipeline", Pipeline.Create);
        });
    }
}

// Function
public class PipelineFunction
{
    private readonly IFlowthruService _flowthru;

    public PipelineFunction(IFlowthruService flowthru)
    {
        _flowthru = flowthru;
    }

    [FunctionName("RunPipeline")]
    public async Task Run(
        [TimerTrigger("0 0 * * *")] TimerInfo timer,
        ILogger log)
    {
        var request = new PipelineExecutionRequest { PipelineName = "daily_etl" };
        await _flowthru.ExecutePipelineAsync(request);
    }
}
```

---

## Deprecation Timeline

- **0.2.0** - New service layer introduced, old API marked obsolete (warning only)
- **0.3.0** (planned) - Deprecation warnings upgraded to compile errors for new projects
- **1.0.0** (planned) - Old API removed entirely

---

## Getting Help

- See `examples/KedroSpaceflights.Pure/Program.cs` for complete migration example
- Review `docs/scratch/service-based-architecture.md` for architectural details
- File issues at https://github.com/chaoticgoodcomputing/flowthru/issues
