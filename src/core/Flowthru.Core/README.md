# Flowthru.Core

The Flowthru engine: Flow building, Step execution, Catalog adapter contracts, pre-flight
validation, smart caching, and hosting. Everything Flowthru does at runtime lives here, and
every format or stack extension (`Flowthru.Extensions.*`) builds on the contracts this
package exposes. Install it directly when you want the engine and intend to choose your own
Catalog adapters; install the `Flowthru` umbrella for a batteries-included setup.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_core)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Install

```bash
dotnet add package Flowthru.Core
```

Register the engine, a Catalog, and your Flows on any `IServiceCollection`:

```csharp
services.AddFlowthru(flowthru =>
{
    flowthru.RegisterCatalog(sp => new Catalog(basePath));
    flowthru
        .RegisterFlow<Catalog>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Preprocesses raw inputs into model-ready tables");
});
```

A Catalog declares typed Items; a Flow wires Steps between them. The engine validates the
merged DAG before any Step runs, so a missing producer, a duplicate writer, or a broken type
contract fails at pre-flight rather than mid-run.
