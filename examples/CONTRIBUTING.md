# Contributing to Flowthru Examples

This document is for developers writing **on top of Flowthru** — Flow Developers and Catalog Developers building ETL workflows, both in this repo's `examples/` projects and (the same audience) in their own downstream projects.

**Audience scope:** what a downstream user would know. Nothing here should require knowledge of Flowthru's internal implementation. If a term used here isn't defined in this document's glossary, treat that as a signal that the public API surface is leaking internal context — file an issue.

See [/CONTRIBUTING.md](/CONTRIBUTING.md) for cross-cutting design rules (the three error phases, decision rules for where validation belongs) that apply to all contributors. See [/docs/CONTRIBUTING.md](/docs/CONTRIBUTING.md) for documentation tone and the Diátaxis framework — both inform the example-structure rules below.

## What Examples Are For

Examples are the primary on-ramp for new Flowthru users. They serve as documentation-by-example and as templates that downstream users clone, rename, and modify.

The target reader is someone evaluating Flowthru: "I'm interested, but my stack is Python / databases / <X> — how would Flowthru cover me?" Their entry point is `examples/starter/`. Once they're convinced, they reach for `examples/advanced/` to answer "how do I do `<specific pattern>`?"

## Starter vs Advanced (Diátaxis split)

The split mirrors the Diátaxis quadrants documented in [/docs/CONTRIBUTING.md](/docs/CONTRIBUTING.md):

- **`examples/starter/`** — *tutorials.* A single Flowthru concept demonstrated end-to-end in a runnable project. Answers "where do I start?" Each starter should be cloneable: copy the directory, rename the project, modify. No reference to repo-internal paths, no shared infrastructure, no assumptions only valid inside Flowthru's own repo.
- **`examples/advanced/`** — *how-to-guides.* A composition of multiple concepts or a non-default pattern (DI, distributed, GraphQL, Python interop, environment-based catalogs, library/harness). Answers "how do I do X?" Less templatable; assumes the reader has worked through the relevant starters and is building toward a specific production shape.
- **`examples/archived/`** — *staging area.* In-progress or half-baked examples that aren't ready to land in `starter/` or `advanced/` yet. Not a graveyard — completed examples move out of `archived/` into the appropriate Diátaxis-aligned dir.

If you're building an example and aren't sure where it lands, start it in `archived/`. Decide on tutorial vs how-to-guide at promotion time.

## Per-Example Requirements

Every example project in `starter/` and `advanced/` must include:

1. **A README** — Diátaxis-aligned. For starters, tutorial form: assume the reader knows nothing; walk them through what the example does, how to run it, and what concept it demonstrates. For advanced, how-to-guide form: assume the reader has worked through the relevant starter; explain what pattern this composes and what production problem it solves.
2. **A Mermaid diagram of the example's Flow(s)**, output via the [`Flowthru.Extensions.Metadata.Mermaid`](/src/extensions/Flowthru.Extensions.Metadata.Mermaid/) extension, registered in `Program.cs`'s metadata configuration. The diagram is auto-managed by `nx run examples:sync-readmes` ([scripts/update-example-readmes.mjs](/scripts/update-example-readmes.mjs)): the target invokes `dotnet run -- --dry-run` per example to refresh `Metadata/dag-merged.md`, then splices the contents between paired markers in the README:

   ```markdown
   <!-- flowthru:mermaid:start -->
   ```mermaid
   …generated content…
   ```
   <!-- flowthru:mermaid:end -->
   ```

   Authors preserve the marker pair; everything between them is owned by the target. READMEs without the marker pair get one appended at EOF; missing READMEs are scaffolded as `# {ProjectName}` plus the marker block. Examples whose `--dry-run` requires live infra (e.g. Testcontainers) are skipped with a warning — re-run the target after the infra is up, or accept that the diagram only refreshes during full runs.
3. **An acknowledgement in the README** if the example mirrors a Kedro tutorial or other external source, with a link. This preserves intellectual provenance.

## Project Naming

New example projects should be named for the concept or domain they demonstrate (`SpaceflightsDistributed`, `RetailDataSplitFlow`, `FlowthruCoverage`).

The `Kedro*` prefix on older projects is historical — they were direct ports of Kedro's tutorial set. Going forward, **drop the prefix**; Flowthru is differentiated enough from Kedro that the prefix is no longer load-bearing. Existing `Kedro*` projects will be renamed as part of a separate cleanup; the Kedro origin moves to a README acknowledgement.

## Glossary

### Roles

