# Flowthru.Extensions.Metadata.Diagnostics

Report what a Flow run actually did. Registers a curated set of post-run diagnostic
providers — step timings, a run summary, and (opt-in) row counts and an output-existence
audit — that post-process the run result and log it. `UseDiagnostics()` wires the
default set in one line inside `ConfigureMetadata`.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_metadata_diagnostics)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

This package adds nothing to what your pipeline *does* — it extends what you can *see*.
Flowthru already records each run's result: which steps ran, how long they took, what
they produced. These providers read that result and surface it as diagnostics — the
slowest steps, a per-run summary, and on request the row counts each Item emitted or
whether every output landed. The cheap views (step timings, run summary) are pure
post-processing of the result the scheduler already produced, so they run by default; the
ones that touch live storage (row counts, output existence) stay opt-in, because the
engine does not subsidise expensive observation. The Flow itself is unchanged.

## Install

```bash
dotnet add package Flowthru.Extensions.Metadata.Diagnostics
```

Register the default provider set inside `ConfigureMetadata`:

```csharp
services.AddFlowthru(flowthru =>
{
    flowthru.ConfigureMetadata(meta =>
    {
        // StepTimings + RunSummary by default (free post-run computations).
        // Opt into the storage-touching providers via the configure lambda.
        meta.UseDiagnostics(opt =>
        {
            opt.RowCounts.Enabled = true;
            opt.OutputExistence.Enabled = true;
        });
    });
});
```
