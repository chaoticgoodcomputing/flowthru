---
name: flowthru-metadata-json
description: Deep skill for the Flowthru Metadata.Json extension — emits a Flow's DAG and run result as JSON. Use when a project needs a machine-readable record of its steps, Catalog Items, and run outcomes for a dashboard, diff tool, or downstream job. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.Metadata.Json
    surface: metadata
    capability: Serializes the planned DAG and run result to JSON — a pre-run manifest and a post-run result file per Flow.
    register: meta.AddJsonMetadata(…)
---

# flowthru-metadata-json

Registers a **metadata provider** that serializes Flowthru's introspection surface to JSON. It changes nothing about what the pipeline *does* — it extends what you can *see*. Before a run it writes a manifest of the planned DAG (steps, Catalog Items, dependencies); after a run it writes a result file of what happened. Point a dashboard, diff tool, or downstream job at the output directory.

## Register it

Reference the package, then register inside `ConfigureMetadata`, pointing at an output directory:

```bash
dotnet add package Flowthru.Extensions.Metadata.Json
```

<!-- flowthru:snippet:docs:register-metadata-json:start -->
```csharp
meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
```
<!-- flowthru:snippet:docs:register-metadata-json:end -->
<!-- flowthru:snippet:docs:register-metadata-json:start -->
```csharp
meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
```
<!-- flowthru:snippet:docs:register-metadata-json:end -->

_(real source: [Spaceflights `Program.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/Spaceflights/Program.cs))_

## When to use

- You want a machine-readable audit trail of every run — which steps existed, which Items fed them, how it turned out.
- You're feeding a dashboard, a diff tool (compare planned DAG across commits), or a downstream job.
- Reach for `flowthru-metadata-mermaid` instead (or alongside) when you want a human-viewable diagram rather than parseable JSON.

## Notes

- Two artifacts per run: a **pre-run** DAG manifest and a **post-run** result file.
- It's a sink on the existing introspection surface, not a pipeline step — the Flow is unchanged and needs no schema or catalog edits.
