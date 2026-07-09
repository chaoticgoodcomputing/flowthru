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

1. **A README** that conforms to [§ README Standards](#readme-standards).
2. **A Mermaid diagram of the example's Flow(s)**, output via the [`Flowthru.Extensions.Metadata.Mermaid`](/src/extensions/Flowthru.Extensions.Metadata.Mermaid/) extension, registered in `Program.cs`'s metadata configuration. The diagram is auto-managed by `nx run examples:sync-readmes` ([scripts/update-example-readmes.mjs](/scripts/update-example-readmes.mjs)): the target invokes `dotnet run -- --dry-run` per example to refresh `Metadata/dag-merged.md`, then splices the contents between paired markers in the README:

   ```markdown
   <!-- flowthru:mermaid:start -->
   ```mermaid
   …generated content…
   ```
   <!-- flowthru:mermaid:end -->
   ```

   Authors preserve the marker pair; everything between them is owned by the target. READMEs without the marker pair get one appended at EOF; missing READMEs are scaffolded as `# {ProjectName}` plus the marker block. Examples whose `--dry-run` requires live infra (e.g. Testcontainers) are skipped with a warning — re-run the target after the infra is up, or accept that the diagram only refreshes during full runs.

   The same target also manages a filetree breadcrumb under `<!-- flowthru:filetree:start -->` / `<!-- flowthru:filetree:end -->` markers. The block contains a plain-fenced ASCII tree of the example directory — its job is to anchor the mermaid diagram onto the filesystem, not to stand alone as documentation:

   ```markdown
   <!-- flowthru:filetree:start -->
   ```
   ExampleName/
   ├── Program.cs  # entry point
   ├── Data/
   …
   ```
   <!-- flowthru:filetree:end -->
   ```

   Pruning rules: at each project-boundary directory (the example root, plus any nested dir containing a `.csproj`), only `Program.cs` survives among files — it carries a fixed `# entry point` annotation. Inside `Data/`, the dotted-segment catalog plumbing (`Catalog.cs`, `Catalog.<Category>.cs`, `Catalog.<X>.<Y>.cs`) is stripped, and the `_NN_<name>` category subdirs are elided to the first and last only (with `...` between). Inside each `Flows/<FlowName>/` directory, the matching `<FlowName>Flow.cs` registration file is stripped. The tree carries no other inline annotations — Schemas and Steps speak for themselves; if you need prose, write it outside the marker block.

   Hand-authored `## File Structure` / `## Project Structure` blocks present at first sync are migrated in place into marker-wrapped auto-managed blocks; any annotations inside the original fence are dropped — see the marker as a contract that the section's content is now generated.

## Logging from Steps

Steps that want to emit log lines declare `ILogger` (non-generic) as a parameter on their `Create()` factory:

```csharp
[FlowthruStep]
public static class PreprocessCompaniesStep
{
  public static Func<IEnumerable<CompanySchema>, IEnumerable<PreprocessedCompanySchema>> Create(
    ILogger logger)
  {
    return input =>
    {
      var rows = input.ToList();
      var processed = rows.Select(Parse).Where(item => item != null).Cast<PreprocessedCompanySchema>().ToList();
      var dropped = rows.Count - processed.Count;
      if (dropped > 0)
      {
        logger.LogWarning("Dropped {Count} rows with invalid rating percentages", dropped);
      }
      return processed;
    };
  }
}
```

The `[FlowthruStep]` source generator recognises interface-typed `Create()` parameters as service dependencies, so no other wiring is required — `AddFlowthru` registers a singleton `ILogger` resolved as `loggerFactory.CreateLogger("Flowthru")`, and the engine and every step share that single logger identity under the category `Flowthru`. Flow factories resolve it like any other DI dependency:

```csharp
b.RegisterFlow<MyCatalog, ILogger>(
  "main",
  (catalog, logger) => FlowBuilder.CreateFlow("main", p =>
    p.AddStep<TIn, TOut>(
      "preprocess",
      PreprocessCompaniesStep.Create(logger),
      catalog.RawCompanies,
      catalog.PreprocessedCompanies))
);
```

The convention is *declare when useful*, not *declare always*. Pure-transform steps that have no events worth narrating leave `Create()` parameterless — adding `ILogger` everywhere just to satisfy a rule produces noise, not signal.

Hosts wire logging the standard .NET way:

```csharp
services.AddLogging(b => b.AddConsole());
services.AddFlowthru(b => { /* ... */ });
```

Without `AddLogging()`, the logger resolves to a `NullLogger` via `AddFlowthru`'s `TryAdd<NullLoggerFactory>` fallback and calls are silently dropped. Engine internals (`FlowthruService`, `ParallelFlowScheduler`) follow the same convention and render their lifecycle lines through the same shared `ILogger`.

**Per-step categorization (opt-in).** The default collapses every Flowthru log into one `"Flowthru"` category — engine and steps are indistinguishable in the log stream. Hosts that want per-step filtering in `appsettings.json` take `ILoggerFactory` in `Create()` and build a categorized logger themselves:

```csharp
public static Func<...> Create(ILoggerFactory loggerFactory)
{
  var logger = loggerFactory.CreateLogger(typeof(PreprocessCompaniesStep).FullName!);
  // ...
}
```

This is the escape hatch, not the default — most flows are fine with the single shared category.

The full rationale lives in [.claude/docs/adr/0005-step-logging-via-shared-ilogger.md](/.claude/docs/adr/0005-step-logging-via-shared-ilogger.md).

## README Standards

Every example README follows a three-section skeleton: `## Getting Started`, `## Concepts`, `## Structure`. Structural conformance is enforced by the README meta-test (see [§ Meta-test scope](#meta-test-scope)); deviations fail the build.

### Title and lead-in

```markdown
# <ProjectName> <Starter|Advanced>

> [!NOTE]
> How do I <the question this example answers>?

This project demonstrates <one-sentence declarative paraphrase of the answer>.

This project:
- <one bulleted broad stroke per major piece of pipeline behavior>
- <...>

<Scope boundary + audience contract — non-vanilla only.>
<Acknowledgement, if the Flow structure derives from an external source.>
```

The lead-in has four subsections:

1. **The question.** The `[!NOTE]` callout — the literal question this example answers, written in [Diátaxis](/docs/CONTRIBUTING.md) form. Starters answer *learning* questions ("How do I get started with `<X>` in Flowthru?"); advanced answer *working* questions ("How do I `<specific pattern>`?"). If you can't write the question in one sentence, the example is doing too much.
2. **The short answer.** One declarative sentence beginning `This project demonstrates …` that paraphrases the question into the answer. Useful for skimmers who want a one-line confirmation before deciding whether to keep reading.
3. **The overview.** A bulleted list beginning `This project:` — one bullet per broad stroke of pipeline behavior (typically one bullet per Flow, plus any structural-shape callout like "exercises all eight Data categories"). Scannable; tells the reader what the example *does* without prose density.
4. **The acknowledgement** (optional). One sentence at the end of the lead-in if the Flow structure derives from an external source — see **Acknowledgement** below.

**Scope boundary.** A hard rule: **everything in `starter/` is a template**, and **nothing in `advanced/` is a template**. Starters get scaffolded via `dotnet new` from `.template.config/template.json` and must remain cloneable. Advanced examples are reference-only — they exist to be read, not copied. Starters (vanilla or extension) omit the scope-boundary sentence — the tier already conveys "template," and template-enumeration guidance lives in central onboarding docs. Advanced examples include a scope-boundary sentence; depth-probe advanced examples must include an explicit **"not a template"** warning, since the risk of readers cloning them is highest.

**Audience contract.** Names who this is written for. Vanilla starters omit this — "no Flowthru background assumed" is implicit. Extension starters and advanced examples state assumed reading:

- *Extension starter:* "assumes you've worked through `<vanilla starter>`."
- *Advanced:* "assumes you've worked through `<starter A>` and `<starter B>`."

**Avoid negative-space sentences.** Don't list what the example *doesn't* exercise ("no DI, no Python, no persistence — see…"). The tier (starter vs. advanced) and the audience contract already convey scope; "what isn't here" is noise.

**Acknowledgement.** If the *structure of the Flow* derives from a Kedro tutorial (or other external source), the final sentence of the lead-in must acknowledge the lineage and link to the source (e.g., [`kedro-org/kedro-starters`](https://github.com/kedro-org/kedro-starters)). The rule is structure-based, not dataset-based: `SpaceflightsPythonEFCore` acknowledges (Flow shape is Kedro-derived); `RetailDataSplitFlow` does not (Flow is original).

### `## Getting Started`

A minimal, copy-pasteable invocation block.

- **Starters:** assume the reader is outside this monorepo (they may have just `dotnet new`'d the template). The run command is `dotnet run`.
- **Advanced:** assume the reader is inside this monorepo. The run command is `nx run <ProjectName>` (project name from the CSProj — `nx run FlowthruCoverage`, `nx run RetailDataSplitFlow`, etc.).
- **Prerequisites** (Python 3.10+, `uv sync`, Docker for Testcontainers, etc.) go inline above the run block as one sentence per prereq. Do not introduce a separate `### Prerequisites` subsection.
- **What success looks like.** One sentence after the run block linking to the canonical output file(s).
- **Multi-flow or env-var-driven examples** document only first-time-reader invocations; defer the full surface to `--help` or a code link. Exception: when the invocations themselves are the concepts being demonstrated (e.g., `SpaceflightsHybridCatalog`'s `ASPNETCORE_ENVIRONMENT` toggle, or `SpaceflightsStagingSchema`'s `--dry-run` lifecycle), they may be enumerated explicitly here.

### `## Concepts`

Bulleted list. Each bullet names a pattern in bold and links to the file (or file + line range) that load-bears it. Soft cap of 8 bullets — exceeding 8 is a signal the example is doing too much.

Per-archetype rules:

- **Vanilla starter** (`Iris`, `Spaceflights`, `Minimal`). Each bullet = one Flowthru primitive (Step, Schema, Catalog, Catalog Item, Data category, FlowBuilder), linked to one concrete instance in this example.
- **Extension starter** (everything else under `starter/`). Each bullet = one piece of the extension's public surface that the example exercises. **Do not re-explain primitives** — the audience contract already said "you've done the vanilla starter." The Concepts section is a *conceptual diff* between this example and base Flowthru.
- **Interaction advanced** (`SpaceflightsHybridCatalog`, `SpaceflightsPythonEFCore`, `SpaceflightsStagingSchema`). Each bullet = one interaction point between concepts that already work on their own. Focus on what makes the combination non-obvious.
- **Depth-probe advanced** (`SpaceflightsEnhanced`, `FlowthruCoverage`). Each bullet = a pattern that emerges at scale. The Concepts section must repeat the **"not a template"** reminder from the lead-in — this is the section a casual reader will skim looking for code to copy.

### `## Structure`

Two sub-headers in order: `### Diagram` (mermaid marker pair) and `### Files` (filetree marker pair). Each may have an optional one-sentence prose intro above its marker pair; no required prose. Marker mechanics are defined in [§ Per-Example Requirements](#per-example-requirements).

### Linking

Starter READMEs travel with their project — `dotnet new Flowthru.<X>` clones the directory into a downstream user's repo, and the README has to keep resolving its links from that new location. Advanced READMEs don't travel (they're not templates), so the rule is starter-specific.

For starters:

- **Intra-project links** (anywhere under `./`): use relative paths — they survive the `dotnet new` move.
  ```markdown
  [Step](./Flows/DataEngineering/Steps/SplitAndEncodeStep.cs)
  ```
- **Cross-project links** (sibling examples, repo-level files): use absolute URLs to `chaoticgoodcomputing/flowthru` on GitHub — relative paths break after cloning.
  ```markdown
  [Iris](https://github.com/chaoticgoodcomputing/flowthru/tree/main/examples/starter/Iris)
  ```
- **External links** (Kedro, docs.microsoft.com, etc.): absolute URLs as usual.

For advanced READMEs, sibling-example links may stay relative (`../SpaceflightsEFCore/`), since the README is read in-repo.

### Vocabulary

README prose uses canonical Flowthru vocabulary as defined in the [Glossary](#glossary).

**Hard-banned in prose** (meta-tested): `node`, `task`, `operator`, `data layer`, `tier`, `DTO`, `repository`, `data source`.

**Soft-banned:** `pipeline` — allowed only when referring to the general data-engineering concept ("this is a typical ML training pipeline pattern"), banned in Flowthru-specific context ("this pipeline reads from CSV"). Use "Flow" in Flowthru-specific context.

**Exemptions.**

- Code blocks are exempt — embedded Python that uses `@node` keeps `@node` (it's the actual API surface).
- External-source acknowledgements are exempt — "modeled after the Kedro Spaceflights pipeline" is referring to Kedro's term for its own concept.
- Mermaid block contents are not prose.

**Embedded code.** Prefer file links over embedded snippets — source cannot lie; an embedded snippet can drift silently. When a snippet is unavoidable, the reviewer verifies it faithfully reflects current source. If the *source* itself uses non-canonical vocabulary, file a separate GitHub issue; do not silently rewrite it in the README.

### Meta-test scope

A README meta-test (target TBD; likely in a new `tests/examples/` project or as a `scripts/` check called by an NX target) asserts, per example:

- Title matches `# <ProjectName> <Starter|Advanced>` with the tier matching the parent directory.
- `> [!NOTE]` callout present immediately after the title.
- `## Getting Started`, `## Concepts`, `## Structure` headers present in that order; no other top-level `##` headers.
- `### Diagram` and `### Files` sub-headers present under `## Structure` in that order, each with its marker pair populated.
- Starters have a `.template.config/template.json`; advanced does not.
- Prose outside fenced code and outside marker pairs contains none of the hard-banned vocabulary tokens. Soft-banned `pipeline` produces a flagged-for-review list, not a hard failure.

## Project Naming

New example projects should be named for the concept or domain they demonstrate (`SpaceflightsDistributed`, `RetailDataSplitFlow`, `FlowthruCoverage`).

The `Kedro*` prefix on older projects is historical — they were direct ports of Kedro's tutorial set. Going forward, **drop the prefix**; Flowthru is differentiated enough from Kedro that the prefix is no longer load-bearing. Existing `Kedro*` projects will be renamed as part of a separate cleanup; the Kedro origin moves to a README acknowledgement.

## Local State

Flowthru tooling (notably the Inspector) writes per-project local state under `.flowthru/` — pre-flight Manifest snapshots, cache manifests, log archives, user preferences. Every Flowthru project's `.gitignore` should include:

```
.flowthru/
```

The dot-prefix convention matches `.git/`, `.vscode/`, `.idea/`, etc. — anything under `.flowthru/` is Flowthru-owned, regenerable, and never belongs in version control.

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

**Wide vs narrow transform**: A classification of Step logic. A *narrow* transform produces each output row from one input row at a time (parse, filter, map) and is a natural fit for an ordinary C# Step; a *wide* transform (join, aggregate, global sort, dedup) must see all of its input before it can emit any output — the set-oriented work worth handing to an engine-side SQL Step when the data is large.
_Avoid_: blocking step ("blocking" reads as thread semantics in .NET), shuffle (cluster vocabulary; Flowthru is single-node)

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
