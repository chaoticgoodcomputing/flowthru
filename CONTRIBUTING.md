# Contributing to Flowthru

Flowthru is a type-safe, no-bullshit data engineering framework for .NET. Its design philosophy can be summarized in one sentence: **developing an ETL workflow should be easy, and a broken workflow should fail fast.**

First of all — thank you for contributing! I appreciate you taking the time to help make Flowthru better. This document explains the theory behind that philosophy, and helps ensure new features and fixes are aligned with Flowthru's theories and end-user promises.

## Why Fail-Fast Matters

If you've worked with runtime-only ETL frameworks, these scenarios will be familiar:

**The silent schema break.** An upstream team renames a column in a source table from `customer_id` to `cust_id`. Your workflow launches, spends two hours processing raw data through three stages, then fails at a join that expected the old name. In the worst case, the compute is wasted, and the error message points at a symptom (`KeyError: 'customer_id'`) rather than the cause (a contract violation between the producer and consumer).

**The rogue edit.** You're using an interpreted language, and somebody on the team has made a typo in one of the later steps of the workflow. It happens! You start the workflow — a workflow that never *could* have finished — and it fails at the finish line. Somebody must find, and fix, the typo before the workflow can finish, delaying the output and wasting the lead-up computation necessary to reach that point in the step again.

**The silent overwrite.** Two workflow branches independently write to the same output table. Your data isn't part of the DAG — it's just a side effect. There's nothing to check for duplicate producers — whichever branch finishes last wins, and the other branch's output is silently lost. This race condition makes the data unpredictable.

Each of these failures shares a root cause: **your language and framework can't find errors until it *hits* errors**

## Maintaining Flowthru's Core Promise

Flowthru's promises are simple:

1. End-users can easily write ETL workflows, and have a development experience focused on what *their* Flows will do, not how Flowthru is handling the Flow.
2. If an error can occur in the Flow they've created, it will occur as soon in the development process as possible.

Flowthru's architecture is designed to balance these requirements: a straightforward API surface free of unnecessary ceremony or boilerplate, and an error surface that pushes errors as early in the development process as C#, .NET, and Roslyn can offer.

## The Three Error Phases

Every possible failure in a Flow falls into one of three phases:

