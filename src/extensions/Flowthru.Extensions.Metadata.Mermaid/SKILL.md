---
name: flowthru-metadata-mermaid
description: Deep skill for the Flowthru Metadata.Mermaid extension — renders a Flow's DAG as a Mermaid flowchart in Markdown. Use when a project wants a human-viewable diagram of its topology and run outcome to drop in a README or wiki. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.Metadata.Mermaid
    surface: metadata
    capability: Draws the planned DAG and a colour-coded run-result diagram as Mermaid Markdown — renders anywhere Mermaid is supported.
    register: meta.AddMermaidMetadata(…)
---

# flowthru-metadata-mermaid

Registers a **metadata provider** that draws Flowthru's introspection surface as a Mermaid flowchart. It changes nothing about what the pipeline *does* — it extends what you can *see*. Before a run it writes a diagram of the planned topology; after a run it writes a diagram that styles each step by outcome (succeeded, failed, skipped). Output is plain Markdown, so it renders in a README, a wiki, or anywhere Mermaid is supported.

## Register it

Reference the package, then register inside `ConfigureMetadata`, pointing at an output directory:

```bash
dotnet add package Flowthru.Extensions.Metadata.Mermaid
```

<!-- flowthru:snippet:docs:register-metadata-mermaid:start -->
```csharp
meta.AddMermaidMetadata(opt => opt
  .WithOutputDirectory(metadataPath)
  .WithShowFullDag(false));
```
<!-- flowthru:snippet:docs:register-metadata-mermaid:end -->
<!-- flowthru:snippet:docs:register-metadata-mermaid:start -->
```csharp
meta.AddMermaidMetadata(opt => opt
  .WithOutputDirectory(metadataPath)
  .WithShowFullDag(false));
```
<!-- flowthru:snippet:docs:register-metadata-mermaid:end -->

_(real source: [Spaceflights `Program.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/Spaceflights/Program.cs))_

## When to use

- You want a human-viewable diagram of the DAG and run outcome, not parseable data.
- You're documenting a Flow in a README or wiki that renders Mermaid.
- Reach for `flowthru-metadata-json` instead (or alongside) when a machine needs to read the manifest.

## Notes

- Two Markdown artifacts per run: a **pre-run** topology diagram and a **post-run** outcome-coloured diagram.
- `.WithShowFullDag(false)` collapses the diagram to the operative steps; drop it (or set `true`) for the full DAG.
- It's a sink on the existing introspection surface — the Flow is unchanged and needs no schema or catalog edits.
