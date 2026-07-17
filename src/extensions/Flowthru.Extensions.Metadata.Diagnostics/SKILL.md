---
name: flowthru-metadata-diagnostics
description: Deep skill for the Flowthru Metadata.Diagnostics extension — logs what a Flow run actually did (step timings, run summary, opt-in row counts and output-existence audit). Use when a project wants post-run observability without wiring up JSON or diagrams. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.Metadata.Diagnostics
    surface: metadata
    capability: Curated post-run diagnostic providers — step timings and a run summary by default, opt-in row counts and output-existence audit.
    register: meta.UseDiagnostics()
---

# flowthru-metadata-diagnostics

Registers a **curated set of post-run diagnostic providers** that read the run result and log it. It changes nothing about what the pipeline *does* — it extends what you can *see*. `UseDiagnostics()` wires the default set (StepTimings + RunSummary) in one line; the storage-touching providers (row counts, output existence) stay opt-in because the engine does not subsidise expensive observation.

## Register it

Reference the package, then register inside `ConfigureMetadata`:

```bash
dotnet add package Flowthru.Extensions.Metadata.Diagnostics
```

<!-- flowthru:snippet:docs:register-diagnostics:start -->
```csharp
meta.UseDiagnostics();
```
_(source: [`SimpleEffectsExample/Program.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/SimpleEffectsExample/Program.cs))_
<!-- flowthru:snippet:docs:register-diagnostics:end -->

Opt into the heavier providers via the configure lambda:

```csharp
meta.UseDiagnostics(opt =>
{
    opt.RowCounts.Enabled = true;
    opt.OutputExistence.Enabled = true;
});
```

## When to use

- You want the slowest steps and a per-run summary logged, cheaply, on every run.
- You need to confirm each Item emitted rows or that every output landed — enable `RowCounts` / `OutputExistence`.
- Reach for `flowthru-metadata-json` or `flowthru-metadata-mermaid` when you need a persisted artifact (JSON record, Mermaid diagram) rather than logged diagnostics.

## Notes

- **Default (free):** StepTimings + RunSummary — pure post-processing of the result the scheduler already produced.
- **Opt-in (touches live storage):** RowCounts, OutputExistence.
- It's a sink on the existing run result — the Flow is unchanged and needs no schema or catalog edits.
