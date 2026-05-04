# Spaceflights Staging Schema

This starter demonstrates the **catalog-attached resource lifecycle** pattern: an ephemeral staging database is provisioned in pre-flight, populated by `DataProcessing`, promoted to production by a dedicated `Promotion` flow, then dropped. `DataScience` and `Reporting` only ever read from production.

## What this demonstrates

- A catalog declaring `FlowResource<DbScope>` to own an ephemeral SQLite database.
- A catalog declaring `FlowValidation` (applicative, error-accumulating) to fail fast on connection or filesystem permission issues.
- A `Promotion` flow as the single boundary between intermediate work (staging) and durable artifacts (production).
- LIFO unwind: staging is always dropped at the end of an execution, even on failure (with `PreserveOnFailure` opt-in for debugging).

## Topology

```
Raw CSV/Excel  →  DataProcessing  →  staging.db  →  Promotion  →  production.db  →  DataScience  →  Reporting
                                     (ephemeral)                  (persistent)
```

`staging.db` exists only during execution. `production.db` persists across runs and contains the model input table, train/test splits, the trained model, metrics, predictions, and reports.

## Project structure

```
SpaceflightsStagingSchema/
├── Program.cs                          # Service registration with new API surface
├── appsettings.json                    # Pipeline configuration
├── Data/
│   ├── StagingDbContext.cs             # Intermediate + primary tables (ephemeral)
│   ├── ProductionDbContext.cs          # Promoted + science + reporting tables
│   ├── RawCatalog.cs                   # CSV/Excel inputs (no resource)
│   ├── StagingCatalog.cs               # Owns FlowResource<DbScope>
│   ├── ProductionCatalog.cs            # Persistent, no resource
│   ├── FlowConfig.cs
│   └── _01_Raw/ … _08_Reporting/       # Layered schemas + per-layer catalog partials
└── Flows/
    ├── DataProcessing/                 # Raw → Staging
    ├── Promotion/                      # Staging → Production (NEW)
    ├── DataScience/                    # Production → Production
    └── Reporting/                      # Production → Production
```

## Running

```bash
nx run SpaceflightsStagingSchema --target=run -- DataProcessing Promotion DataScience Reporting
```

Or run all registered flows:

```bash
nx run SpaceflightsStagingSchema --target=run
```

## Status

This starter exercises an API surface that is **not yet implemented in core**. It is part of an in-progress design initiative ([docs/scratch/catalog-resource-lifecycles.md](../../../docs/scratch/catalog-resource-lifecycles.md)). Until the supporting types ship in `Flowthru.Core` and `Flowthru.Extensions.EFCore`, this project will not build — that's intentional, and the compile errors validate the proposed API surface.