1. **Design-time**: While projects are being developed (beautiful, gold standard, chef's kiss)
2. **Pre-flight**: After projects are built and run, but before any logic begins executing (tolerable, but aggravating)
3. **Runtime** (evil! should be destroyed wherever possible)

## Flowthru Development Roles

Contributions to Flowthru fall under one of six roles. Each role's full definition, conventions, and vocabulary live in a per-context CONTRIBUTING file:

- **Flow Developer** / **Catalog Developer** — writing Flows and Catalogs on top of Flowthru. See [examples/CONTRIBUTING.md](/examples/CONTRIBUTING.md).
- **Extension Developer** — extending Flowthru with new Catalog formats, Step types, or type-safety patterns. See [src/extensions/CONTRIBUTING.md](/src/extensions/CONTRIBUTING.md).
- **Core Developer** — curating Flowthru's core library and Roslyn surface. See [src/core/CONTRIBUTING.md](/src/core/CONTRIBUTING.md).
- **Tool Developer** — building processes that consume Flowthru from outside a Flow Dev's project: editor frontends, agent frontends, CLI utilities, and the shared Inspector backbone they rely on. See [src/tools/CONTRIBUTING.md](/src/tools/CONTRIBUTING.md).
- **Website Developer** — maintaining the Astro/Starlight site that publishes this documentation: the ingest pipeline, frontmatter validation, link resolution, and theming. See [src/website/CONTRIBUTING.md](/src/website/CONTRIBUTING.md).

Testing-specific conventions for each context live in [tests/core/CONTRIBUTING.md](/tests/core/CONTRIBUTING.md) and [tests/extensions/CONTRIBUTING.md](/tests/extensions/CONTRIBUTING.md).

The design rules in this document apply to all six roles regardless of which context they're working in.

## Context Map

<!-- flowthru:contexts:start -->

A directory is a context iff it *directly* contains a `CONTRIBUTING.md`. This
table is generated from the repo layout by `scripts/generate-context-map.mjs` —
to add a context, add its `CONTRIBUTING.md` and re-run the generator.

| Context | Conventions | Owns ADRs |
|---|---|---|
| `/` | [CONTRIBUTING.md](/CONTRIBUTING.md) | [`docs/adr/`](/docs/adr) |
| `docs` | [docs/CONTRIBUTING.md](/docs/CONTRIBUTING.md) | no |
| `examples` | [examples/CONTRIBUTING.md](/examples/CONTRIBUTING.md) | yes — none yet |
| `src/core` | [src/core/CONTRIBUTING.md](/src/core/CONTRIBUTING.md) | [`src/core/docs/adr/`](/src/core/docs/adr) |
| `src/extensions` | [src/extensions/CONTRIBUTING.md](/src/extensions/CONTRIBUTING.md) | yes — none yet |
| `src/extensions/Flowthru.Extensions.Google.Sheets` | [src/extensions/Flowthru.Extensions.Google.Sheets/CONTRIBUTING.md](/src/extensions/Flowthru.Extensions.Google.Sheets/CONTRIBUTING.md) | [`src/extensions/Flowthru.Extensions.Google.Sheets/docs/adr/`](/src/extensions/Flowthru.Extensions.Google.Sheets/docs/adr) |
| `src/extensions/Flowthru.Extensions.Python` | [src/extensions/Flowthru.Extensions.Python/CONTRIBUTING.md](/src/extensions/Flowthru.Extensions.Python/CONTRIBUTING.md) | [`src/extensions/Flowthru.Extensions.Python/docs/adr/`](/src/extensions/Flowthru.Extensions.Python/docs/adr) |
| `src/tools` | [src/tools/CONTRIBUTING.md](/src/tools/CONTRIBUTING.md) | [`src/tools/docs/adr/`](/src/tools/docs/adr) |
| `src/website` | [src/website/CONTRIBUTING.md](/src/website/CONTRIBUTING.md) | yes — none yet |
| `tests/core` | [tests/core/CONTRIBUTING.md](/tests/core/CONTRIBUTING.md) | yes — none yet |
| `tests/extensions` | [tests/extensions/CONTRIBUTING.md](/tests/extensions/CONTRIBUTING.md) | yes — none yet |

11 context(s). A context may own ADRs under its own `docs/adr/`;
`docs/` is the single exclusion, since documentation decisions are repo-wide.

<!-- flowthru:contexts:end -->

## Glossary

Repo-wide vocabulary — the terms every context depends on and none of them owns.
Terms specific to a context live in that context's own `CONTRIBUTING.md`; see the
context map above. Each entry's `_Avoid_` line is the enforcement: those synonyms
are not interchangeable substitutes, and output that reaches for one has drifted.

**Context**: A directory that *directly* contains a `CONTRIBUTING.md`. That is the whole rule, which is what makes the context set mechanically enumerable and the map above generated rather than maintained. Contexts are minted **lazily** — a directory becomes one when it accrues vocabulary or decisions with nowhere else to live, and a package cannot own an ADR until it does. The relationship to [[Shippable package]] runs one way and loosely: a shippable package *becomes* a context once it has its own words, but many contexts ship nothing at all (`examples`, `tests/core`, `tests/extensions`, `docs`).
_Avoid_: module (no build or packaging meaning here), bounded context (the DDD term carries a domain-model boundary Flowthru does not claim), package (a context need not ship, and a shipping package need not be a context yet)

**Shippable package**: A `src/` project that ships to consumers as — or bundled inside — a NuGet package; the unit the per-package documentation standard governs (a README, an API-reference landing, and a per-package coverage badge). The packable libraries: `Flowthru.Core`, the `Flowthru` umbrella, `Flowthru.Cli`, `Flowthru.FUnit`, and every `Flowthru.Extensions.*`. *Excludes* source-generator and code-fix projects (`IsPackable=false` — they ride *inside* a parent package's `analyzers/`, never standalone) and test projects. The boundary is non-obvious because a package's namespace need not match its name — `Flowthru.Extensions.Csv` declares types in the `Flowthru.Core.Data` namespace, so "which package owns this type" is answered by the assembly, not the namespace, which is why cross-package reference links require an assembly-keyed symbol index rather than namespace inference.
_Avoid_: project (too broad — sweeps in tests, source generators, and example Flows), assembly (an implementation artifact; a shippable package is the distributable unit and may bundle several assemblies)

**API Surface**: The set of public types, methods, and attributes that Flow-Project code touches when writing a Flow. One of two primary contributor concerns (alongside [[Error Surface]]) — contributor changes are reviewed against "does this keep the user surface small, expressive, and ceremony-free?"
_Avoid_: public API (correct but missing Flowthru's design-axis framing), surface area

**Error Surface**: The complete set of failure modes a Flow can exhibit — what can fail, in which [[Design-time error|design-time]] / [[Pre-flight error|pre-flight]] / [[Runtime error (phase)|runtime]] phase, what the failure looks like, and how it surfaces to the user. The second primary contributor concern (alongside [[API Surface]]) — every new feature is reviewed against "when can this break, and is that point as early as we can make it?"
_Avoid_: error model, failure surface

**Design-time error**: An error caught while the developer is authoring code — surfaced as IDE squigglies, blocked autocomplete, build failures, or failing FUnit tests, all before any Flow reaches production pre-flight. Flowthru's gold standard error phase, enabled by the C# type system, source generators, Roslyn analyzers, code fixes, and rapid FUnit test execution — push every constraint here that those tools can express.
_Avoid_: compile-time error (too narrow — design-time also covers analyzer diagnostics, IDE guidance, and FUnit test runs), build-time error (too broad — includes linker/packaging failures)

**Pre-flight error**: An error caught after a Flow is invoked but before any Step's logic runs. Used for environmental checks the type system can't express — file existence, schema drift in external data, DAG validation (duplicate producers, cycles).
_Avoid_: startup error, initialization error

**Runtime error (phase)**: The third error phase — an error that occurs during actual Step execution. Reserved for truly unpredictable failures (network drops, out-of-memory, hardware faults) that cannot be pushed earlier. Flowthru minimizes these by design; they are captured as values rather than thrown. The `(phase)` parenthetical is load-bearing: Core carries a `RuntimeError` *type* under the same name, disambiguated in its own entry name rather than by a note here — root vocabulary never reaches down into a context's.
_Avoid_: execution error, "raised exception" (Flowthru's runtime errors are values, not throws)

## What Flowthru *Won't* Be

Flowthru, at its core, will *not* be a full piece of orchestration software. The core library will not be concerned with when or how users want to run their Flows — just that it will be correctly configured, and as stable as possible, when they do.

This doesn't mean *ignoring* these concerns — it just means extending the API surface to allow end-users to run Flows flexibly (such as the service-based and CLI access options), as well as ensuring the core engine uses extensible patterns for modification (such as additional formats and methods for data access, and the ability to DI services into Steps for additional utility).

## Development Workflow

### Building and Testing

The project uses NX for task orchestration. When possible, use `nx run` targets over `dotnet` directives, as the `nx` targets may include prerequisites to target runs.

```bash
nx run-many -t build # Confirm solution builds fully
nx run affected -t test # IMPORTANT: Run all test projects affected by current changes
nx run Spaceflights # Run a specific Flowthru example Flow
```

`dotnet` can be used to run subsets of tests, or specific tests:

```bash
dotnet test --filter "Category=Compilation"
```
