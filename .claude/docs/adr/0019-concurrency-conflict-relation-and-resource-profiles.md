---
status: proposed
---

# Step concurrency is a conflict relation over the DAG, driven by per-resource capacity profiles

The DAG encodes one relation between nodes — **precedence** (a producer before its consumer), the partial order `DependencyAnalyzer` topologically sorts and `ParallelFlowScheduler` relaxes into parallel execution. That relaxation is currently unsound: the scheduler assumes any two precedence-incomparable steps may co-run, but two steps that share a non-shareable side-effecting resource must not, even with no data dependency between them. We will model that second, orthogonal relation — **conflict** — at the scheduler layer, derived from per-resource **capacity profiles**, without changing the place/arrow category itself. A node may run iff it is unblocked by precedence (already handled) *and* admitting it would not exceed any shared resource's capacity. The motivating instance is the Python worker: `IPythonExecutor` is a singleton ([PythonFlowthruBuilderExtensions.cs:67](/src/extensions/Flowthru.Extensions.Python/Hosting/PythonFlowthruBuilderExtensions.cs#L67)) whose `SendRequest` serializes every call on one lock over one worker pipe ([SubprocessPythonExecutor.cs:587](/src/extensions/Flowthru.Extensions.Python/Step/Python/Internal/SubprocessPythonExecutor.cs#L587)), so raising `ExecutionOptions.Parallelism` ([ExecutionOptions.cs](/src/core/Flowthru.Core/Flow/ExecutionOptions.cs)) above 1 on a Python-heavy flow yields zero real concurrency and a net loss (lock queueing plus blocked threadpool threads). EFCore is the second instance on a different node archetype: SQLite is single-writer, so concurrent saves to one `DbScope` ([DbScope.cs](/src/extensions/Flowthru.Extensions.EFCore/Data/Storage/EFCore/DbScope.cs)) fail at runtime — the same class of silent-runtime failure, surfaced through an item rather than a step.

