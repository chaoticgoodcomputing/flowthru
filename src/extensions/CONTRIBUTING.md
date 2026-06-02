# Contributing to Flowthru Extensions

This document is for **Extension Developers** — adding new Catalog formats, Step types, or extensions to Flowthru's public interfaces. Extensions sit at the boundary between Core and the broader .NET ecosystem; they make Flowthru fit a team's stack without changing Core itself.

**Audience scope:** assumes familiarity with [examples/CONTRIBUTING.md](/examples/CONTRIBUTING.md) — Flow Developer and Catalog Developer vocabulary (Flow, Step, Schema, Catalog, Catalog Item, etc.). Terms defined here are the *additional* vocabulary specific to writing extensions.

See [/CONTRIBUTING.md](/CONTRIBUTING.md) for cross-cutting design rules (the three error phases, decision rules for where validation belongs).

## The Extension Pattern: Purpose → Coverage

Extensions follow a deliberate pattern. The starting question is always:

> What popular piece of a traditional ETL stack would fit well in Flowthru?

The answer (EFCore, Python, Parquet, GraphQL, Mermaid diagrams, etc.) becomes the extension's *purpose*. The follow-up question is the extension's *coverage*:

> With knowledge of how the target stack actually works, what maps onto Flowthru's [[Extension surface]], and how?

An extension is the bridge between those two questions: the target stack's mental model translated into Flowthru's extension points.

## The Three Extension Surfaces

Core exposes three primary surfaces for extension:

1. **New ways to define Catalog entries** — storage adapters, item factories, format serializers. Most stack integrations land here (Csv, Excel, Parquet, Xml, EFCore, Http).
2. **New ways to define Steps** — Step authoring patterns and runtime hosts (Python, GQL, source-generator-backed Step types).
3. **Public interfaces for cross-cutting concerns** — metadata providers, diagnostics sinks, schedulers. These don't add new ways to write a pipeline; they extend Flowthru's introspection or execution surface (Metadata.Json, Metadata.Mermaid, Metadata.Diagnostics).

The extension surface is broadcast through *open* (unclosed) polymorphic types — interfaces and abstract base classes Core exposes. An extension *closes* its slice of the surface by providing concrete implementations.

## Naming Convention

`Flowthru.Extensions.<Stack>`:

- For stack integrations, `<Stack>` is the integration partner: `EFCore`, `Python`, `Parquet`, `Excel`, `Xml`, `Http`, `GQL`.
- For cross-cutting extensions, `<Stack>` names the host concern the extension hooks into: `Metadata.Mermaid`, `Metadata.Json`, `Metadata.Diagnostics`.
- Sub-packages for variants on the same stack get a dotted suffix: `Flowthru.Extensions.EFCore.Bulk` complements `Flowthru.Extensions.EFCore`. Use a sub-package when the variant has its own dependency footprint or audience.
- Source-generator companion packages use `.SourceGenerators`: `Flowthru.Extensions.Python.SourceGenerators` is the source-generator companion to `Flowthru.Extensions.Python`.

## Diagnostic ID Namespaces

Roslyn analyzer and source-generator diagnostic IDs are namespaced by extension. Bare `FT` (e.g. `FT1301`, `FT1303`) is reserved for Core; each extension under `src/extensions/` allocates its own short stable suffix and uses `FT<suffix>` for every `DiagnosticDescriptor.Id` it emits.

| Extension | Prefix | Example |
|-----------|--------|---------|
| `Flowthru.Extensions.Python` | `FTPY` | `FTPY1501` |
| `Flowthru.Extensions.Google.Sheets` | `FTGS` | `FTGS1501` |
| (future) `Flowthru.Extensions.SQL` | `FTSQL` | `FTSQL1501` |
| (future) `Flowthru.Extensions.Kafka` | `FTKFK` | `FTKFK1501` |

Mirrors the convention used elsewhere in the Roslyn ecosystem (`CS`, `CA`, `IDE`, `xUnit`): each owner controls a distinct namespace, so diagnostic provenance is obvious from the ID alone, version numbering is independent per owner, and there is no risk of an extension's diagnostic colliding with a future Core diagnostic.

Single spelling across code and docs — write `FTPY1501` in the `DiagnosticDescriptor.Id`, in error messages, and in prose. Never `FT-PY1501` (Roslyn IDs are alphanumeric-only; matching docs to the literal ID avoids confusion when readers grep for a diagnostic they saw in their build output).