**Flow Developer**: The role that authors Flows — writing Steps, declaring Schemas, and assembling them via FlowBuilder. Analogous to a data analyst or data scientist: the focus is logical correctness of the transformations, not the framework's execution engine, scheduling, or caching.

**Responsibilities:**
- Write Steps as `[FlowthruStep]`-attributed `Create()` factories from input Schemas to output Schemas
- Declare Flows via `FlowBuilder.CreateFlow(name, builder => builder.AddStep<...>(...))` (the API currently uses `pipeline` as the param name; see [[DAG]] _Avoid_)
- Handle the "Transformation" portion of ETL

_Avoid_: pipeline developer, data engineer (the latter is the broader job title; Flow Developer is the Flowthru-specific role)

**Catalog Developer**: The role that builds and maintains Catalogs — defining the Catalog Items (data, configuration, and storage bindings) that Flow Developers consume by name. Insulates Flow Developers from *where* and *how* data is stored.

**Responsibilities:**
- Author Catalog classes and per-[[Data category]] partial files (`Catalog.{Category}.cs`)
- Declare Catalog Items with appropriate backings (CSV, Excel, configuration sections, databases, etc.)
- Handle the "Extract" and "Load" portions of ETL — serializers, storage adapters, schema-format compatibility

_Avoid_: data engineer, ETL developer (Catalog Devs handle Extract and Load only; Transform is the Flow Developer's domain)

### Flow Developer Vocabulary

**Flow**: Flowthru's typed, validated implementation of a data pipeline — a DAG of Steps that produce and consume Catalog Items via Schemas. Declares its Catalog dependencies as type parameters (e.g., `RegisterFlow<MyCatalog>(...)` or `RegisterFlow<CatalogA, CatalogB>(...)`); a project may host many Flows that compose multiple Catalogs.
_Avoid_: workflow, job, pipeline (fine as the general data-engineering concept; in Flowthru-technical contexts use "Flow")

**Step**: A logical unit of work in a Flow. Like a Jupyter notebook cell with named inputs and named outputs — but composable into a type-validated DAG rather than constrained to a linear order.
_Avoid_: task, operator, node

**Schema**: A typed contract for the shape of data flowing through a Flow, declared by both Steps (as their input/output types) and Catalog Items (as the data they hold). The compiler uses these declarations to verify connections at build time.
_Avoid_: type, model, DTO

**Catalog**: A named, typed registry of items — data and configuration alike — scoped to a particular concern. A project may host multiple Catalogs; Flows declare which they need, and Steps reference items by name (`catalog.Companies`) without knowing the actual backing (CSV file, config section, database, etc.).
_Avoid_: repository, data source

**Catalog Item**: A named, typed handle that binds a value — typically a Schema or collection of Schemas — to its backing (CSV file, Excel sheet, config section, etc.). Steps reference Items by name (`catalog.Companies`); the framework loads or saves the value at the right time.
_Avoid_: node, slot

**DAG**: The graph structure of a Flow — directed (data flows one way), acyclic (no cycles), and bipartite between Steps and Items. Edges run only between Steps and Items (never Step-to-Step or Item-to-Item), so connecting two Steps requires routing through an intermediate Item.
_Avoid_: pipeline, workflow graph

**Dry-run mode**: Execution mode that runs all pre-flight checks (DAG validation, external input inspection, etc.) without invoking any Step's actual logic. The result tells you whether the Flow *would* succeed if run for real — no compute, no writes.
_Avoid_: dry mode, preview mode, simulation

### Catalog Developer Vocabulary

**Data category**: A directory under `Data/` indicating where in the Flow lifecycle a Catalog Item sits — `_01_Raw`, `_02_Intermediate`, `_03_Primary`, `_04_Feature`, `_05_ModelInput`, `_06_Models`, `_07_ModelOutput`, `_08_Reporting`. The Catalog is split into `Catalog.{Category}.cs` partial files per category; not every project uses all eight.
_Avoid_: data layer (collides with three-tier/onion architecture), tier, zone, lake layer
*Note*: the Core context's `DAG` entry uses "category" in the formal mathematical sense (objects + morphisms); here, "category" is the classification sense (Raw, Models, etc.). Context disambiguates.

**Configuration Item**: A Catalog Item whose backing is a section of `IConfiguration` (typically `appsettings.json`) rather than a data file. Built via `Item.Of<T>(...).FromConfiguration(...).AtSection(...).Build()`; Steps consume Configuration Items the same way they consume any other input — the framework re-reads the bound section and re-invalidates dependent step caches when the config file changes.
_Avoid_: config, options object, settings
