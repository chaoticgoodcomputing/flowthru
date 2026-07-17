---
name: flowthru
description: Use when authoring or modifying Flowthru data pipelines — creating flows, schemas, catalog items, or steps in a Flowthru (.NET) project. Covers the layered Data/ structure (_01_Raw → _08_Reporting), the schema → catalog → step → flow model, the end-to-end input-to-output workflow, and how to pull stack-specific extension skills (Csv, Parquet, EFCore, Python, DuckDB, Http, …). Routes to flow-developers.md, catalog-developers.md, and extensions.md.
---

# Flowthru Flow Agent Guide

You are working inside a Flowthru data pipeline project. Flowthru is a type-safe, fail-fast data-engineering framework for .NET. You have baseline knowledge of data pipelines, DAGs, and ETL — this guide tells you how to express those concepts in Flowthru's structure.

**Fail-fast is the whole philosophy.** Every failure Flowthru can catch, it catches as early as possible: *design-time* (compile errors, analyzer squigglies, fast unit tests) is the gold standard, *pre-flight* (after launch, before any step runs) is tolerable, and *runtime* is the enemy. When you write a Flow, lean into the type system — passing the wrong-typed catalog item to a step is a compile error, not a 2-a.m. page.

**Ground new code in the code already here.** Exact syntax for schemas, catalog items, steps, and flows is best gleaned from this project's existing files — match their shape, naming, and idiom. The canonical reference is [`examples/starter/IrisFUnit/`](https://github.com/chaoticgoodcomputing/flowthru/tree/main/examples/starter/IrisFUnit) in the Flowthru repository; model new code on it when this project has no local precedent.

## The model in four nouns

| Noun | What it is | Deep guide |
|------|-----------|------------|
| **Schema** | The typed shape of a record — a `[FlowthruSchema] partial record`. | [catalog-developers.md](catalog-developers.md) |
| **Catalog item** | A typed handle to stored data: `IItem<T>`, defined by *format × medium × container*. | [catalog-developers.md](catalog-developers.md) |
| **Step** | A pure transform: `[FlowthruStep]` with `Create(deps) => input => output`. | [flow-developers.md](flow-developers.md) |
| **Flow** | Steps wired between catalog items into a DAG (`BuiltFlow`). | [flow-developers.md](flow-developers.md) |

## Project structure

```
Program.cs                          # Entrypoint — flows are registered here; runs as a
                                    # Flowthru CLI service via `dotnet run`
Data/                               # All data definitions
├── _01_Raw/                        # Immutable source data — never mutated, only copied from
│   ├── Catalog.Raw.cs              # Catalog items for this layer (partial class Catalog)
│   └── Schemas/*.cs                # Schemas referenced by this layer's catalog items
├── _02_Intermediate/               # Typed mirror of raw — cleaned, parsed, typed
├── _03_Primary/                    # Domain data — restructured for the problem
├── _04_Feature/                    # Engineered features derived from primary data
├── _05_ModelInput/                 # Joined feature sets ready for model consumption
├── _06_Models/                     # Serialized trained models
├── _07_ModelOutput/                # Results and predictions from model runs
├── _08_Reporting/                  # Descriptive/summary outputs for business consumption
└── Catalog.cs                      # `partial class Catalog : CatalogAbstract` root + ctor

Flows/
└── <FlowName>/
    ├── <FlowName>Flow.cs           # Steps wired to input/output catalog items
    └── Steps/*.cs                  # Individual transformation steps
```

Not every layer is used by every pipeline — empty layers are left as placeholders. Put each schema in the layer whose data it describes.

## Workflow

Tasks are ordinarily: *"I have X input data, and I want Y output data."* Work outward from the data:

1. **Plan the transformation steps** — the discrete transforms from input to output.
2. **Plan the intermediate schemas** — the data shapes between each step.
3. **Write the schemas** (`Data/<Layer>/Schemas/`) — see [catalog-developers.md](catalog-developers.md).
4. **Create the catalog items** (`Data/<Layer>/Catalog.<Layer>.cs`) — see [catalog-developers.md](catalog-developers.md).
5. **Write the steps** (`Flows/<FlowName>/Steps/`) — see [flow-developers.md](flow-developers.md).
6. **Wire the flow** (`Flows/<FlowName>/<FlowName>Flow.cs`) — see [flow-developers.md](flow-developers.md).
7. **Register the flow** in `Program.cs` (`AddFlowthru` + `RegisterCatalog` + `RegisterFlow` + any `UseXxx()`).
8. **Run and confirm** with `dotnet run` (no args) — the CLI runner discovers and executes registered flows.

## Extensions — the stack-specific menu

Core covers the model above. Everything stack-specific — file formats (Csv, Excel, Parquet, Xml), databases (EFCore), remote media (Http, S3), execution engines (Python, DuckDB), diagnostics — lives in **extensions**, and *each extension has its own deep skill*.

**[extensions.md](extensions.md) is the full menu.** Use it as a capability index: when a task needs a capability, check whether an extension already covers it. To detect what *this* project already uses, read its `.csproj` for `Flowthru.Extensions.*` `PackageReference`s and the `b.UseXxx()` calls in `Program.cs`. Each row names a deep skill (`--skill flowthru-<ext>`) — pull it for any extension the project uses or that you're about to introduce:

```bash
npx skills add chaoticgoodcomputing/flowthru --skill flowthru-<ext>   # e.g. flowthru-efcore-npgsql
```

This installs the deep skill into the project (`.claude/skills/flowthru-<ext>/` under Claude Code). **A skill added mid-session is not auto-loaded — read the installed `SKILL.md` now to use it**; it is then committed with the project and auto-loads in every later session. To consult a skill *without* installing, `npx skills use chaoticgoodcomputing/flowthru --skill flowthru-<ext>` prints it to stdout. The umbrella tells you *what exists*; the extension skill tells you *how to use it*.

## When to stop

**If a build error or runtime failure surfaces something these guides don't cover, stop and ask.** The API surface is intentionally narrow; if the natural code shape doesn't fit it, that's a signal to discuss before working around it — not to invent a workaround.
