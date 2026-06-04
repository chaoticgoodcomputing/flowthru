# Flowthru.Extensions.Metadata.Json

Emit your Flow's structure and run results as JSON. Registers a metadata provider that
writes a DAG manifest before a run and a run-result file after, so every Flow leaves
behind a machine-readable record of its steps, Catalog Items, and what happened — one
line in `ConfigureMetadata`.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_metadata_json)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

This package adds nothing to what your pipeline *does* — it extends what you can *see*.
Flowthru already knows its own DAG: which steps exist, which Catalog Items feed them,
and how a run turned out. This provider serializes that introspection surface to JSON —
a pre-run manifest of the planned DAG and a post-run file of the result. Point a
dashboard, a diff tool, or a downstream job at the output directory; the Flow itself is
unchanged.

## Install

```bash
dotnet add package Flowthru.Extensions.Metadata.Json
```

Register the provider inside `ConfigureMetadata`, pointing it at an output directory:

```csharp
services.AddFlowthru(flowthru =>
{
    flowthru.ConfigureMetadata(meta =>
    {
        meta.AddJsonMetadata(opt => opt
            .WithOutputDirectory("Metadata"));
    });
});
```