This is the **orthogonal twin of the caching decision**. The cache planner already triages the same `ServiceDependencies` ([IStepNode.cs:110](/src/core/Flowthru.Core/Step/IStepNode.cs#L110)) on a different axis — a step is cacheable iff it has no output-affecting service deps, with `ServiceRef.ObservationOnly` carved out as cache-neutral ([CachePlanBuilder.cs:126](/src/core/Flowthru.Core/Caching/CachePlanBuilder.cs#L126), [ADR-0010](./0010-observation-only-service-refs.md)). Parallel-safety is the same fold over the same carrier, reading a different per-service bit. A service therefore has a **profile** with independent fields: `AffectsOutputs` (cache) and a concurrency `Capacity` (scheduler). The Python executor is the proof these must be independent: it is cache-*neutral* (determinism is captured by `CodeVersion` derived from `.py` source + interpreter + lockfile, so a pure `@step(cacheable=True)` Python step is correctly cacheable) yet concurrency-*constrained* (capacity 1). No single flag — including `ObservationOnly` — can express "cache-neutral but serial," because the executor is the opposite of observation-only: its call *produces* the output.

## Decided

- **Conflict lives in the scheduler (the interpreter), not the DAG (the syntax).** Profiles never add edges; topological order is always honored, so correctness never depends on the conflict layer. This is the established role of `IFlowScheduler` as the algebra-interpretation extension point.
- **Capacity-N, not binary.** A resource declares a max concurrent holder count; 1 = mutex, ∞ = unconstrained, N = a warm pool (Python worker pool, DB connection pool). Binary cannot express "up to 4."
- **The profile lives on the resource; nodes declare a dependency on it.** Authored once by the resource owner (usually an Extension developer); referenced by nodes.
- **Service-dependency-and-conflict is an `INode` concern, not step-only.** Both archetypes carry it — steps (arrows, [IStepNode.cs](/src/core/Flowthru.Core/Step/IStepNode.cs)) declared by Flow developers at the step definition, items (places, [IItem.cs](/src/core/Flowthru.Core/Data/Catalog/IItem.cs)) declared by Catalog/Extension developers in the item factory. A step's effective conflict set unions in the resources of the items it loads and saves, so the EFCore/SQLite conflict reaches the scheduler through the item — one model, one resolution path, no second mechanism.
- **Capacity rides the existing archetype-specific trait records, not a new universal `NodeTraits` field.** Per the §1.6(a)/§2.10 decision that step traits do not inherit from `NodeTraits` ([StepTraits.cs](/src/core/Flowthru.Core/Step/StepTraits.cs)), capacity goes in `StorageTraits` ([StorageTraits.cs](/src/core/Flowthru.Core/Data/Storage/StorageTraits.cs)) for items — a sibling of `IsTransactional`, declared by the adapter, tightened by the catalog through the one-way `Constrain()` ratchet — and is derived from `ServiceDependencies` profiles for steps. The conflict **key** is the existing resource identity (`DbScope` for storage, `ServiceRef.DagId` for services); the trait supplies only the **capacity**.
- **Profiles are resolved, not stored on the ref.** Capacity is contextual — the same `ServiceRef.CSharp(T)` is serial as a singleton, parallel as transient — so an `IServiceProfileProvider` resolves `(ref, host registrations, declarations) → profile` at pre-flight, mirroring how `IServiceRefDispatcher` already resolves `External` refs. Resolution composes layered sources by conservative meet (most-restrictive wins) with explicit declarations able to pin a value.
- **Default ∞, reduce only where we know.** A node with no service deps is pure and parallel-safe. DI lifetime is the inference signal: singleton (shared instance) defaults to capacity 1, transient/scoped to ∞. An explicit declaration at service registration overrides. This keeps the conservative posture where sharing is real without serializing the world.

## Considered options

- **Extend the `ServiceRef` closed sum with more subtypes** (the path `ObservationOnly` took). Rejected: facts are orthogonal bits, so subtypes multiply combinatorially (`affectsOutputs × capacity × {CSharp,External} × future-Lambda`); a profile record is the scalable carrier.
- **Store the profile on the `ServiceRef`.** Rejected: capacity is contextual (depends on host registration / deployment), not knowable where the ref is constructed inside a step factory; it must be resolved.
- **Default-conservative (capacity 1 for any service dep), opt into parallel.** Rejected: it would serialize the whole flow on any shared singleton and forces every storage adapter to opt back into ∞, while file adapters are already protected from write conflicts by the single-producer law. Default-∞ with known reductions is the lower-churn, equally-safe posture.
- **A separate storage-conflict mechanism distinct from service deps.** Rejected once `INode` carries the dependency uniformly: the item-vs-step "two carriers" framing collapses into one carrier with the node archetype determining only *when* the resource is held (item: `Load`/`Save`; step: `Transform`).

## Consequences

Per-role impact:

- **Flow developer** — nearly unchanged. Step service-deps are declared as today (often source-generated from `Create()` parameters); conflicts from touched items are inherited automatically. The net effect is positive: `ConfigureExecution(Parallelism = N)` becomes safe to enable. A manual exclusion-key escape hatch may be added for hand-rolled steps.
- **Catalog developer** — additive and optional: tighten an item's concurrency through the existing `Constrain()` ratchet.
- **Extension developer** — the primary locus. Storage-adapter authors declare medium capacity in `StorageTraits` and expose the resource identity; service-providing extensions author the profile and surface the resource as a *declared* dependency rather than a closure capture.
- **Core developer** — the bulk: capacity field on `StorageTraits`; the `ServiceProfile` record; `INode`-level conflict-requirement resolution; resource gating in the `ParallelFlowScheduler` ready-set loop ([ParallelFlowScheduler.cs](/src/core/Flowthru.Core/Flow/ParallelFlowScheduler.cs)); the `IServiceProfileProvider` seam; pre-flight validation and DAG-metadata surfacing of conflict groups; migration of the cache planner's `ObservationOnly` subtype check to read the profile.

Extension rework (bounded by the default-∞ choice):

- **Required now** — Python (executor becomes a declared, capacity-1 profiled dependency instead of a closure capture) and EFCore (`DbScope` key plus provider-derived write capacity: SQLite → 1).
- **Likely** — Google Sheets (a spreadsheet is shared mutable state under an API quota).
- **Opt-in cap** — GQL, HTTP (network/rate-limited; ∞ is the default, a cap is opt-in).
- **None** — the file adapters Csv, Parquet, Excel, Xml (∞ holds; the single-producer law already prevents two steps writing one file — Excel multi-sheet-in-one-workbook is the lone edge case) and the Metadata.* report emitters.

The profile is the natural carrier for a third axis when the AWS Lambda harness ([ADR-0017](./0017-aws-lambda-harness.md)) needs to describe how a service behaves across an invocation boundary — same pattern, additional field.

## Anchor code

The decision extends these existing types (implementation pending):

- `src/core/Flowthru.Core/Data/Catalog/INode.cs` — the shared base where service-dependency/conflict is to be lifted from `IStepNode`
- `src/core/Flowthru.Core/Validation/Runtime/ServiceRef.cs` — identity carrier; `ObservationOnly` collapses into a resolved profile
- `src/core/Flowthru.Core/Data/Storage/StorageTraits.cs` — item-side capacity home, sibling of `IsTransactional`
- `src/core/Flowthru.Core/Flow/ParallelFlowScheduler.cs` — ready-set dispatch gains per-key resource gating
- `src/core/Flowthru.Core/Caching/CachePlanBuilder.cs` — `ObservationOnly` check migrates to read `profile.AffectsOutputs`
- `src/extensions/Flowthru.Extensions.Python/Step/Python/Internal/SubprocessPythonExecutor.cs` — capacity-1 profile today; raises to N when a worker pool lands
- `src/extensions/Flowthru.Extensions.EFCore/Data/Storage/EFCore/DbScope.cs` — conflict key for storage-resource contention
