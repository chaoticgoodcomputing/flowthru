# Phase 6 — Cache Manifest + Pre-Flight Cache-Plan Walk + Scheduler Branch

> **Created:** 2026-05-13
> **Status:** Pending
> **Depends on:** Phase 3 (`ISupportsFingerprint`), Phase 4 (`CodeVersion`).
> **Unblocks:** Phase 7 (CLI surface).

## Motivation

This is the load-bearing phase. With leaf fingerprints (Phase 3) and step code identities (Phase 4) in place, we can compose per-DAG-node fingerprints, build a cache plan at pre-flight, and have the scheduler short-circuit fresh nodes at runtime.

The end-user payoff: long-running steps (model training, large embedding passes, big data pulls) that run idempotently against unchanged inputs become no-ops on subsequent runs.

## Scope

**In scope:**
- A framework-managed `IItem<CacheManifest>` for cache state.
- `IFlowthruBuilder.UseCacheStorage(...)` hook for storage customization; default JSON at `.flowthru/cache.json`.
- Pre-flight cache-plan walk: generic DAG traversal over items and steps.
- Scheduler branch in [ParallelFlowScheduler](../../../src/core/Flowthru.Core/Scheduler/ParallelFlowScheduler.cs) to short-circuit fresh steps.
- Post-step framework hook to upsert manifest entries.
- Schema versioning with invalidate-on-mismatch.
- Concurrent-run merge semantics via per-entry `RecordedAt`.

**Out of scope:**
- The CLI surface (`--no-cache`, `flowthru cache invalidate`) — Phase 7.
- Migrations between manifest schema versions — deferred; v1 invalidates on bump.
- Distributed cache backends (S3, Redis) — the storage shape supports them, but adapters are future work.

## Design

### Cache item

```csharp
namespace Flowthru.Caching;

public sealed record CacheManifest(
    int SchemaVersion,
    IReadOnlyDictionary<string, NodeFingerprint> Entries);

public sealed record NodeFingerprint(
    string Value,
    DateTimeOffset RecordedAt);

internal static class CacheManifestSchema
{
    public const int CurrentVersion = 1;
}
```

The manifest is a single `IItem<CacheManifest>` with framework-managed writes. It's **not in the user-visible DAG** — registered separately on `FlowthruService` via `UseCacheStorage`. No user step can produce or consume it.

### Host wiring

```csharp
services.AddFlowthru(b =>
{
    // Default — if .UseCacheStorage() not called, equivalent to:
    b.UseCacheStorage(_ =>
        Item.Of<CacheManifest>("flowthru.cache")
            .Json()
            .AtPath(".flowthru/cache.json")
            .Build());

    // Override — point at a database, S3, etc.:
    // b.UseCacheStorage(sp => sp.GetRequiredService<MyEFCoreCatalog>().FlowthruCache);
});
```

### Fingerprint composition

For each step `S` in the merged flow:

```
composite_hash(S) = SHA256(
    S.CodeVersion ?? "<no-version>",
    for input I in S.Inputs sorted by Label:
        if I has a producer P in this flow:
            composite_hash(P)
        else:
            I.TryGetFingerprint() ?? "<no-fingerprint>"
)
```

If any contributor is `"<no-version>"` or `"<no-fingerprint>"`, `composite_hash(S)` is computed but the step is **marked uncacheable** in the plan regardless of any cache table state. This is the fail-safe path — we never serve potentially-stale data.

For each item `I`:

```
node_fingerprint(I) =
    if I.TryGetFingerprint() is not null:
        I.TryGetFingerprint().Value
    else:
        "<no-fingerprint>"
```

### The cache plan

A `CachePlan` is added to `FlowMetadataContext` ([FlowMetadataContext.cs](../../../src/core/Flowthru.Core/Diagnostics/FlowMetadataContext.cs)):

```csharp
public sealed record CachePlan(
    IReadOnlySet<string> FreshStepLabels,    // skip at runtime
    IReadOnlySet<string> StaleStepLabels,    // must run
    IReadOnlySet<string> UncacheableLabels,  // never cached; always run
    IReadOnlyDictionary<string, string> NewFingerprints);  // for upserts
```

### Pre-flight walk

In topological order over the effective (sliced) flow:

