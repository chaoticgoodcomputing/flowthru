---
status: accepted
---

# Wide transforms and homogeneous transfers execute outside the CLR — as delegated arrows between ordinary items, never as new payload kinds

Row-at-a-time execution pays a 10–20× tax on **wide transforms** (see the [wide vs narrow transform](/src/extensions/CONTRIBUTING.md#glossary) glossary entry): sorting a 43M-row Parquet by composite key took ~62s as a LINQ `OrderBy` step against ~4s in DuckDB on the same machine — the cost being ~215M object materialisations and cache-hostile reference sorting, not the algorithm (#126). Pure DB→DB movement pays the same shape of tax: a row-by-row `NpgsqlDataReader.GetValue()` loop boxing every field was the bottleneck in a 40M-row cross-database promotion until replaced by a hand-rolled raw binary COPY passthrough (#127). [ADR-0023](./0023-streaming-reads-as-catalog-item-type.md)'s `FlowSource` bounds *narrow* transforms to O(batch) memory, but by design cannot cross a wide transform, and both its data-movement models still route every byte through the CLR.

**The decision:** both gaps are filled by *arrows that execute outside the CLR* between *ordinary typed items* — an **engine-delegated transform** (storage→storage relational compute, DuckDB first) and a **negotiated bulk transfer** (provider-native passthrough, Npgsql↔Npgsql first). Neither introduces a new payload kind, a new place archetype, or a relation-typed item. The "execution planes" framing (row / engine / byte) used while designing this is **ADR-narrative only** — it names no type, enum, glossary entry, or diagnostic; the durable commitments are the per-concept names below.

## Decided

### Classification: wide vs narrow routes the work

A *narrow* transform streams via `FlowSource` in O(batch) memory. A *wide* transform (global sort, dedup, aggregate, join) must consume all input before emitting output, so it either materialises the eager view (small data — today's guidance, unchanged) or delegates to an engine (large data — this ADR). The canonical term is **wide/narrow** (Spark's vocabulary); *pipeline breaker*, *blocking operator*, *barrier*, and especially *batch operation* are rejected spellings — "batch" already means a stream *chunk* in Flowthru (`BatchSize`, O(batch)), so "batch operation" would invert its own meaning.

### The engine is a service, not a place

The engine transform is **one step wired between ordinary typed items** (Parquet in, Parquet out first) whose entire body executes inside the engine — rows never enter the CLR; the win is banked only if the data stays engine-side (#126's make-or-break point). The DAG stays a bipartite category of Kleisli arrows composing through byte-holding places:

- **No relation-typed items in v1.** An item whose payload is an unexecuted expression holds no bytes — `Exists()`, fingerprinting, and inspection go hollow, the `.Memory()`/MagicAtlas failure shape. This mirrors ADR-0023 forbidding `FlowSource`-typed intermediates. Composition happens *inside* one delegated arrow (SQL), not across items. A Kedro/Ibis-style deferred-relation graph is a possible future ADR, not this one.
- **The engine is a `ServiceDependency`** with a profile mirroring the Python worker precedent: cache-*neutral* (`AffectsOutputs=false` — determinism is captured by the query text and engine version) yet concurrency-*constrained* (memory/disk capacity keys per [ADR-0019](./0019-concurrency-conflict-relation-and-resource-profiles.md)). The step additionally inherits the conflict keys of both endpoint items.
- **`Item.Introspection` is untouched.** Unlike `FlowSource` (which required a new `StepContainerKind.Source` case), endpoints here are ordinary `IEnumerable`-payload items; there is nothing new for the introspection layer to learn.
- **DuckDB-as-storage-medium is explicitly out of scope** — a separable follow-up. The transform reads and writes existing formats (Parquet, CSV; Postgres later via `ATTACH`) directly.

### v1 relational surface: schema-validated SQL

The transform body is SQL text — but validated against the three error phases, not runtime-only:

- **Hermetic pre-flight:** build empty in-engine tables from the *declared* input record schemas, prepare the SQL against them, and check the `DESCRIBE`d result schema against the declared output schema. No I/O against real data; column-name and type mismatches against declared contracts fail before any step runs. (Shallow/Deep depths may additionally validate against real file schemas.)
- **Design-time:** the same check surfaces as failing FUnit tests — design-time by the glossary's definition. DuckDB is embedded and fast enough to run in unit tests.
- Item labels bind to SQL relation names explicitly at wire-up. Exact API names and binding shape are issue-level.
- **The typed rung above SQL** — a C# relational expression surface compiling to this same primitive (`Flowthru.Misc.DataFrames` continuation or linq2db) — is deliberately deferred to #131. It must move schema errors from pre-flight to design-time, not sideways.

### Core gains two narrow seams; everything else is extension-owned

Following [ADR-0020](./0020-s3-storage-medium-via-gateway-seam.md)'s "one provider + DI registration, no Core change" spirit, Core gains only:

1. **A byte-location capability** — the seam letting an engine extension address an item's bytes (file path, S3 URI + credential handoff via the existing gateway seams) *without* loading rows. Sibling of `ISupportsStreamingView` on `ComposedStorageAdapter`, feature-detected the same way.
2. **The transfer pairing capabilities** — `ISupportsBulkExport` / `ISupportsBulkImport` (names indicative), hosted in Core's cross-cutting interface surface so any two extensions can pair without referencing each other, plus the intent verb (`AddBulkTransfer`) and its pre-flight negotiation, which are medium-agnostic.

The DuckDB step type, SQL validation, and the Npgsql COPY rung all live in extensions (`Flowthru.Extensions.DuckDB`; the Npgsql/EFCore extension family). A core-owned "engine step archetype with pluggable providers" was rejected as speculative generality while exactly one engine exists.

### Transfer: an on-DAG identity arrow with pre-flight negotiation and a visible rung

`AddBulkTransfer(source, target)` emits an on-DAG identity step (the `AddBulkLoad` precedent), buying scheduling, conflict keys on both endpoints, caching, and pre-flight for free. Rung selection:

- **Negotiation happens at pre-flight**, where media and connection config are resolved: probe both endpoints' capabilities and pair compatibility (same provider, same wire format), select the rung, and **report the selected rung in the validated plan** — a downgrade to the streaming fallback is allowed by default but never silent (a 40M-row table quietly taking the 100× path is the MagicAtlas failure shape in a new hat).
- A `RequireNative`-style option turns an unavailable native path into a pre-flight error for Flows that would rather fail than stream.
- **First rung:** Npgsql↔Npgsql raw binary COPY (`BeginRawBinaryCopy` passthrough — true byte-level transfer). **Bottom rung:** the ADR-0023 streaming path (`FlowSource.Into`). An engine-mediated middle rung (DuckDB `ATTACH` for heterogeneous pairs) is a future rung, not v1.
- Runtime try-then-fallback was rejected (invisible downgrade + messy partial-write semantics inside the sink transaction); wire-up-time negotiation was rejected (media/config aren't resolved yet — it would over-promise design-time certainty about a runtime pairing).

### Cache identity: cacheable, with the query in the key

Both new step kinds are first-class cacheable — endpoints hold bytes and fingerprints, so there is no new hollowness. The engine transform's cache identity extends the usual code-version + input-fingerprint key with **hash(SQL text) + engine version + transform options**, because the SQL is wire-up data, not compiled step code — without this, editing a query would silently serve stale cached output. Transfers follow existing rules unchanged: fingerprintable targets can skip; DB-backed unfingerprintable targets are uncacheable exactly as EFCore saves are today.

## Considered options

- **"Plane" as a code axis or glossary term.** Rejected: a `Plane` enum would half-duplicate `StepContainerKind` (row spans `Enumerable`+`Source`; byte has no container kind at all — it is an *execution locus*, a property of the arrow, not a payload shape), and code must self-document without the ADR. Narrative only.
- **DuckDB as a storage medium first.** Rejected for v1: the win only materialises when both endpoints already live in DuckDB; the Parquet→Parquet sort that motivated #126 still routes through the CLR until the transform primitive exists. Medium is a follow-up.
- **Relation-typed items (full Kedro+Ibis deferred graph).** Rejected: hollow places (no bytes → no `Exists()`/fingerprint/inspection), the exact failure ADR-0021's validation model and the `.Memory()` cache-cascade lesson warn about.
- **linq2db from day one.** Rejected for v1: couples the API surface to a months-old third-party DuckDB provider and expression-tree compilation (weak under NativeAOT). Reconsidered at #131.
- **An own expression DSL now.** Rejected for v1 as a language-design project gating a 15× win; logged as #131 with the `Flowthru.Misc.DataFrames` prototype as its starting point.
- **Raw SQL with runtime-only validation.** Rejected: a schema mismatch surfacing two hours into a run is the exact failure mode `/CONTRIBUTING.md`'s opening scenarios exist to kill.
- **Uncacheable v1 / code-version-only cache identity.** Rejected: the former re-creates a silently uncacheable hot path on the framework's largest datasets; the latter holds only while SQL is a compile-time literal and breaks silently the day it is templated.
- **Naming: "pipeline breaker" / "blocking operator" / "barrier" / "batch operation".** Rejected in favour of **wide/narrow** — "pipeline" is on the core DAG entry's avoid list, "blocking" reads as thread semantics in .NET, "breaker" reads as the circuit-breaker resilience pattern, and "batch" already means chunk in Flowthru.
- **Two ADRs (engine / transfer separately).** Rejected: they share the decisions that motivated consolidating #126/#127 in the first place — capability seams, negotiation visibility, cache identity, and the wide/narrow classification.

## Consequences

- **Flow developer** — additive and opt-in: an engine-transform verb for wide transforms over large data, `AddBulkTransfer` for homogeneous movement; both wired between ordinary items. Wrong pairings and unavailable native paths fail at pre-flight with the selected rung visible in the plan.
- **Catalog developer** — nothing new in v1; items are declared exactly as today.
- **Extension developer** — a new extension (`Flowthru.Extensions.DuckDB`) owning the engine step + SQL validation; `ISupportsBulkExport`/`Import` implementations on the Npgsql-backed media; the [wide vs narrow transform](/src/extensions/CONTRIBUTING.md#glossary) vocabulary governs which step shapes may claim streaming.
- **Core developer** — two narrow seams (byte-location capability; transfer pairing interfaces + `AddBulkTransfer` negotiation) and the cache-identity extension for query-text-bearing steps. `Item.Introspection`, the scheduler, and the DAG model are untouched.
- **Issues** — #126 and #127 close as consolidated into the milestone cut from this ADR; #131 tracks the typed rung; #129 (EFCore.BulkExtensions licensing) is adjacent but independent — its outcome determines the foundation for the streaming-sink work this ADR's bottom rung relies on.

## Anchor code

- `src/extensions/Flowthru.Extensions.DuckDB/` **(new)** — the engine-delegated transform step, schema-validated-SQL pre-flight (empty-table prepare + `DESCRIBE` check), engine `ServiceDependency` profile.
- `src/core/Flowthru.Core/Data/Storage/` — the byte-location capability interface **(new)**, sibling of `ISupportsStreamingView.cs`, implemented by `ComposedStorageAdapter`.
- `src/core/Flowthru.Core/` — `ISupportsBulkExport` / `ISupportsBulkImport` **(new)** in the cross-cutting capability surface (alongside `ISupportsFingerprint.cs`); the `AddBulkTransfer` intent verb + pre-flight rung negotiation (sibling of `FlowBuilderStreamingExtensions.AddBulkLoad`).
- `src/extensions/Flowthru.Extensions.EFCore*/` — the Npgsql↔Npgsql raw binary COPY rung (`BeginRawBinaryCopy`).
- `src/core/Flowthru.Core/Prelude/FlowSource.cs` + `IFlowSink.cs` — the unchanged streaming bottom rung (`Compile().Into`).
- `src/misc/Flowthru.Misc.DataFrames/` — dormant prototype; starting point for #131's typed rung.
- Evidence: [#126](https://github.com/chaoticgoodcomputing/flowthru/issues/126) (62s→4s, 43M-row sort), [#127](https://github.com/chaoticgoodcomputing/flowthru/issues/127) (raw COPY passthrough), both filed from the downstream NWYC seed-optimisation exercise.
