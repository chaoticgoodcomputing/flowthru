# Flowthru

A type-safe, fail-fast data engineering framework for .NET. This is the batteries-included
package: it bundles the engine (`Flowthru.Core`) with the most common format extensions
(CSV, Parquet, Excel) and metadata exporters (JSON, Mermaid), so a new project can build a
Flow end-to-end with a single `dotnet add package`.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Install

```bash
dotnet add package Flowthru
```

Wire the engine into a host, register a Catalog and one or more Flows:

```csharp
services.AddFlowthru(flowthru =>
{
    flowthru.RegisterCatalog(sp => new Catalog(basePath));
    flowthru
        .RegisterFlow<Catalog>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Preprocesses raw inputs into model-ready tables");
});
```

## What's bundled

| Package | Role |
|---------|------|
| `Flowthru.Core` | The engine — Flow building, Step execution, Catalog contracts, validation, caching, hosting |
| `Flowthru.Extensions.Csv` / `.Parquet` / `.Excel` | File-format Catalog adapters |
| `Flowthru.Extensions.Metadata.Json` / `.Mermaid` | DAG metadata exporters |

Reach for a specific extension package directly (`Flowthru.Extensions.EFCore`,
`.Python`, `.Http`, …) when you need a stack this bundle doesn't include, or depend on
`Flowthru.Core` alone when you want to pick every adapter yourself.