```
manifest = cacheStorageItem.Load() // graceful fallback to empty if file missing
if manifest.SchemaVersion != CurrentVersion:
    log("Cache schema bumped; invalidating all entries.")
    manifest = empty

for node in topo_order(effective_flow):
    if node is an item:
        if item has no producer in flow:  // external root
            fp = item.TryGetFingerprint()
            mark item fresh iff fp == manifest[item.label]
        else:  // produced internally
            mark item fresh iff its producer is marked fresh
    if node is a step:
        eligible = (every input is fresh)
                && (every input implements ISupportsFingerprint)
                && (step.ServiceDependencies is empty)
                && (step.CodeVersion is not null)
        if not eligible:
            mark step stale and uncacheable
            continue
        composite = compose(step)
        all_outputs_exist = step.Outputs.All(o => o.Exists())
        mark step fresh iff composite == manifest[step.label] AND all_outputs_exist
```

The walk terminates with a `CachePlan` describing every node's status.

### Scheduler branch

In [ParallelFlowScheduler.ExecuteAsync](../../../src/core/Flowthru.Core/Scheduler/ParallelFlowScheduler.cs#L114), before `ExecuteOneAsync` is dispatched:

```csharp
if (cachePlan.FreshStepLabels.Contains(step.Label))
{
    var result = new StepResult.Succeeded(
        step.Label,
        Duration: TimeSpan.Zero,
        Reason: "cached");
    resultsByIndex[idx] = result;
    DecrementDependents(idx);  // existing path
    continue;
}
```

For non-fresh steps, dispatch normally. On successful completion, a framework post-step hook upserts:

- The step's composite hash into manifest (key = step label).
- Each output item's fingerprint into manifest (key = item label).

The upsert is **per-entry**: existing entries for nodes outside the slice are never touched.

### Concurrency

Two `flowthru run` processes against the same project must not lose writes. The manifest's per-entry `RecordedAt` supports last-write-wins merge:

```
on save:
    current = manifest.Load()  // re-read at write time
    merged = current with merge(new_entries):
        for label, new_entry in new_entries:
            if label not in current or new_entry.RecordedAt > current[label].RecordedAt:
                current[label] = new_entry
    manifest.Save(merged)
```

Per-entry granularity avoids file-locking entirely. Slight risk: two concurrent runs producing different outputs for the same step result in the latest writer winning — a user bug, but documented.

### Schema versioning

`CacheManifest.SchemaVersion` is checked on load. Mismatch → manifest treated as empty, logged at INFO level once per run. No migration logic in v1. Bumping the constant on a release note is sufficient; users see a one-time full re-run.

## Tasks

1. **`src/core/Flowthru.Core/Caching/CacheManifest.cs`** — Record types as above. Includes serialization-friendly shape for JSON.

2. **`src/core/Flowthru.Core/Caching/CachePlan.cs`** — Pre-flight artifact passed to the scheduler.

3. **`src/core/Flowthru.Core/Hosting/IFlowthruBuilder.cs`** — Add `UseCacheStorage(Func<IServiceProvider, IItem<CacheManifest>> factory)` method.

4. **`src/core/Flowthru.Core/Hosting/FlowthruServiceBuilder.cs`** — Implement `UseCacheStorage`. Default to `.flowthru/cache.json` if not called.

5. **`src/core/Flowthru.Core/Caching/CachePlanBuilder.cs`** — New pre-flight stage: walks the effective flow in topo order, composes per-node fingerprints, decides fresh/stale/uncacheable. Returns `CachePlan`.

6. **`src/core/Flowthru.Core/Validation/PreFlight/PreFlightPipeline.cs`** — Add a new layer after fingerprint inspection that runs `CachePlanBuilder`. The plan is recorded in `FlowMetadataContext` for downstream use.

7. **`src/core/Flowthru.Core/Diagnostics/FlowMetadataContext.cs`** — Add `CachePlan? CachePlan { get; init; }`.

8. **`src/core/Flowthru.Core/Scheduler/ParallelFlowScheduler.cs`** — Add the cache-branch before `ExecuteOneAsync`. Emit `StepResult.Succeeded(reason: "cached")` for fresh steps.

9. **`src/core/Flowthru.Core/Flow/StepResult.cs`** — Add a `Reason` field on the `Succeeded` case (or extend the existing `Cached` variant if one exists; verify against current shape).

10. **`src/core/Flowthru.Core/Scheduler/ParallelFlowScheduler.cs`** — Implement post-step manifest upsert hook. Composes the step's new composite hash + each output item's new fingerprint; saves via merge.

11. **`src/core/Flowthru.Core/Hosting/FlowthruService.cs`** — Wire the cache item's load into pre-flight; ensure it's resolved through the same DI scope as the rest of the catalog.

12. **Tests:**
    - Cache plan: every fresh/stale/uncacheable case covered.
    - Cache hit short-circuits step execution (verify the transform was not invoked).
    - Cache miss runs the step and writes a new manifest entry.
    - Schema-version bump invalidates the manifest fully.
    - Concurrent-write merge: two simultaneous saves produce the union with last-writer-wins per entry.
    - Cascade rule: a stale step in the middle of a path forces all downstream steps stale, regardless of their individual cacheability.
    - Service-dependency rule: a step with a `ServiceRef` is never cached.
    - First-run cold cache: every step runs, manifest is populated.
    - Output-existence check: a step is stale if any of its outputs is missing, even if hashes match.

## Public Surface Changes

Additive:
- `Flowthru.Caching.CacheManifest` (record).
- `Flowthru.Caching.NodeFingerprint` (record).
- `Flowthru.Caching.CachePlan` (record).
- `IFlowthruBuilder.UseCacheStorage(...)` method.
- `StepResult.Succeeded.Reason` field (or `Cached` variant — TBD against current shape).

No breaking changes. Existing flows without `UseCacheStorage` get the default `.flowthru/cache.json`; existing items without `ISupportsFingerprint` are never cacheable, so behavior is identical to today.

## Phase Placement (per CONTRIBUTING.md)

- **Compile-time:** Eligibility constraints — service-dependency presence, `CodeVersion` nullability, capability-interface check — are static for each step. Where determinable at compile time, source-generated metadata flags it; otherwise the check moves to pre-flight.
- **Pre-flight:** Cache plan composition. All decisions are finalized here. `PreFlightError` raised if the manifest item is unreachable (e.g., disk permission denied).
- **Runtime:** Scheduler reads the plan and either dispatches or short-circuits. Post-step upsert runs as a `FlowIO` effect — failures captured in `StepResult` like any other I/O.

## Testing Strategy

- Per-rule unit tests in `tests/Flowthru.Core.Tests/Caching/`.
- Integration test: a worked flow runs twice; the second run reports every cacheable step as `Cached` with zero duration.
- Stress test: 100 concurrent `flowthru run` processes against a tiny shared project; verify no manifest entries are lost (per-entry merge holds).
- Snapshot test: the manifest's JSON format is stable across releases (or breaks deliberately via schema bump).

## Confirmation Criteria

- `nx run-many -t build` passes.
- `nx run affected -t test` passes; cache test suite has full coverage of the rules above.
- An end-to-end test of magic-atlas (downstream verification): running `FineTuneFlow` twice in a row — second run reports `FineTuneEmbeddingModel` as `Cached` and completes in seconds rather than the original training duration.
- The default `.flowthru/cache.json` is created on first run, populated correctly, and consumed on second run.
- Bumping `CacheManifestSchema.CurrentVersion` in a test produces the expected full-project re-run with a single INFO log line.

## Risks

- **Cache plan complexity bug:** the topological walk is non-trivial; an off-by-one in the cascade rule could produce false hits (data corruption-class bug). Mitigation: invariant test — for every `CachePlan`, if any input to step S is stale, S must be stale.
- **Fingerprint cost at pre-flight scale:** a project with 100 fingerprintable inputs to cacheable steps fingerprints all of them at every pre-flight. Mitigation: parallel `Fingerprint()` calls with a bounded degree (mirror the existing `MaxParallelInspections`).
- **Manifest growth unbounded:** entries for deleted steps remain. Mitigation: on every run, optionally prune entries whose labels aren't in the merged flow. Default off in v1 (cheap to defer; entries are tiny).
- **Catastrophic file corruption:** an in-progress write that crashes leaves an unparseable JSON. Mitigation: write to a temp file and atomic-rename. Already standard for `.flowthru/` artifacts.
- **The "first miss in the middle" surprise:** users observing "I cached step A and B, but I edit C, so D runs — why does B run too?" Answer: it doesn't, only if B is downstream of C. Mitigation: clearer documentation on cascade semantics. A diagnostic mode (`flowthru cache plan --select X`) helps debug.

## Follow-ups

- Phase 7 adds the CLI surface that consumes this phase's manifest.
- A future "cache plan visualization" extension to [Flowthru.Extensions.Metadata.Mermaid](../../../src/extensions/Flowthru.Extensions.Metadata.Mermaid) would color cached vs. stale nodes — defer until users ask.
- A "remote cache backend" RFC could let teams share a Postgres-backed manifest for CI-style state diffs (dbt's `--state` pattern) — defer.
