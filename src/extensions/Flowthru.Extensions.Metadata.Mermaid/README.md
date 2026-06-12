# Flowthru.Extensions.Metadata.Mermaid

Render your Flow's DAG as a Mermaid flowchart. Registers a metadata provider that writes
a diagram of the planned DAG before a run and a colour-coded result diagram after, as
Markdown files — drop them in a README or wiki and the steps, Catalog Items, and run
outcome render as a flowchart, one line in `ConfigureMetadata`.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_metadata_mermaid)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

This package adds nothing to what your pipeline *does* — it extends what you can *see*.
Flowthru already knows its own DAG: which steps exist, which Catalog Items feed them,
and how a run turned out. This provider draws that introspection surface as a Mermaid
flowchart — a pre-run diagram of the planned topology and a post-run diagram that styles
each step by outcome (succeeded, failed, skipped). The output is plain Markdown, so it
renders anywhere Mermaid is supported. The Flow itself is unchanged.

## Install

```bash
dotnet add package Flowthru.Extensions.Metadata.Mermaid
```

Register the provider inside `ConfigureMetadata`, pointing it at an output directory:

```csharp
services.AddFlowthru(flowthru =>
{
    flowthru.ConfigureMetadata(meta =>
    {
        meta.AddMermaidMetadata(opt => opt
            .WithOutputDirectory("Metadata")
            .WithShowFullDag(false));
    });
});
```
