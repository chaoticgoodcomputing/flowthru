# Flowthru.Cli

The standalone entry point for running Flowthru Flows from a `Main`. `RunStandaloneAsync` hosts
the DI container, parses command-line flags, dispatches to the requested Flow, renders each
Step's outcome to the console, and returns a process exit code — `0` on success, `1` on a Flow
failure, `2` on a usage error. Flags cover the common runner needs: list registered Flows, run
a single Flow by label, or slice the merged DAG with `--from` / `--to` / `--only`.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_cli)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Install

```bash
dotnet add package Flowthru.Cli
```

Hand `RunStandaloneAsync` a callback that registers the engine, a Catalog, and your Flows:

```csharp
public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(args, services =>
    {
        services.AddFlowthru(flowthru =>
        {
            flowthru.RegisterCatalog(sp => new Catalog(basePath));
            flowthru
                .RegisterFlow<Catalog>("DataProcessing", DataProcessingFlow.Create)
                .WithDescription("Preprocesses raw inputs into model-ready tables");
        });
    });
```

The callback runs against a fresh `IServiceCollection`, so the same `AddFlowthru(...)` wiring you
would use under any host works here unchanged.