Each extension's diagnostics ship with companion code fixes per Core's rule — see [src/core/CONTRIBUTING.md](/src/core/CONTRIBUTING.md#source-generators-and-code-fixes). A design-time-only error is a tax; a design-time error plus code fix is the feature.

## Quality Bar

Every extension must include:

1. **XML docs on public API**, written for the Flow/Catalog Developer audience. The same audience-scope rule as examples/ applies: if your XML doc requires Extension or Core context to understand, it's leaking. A Flow Developer reading IntelliSense should be able to use the extension without reading the source.
2. **A README per extension** explaining what stack the extension covers, what mental model the user should bring from that stack, and how to start using it. Diátaxis: this is reference + how-to-guide.
3. **Tests in `tests/extensions/<Extension>`** targeting **80% coverage**. See [tests/extensions/CONTRIBUTING.md](/tests/extensions/CONTRIBUTING.md) for the testing conventions and the `tests/helpers/` utilities.
4. **A worked example** in `examples/starter/` (for stack integrations that are entry-points for new users) or `examples/advanced/` (for compositions or production patterns). The worked example demonstrates the extension end-to-end in a runnable project — and is what downstream users will copy as a template.

The Examples integration test exercises every worked example, which automatically exercises every extension. The [`FlowthruCoverage`](/examples/advanced/FlowthruCoverage/) advanced example then processes those coverage reports — extension coverage isn't a separate workflow. Tests in `tests/extensions/` cover the extension's *internals*; the worked example covers its *integration*.

## Compiler-Enforced Coverage (Design Intent)

The extension surface is designed so that **changes to Core fail extensions at compile time, not at runtime.** When Core adds a method to an interface or a variant to a public abstract base, every extension that closes that slice of the surface should fail to compile until updated.

The intent: docs can't go stale because the compiler refuses to let them. If you discover a Core change that didn't surface as a compile-time signal in dependent extensions, treat that as a Core bug — file an issue against Flowthru.Core.

*Current enforcement may be uneven across the extension surface; a Core review is warranted to close any remaining gaps.*

## Glossary

### Roles

**Extension Developer**: The role that builds Flowthru extensions — adding new Catalog formats, Step types, or type-safety patterns that feel native to Flowthru's API surface. Works at the boundary between Core and the broader .NET ecosystem.

**Responsibilities:**
- Add Catalog formats (databases via EFCore, file formats like Parquet/Excel)
- Add Step types (Python and Spark steps)
- Add type-safety patterns (typed wrappers for Spark/ML.NET DataFrames)
- Honor the three error phases ([[Design-time error|design-time]], [[Pre-flight error|pre-flight]], [[Runtime error|runtime]]) — push validation as early as possible
- Meet the [Quality Bar](#quality-bar) for every extension

_Avoid_: plugin developer (Flowthru extensions are first-class citizens, not plug-ins)

### Extension Developer Vocabulary

**Extension surface**: The set of open (unclosed) polymorphic types in Core that Extension Developers implement to add new capability. Three primary surfaces: new Catalog entry types, new Step types, and public interfaces for cross-cutting concerns (metadata, diagnostics). An extension *closes* its slice of the surface by providing concrete implementations of the relevant interfaces or abstract base classes.
_Avoid_: plugin API, extension point (too generic — Flowthru's surface is type-shaped, not callback-shaped)

**Shippable package**: A `src/` project that ships to consumers as — or bundled inside — a NuGet package; the unit the per-package documentation standard governs (a README, an API-reference landing, and a per-package coverage badge). The packable libraries: `Flowthru.Core`, the `Flowthru` umbrella, `Flowthru.Cli`, `Flowthru.FUnit`, and every `Flowthru.Extensions.*`. *Excludes* source-generator and code-fix projects (`IsPackable=false` — they ride *inside* a parent package's `analyzers/`, never standalone) and test projects. The boundary is non-obvious because a package's namespace need not match its name — `Flowthru.Extensions.Csv` declares types in the `Flowthru.Core.Data` namespace, so "which package owns this type" is answered by the assembly, not the namespace, which is why cross-package reference links require an assembly-keyed symbol index rather than namespace inference.
_Avoid_: project (too broad — sweeps in tests, source generators, and example Flows), assembly (an implementation artifact; a shippable package is the distributable unit and may bundle several assemblies)

**Closed sum**: An abstract record with a private constructor and a fixed set of sealed nested record variants, consumed by exhaustive pattern matching (typically via a terminal `Match` method). Used throughout Flowthru to model outcomes whose alternatives are known up-front — `EffResult` (Success/Failure), `Validated` (Valid/Invalid), `StepResult`, `RuntimeError`, `PreFlightError` — so consumers must handle every variant and the compiler enforces it.
_Avoid_: discriminated union (correct in theory, but C# users will reach for F#'s `DU` and miss the closed-vs-open distinction), tagged union, polymorphism

**Design-time error**: An error caught while the developer is authoring code — surfaced as IDE squigglies, blocked autocomplete, build failures, or failing FUnit tests, all before any Flow reaches production pre-flight. Flowthru's gold standard error phase, enabled by the C# type system, source generators, Roslyn analyzers, code fixes, and rapid FUnit test execution — push every constraint here that those tools can express.
_Avoid_: compile-time error (too narrow — design-time also covers analyzer diagnostics, IDE guidance, and FUnit test runs), build-time error (too broad — includes linker/packaging failures)

**Pre-flight error**: An error caught after a Flow is invoked but before any Step's logic runs. Used for environmental checks the type system can't express — file existence, schema drift in external data, DAG validation (duplicate producers, cycles).
_Avoid_: startup error, initialization error

**Runtime error**: An error that occurs during actual Step execution. Reserved for truly unpredictable failures — network drops, out-of-memory, hardware faults — that can't be pushed earlier. Flowthru minimizes these by design.
_Avoid_: execution error, "raised exception" (Flowthru's runtime errors are captured as values into a [[Closed sum]], not thrown)

**API Surface**: The set of public types, methods, and attributes that Flow-Project code touches when writing a Flow. CONTRIBUTING.md treats it as one of two primary contributor concerns (alongside [[Error Surface]]) — contributor changes are reviewed against "does this keep the user surface small, expressive, and ceremony-free?"
_Avoid_: public API (correct but missing Flowthru's design-axis framing), surface area

**Error Surface**: The complete set of failure modes a Flow can exhibit — what can fail, in which [[Design-time error|design-time]] / [[Pre-flight error|pre-flight]] / [[Runtime error|runtime]] phase, what the failure looks like, and how it surfaces to the user. CONTRIBUTING.md treats it as the second primary contributor concern (alongside [[API Surface]]) — every new feature is reviewed against "when can this break, and is that point as early as we can make it?"
_Avoid_: error model, failure surface
