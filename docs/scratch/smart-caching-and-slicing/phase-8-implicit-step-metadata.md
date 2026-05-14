# Phase 8 — Implicit Step Metadata + Manifest Symmetry

> **Created:** 2026-05-14
> **Status:** Pending
> **Depends on:** Phases 1-7 (the caching feature must already exist for this phase to bury anything).

## Motivation

Three concrete observations after wiring caching into `FlowthruCoverage`:

1. **Every cacheable step author has to type the same boilerplate.**
   `codeVersion: <Step>_Metadata.CodeVersion` appears once per `AddStep`
   invocation. The framework already knows everything it needs — the
   step class is visible in the `transform:` argument, the source-gen
   companion sits right next to it — but the user is forced to thread
   the value by hand. This is the kind of "implementation detail
   leaking into authoring" the framework's design philosophy treats as
   a defect.

2. **There's no useful reason for step metadata to be opt-in.** Whether
   a step is *cached* is governed by its inputs (opt-in via
   `ISupportsFingerprint`) and its services (always disabling). The
   step's *identity* — its `CodeVersion` — is free metadata to capture
   regardless. The current design conflates "should we cache this
   step?" (a runtime question gated by inputs) with "do we know what
   identity to give this step?" (a build-time question gated by
   `[FlowthruStep]`).

3. **The cache manifest only stores step entries.** A user
   inspecting `.flowthru/cache.json` sees `step_label → composite_hash`
   but no corresponding `item_label → fingerprint` records. That
   diverges from the locked design ("one hash per DAG node — a
   fingerprint for an item, a fingerprint for a step — then, the
   underlying cache miss calculation is simply walking through nodes,
   generically, and looking for the highest-up cache misses"). The
   walk in `CachePlanBuilder` works correctly today because it
   recomputes item fingerprints in memory, but it diverges from a
   single generic node-walk and forecloses on diagnostics that want to
   reason about per-item state (e.g., "this Cobertura XML changed since
   the last run").

## Scope

**In scope:**
- Bury `codeVersion:` from the visible `FlowBuilder.AddStep` surface
  via a module-initializer-driven `StepMetadataRegistry` plus
  automatic discovery on the `transform:` delegate's enclosing type.
- Upgrade `FT1101` (missing `[FlowthruStep]`) from `Warning` to
  `Error` — every named-class transform must be attributed.
- Add item entries to `CacheManifest` so the persisted state mirrors
  the DAG-node design exactly: one entry per item, one per step.
- Bump `CacheManifestSchema.CurrentVersion` from `1` to `2`. Existing
  manifests are absorbed as empty (v1 semantics preserved).
- Strip every explicit `codeVersion:` line from the FlowthruCoverage
  example to demonstrate the bury end-to-end.

**Out of scope:**
- Item-level CachePlan exposure beyond what providers need. The
  `CachePlan` stays step-focused (the scheduler only short-circuits
  steps); item state is computed inside the walk and persisted in the
  manifest, but isn't surfaced as a separate set on `CachePlan` until
  there's a diagnostic that needs it.
- Cross-extension auto-discovery. Python and future step extensions
  resolve their own `CodeVersion` via their own `Add{Kind}Step`
  overloads — the C#-specific registry doesn't apply to them.
- `flowthru cache invalidate` CLI subcommand (still deferred per the
  Phase 7 RFC).

## Locked Design

### Implicit C# `CodeVersion` via module-initializer registry

`StepMetadataGenerator` already emits `{ClassName}_Metadata` companions
carrying `CodeVersion` constants. This phase adds a second emission:
a `[ModuleInitializer]`-attributed static registration call that
inserts the (`Type`, `CodeVersion`) pair into a process-wide
`StepMetadataRegistry`:

```csharp
// Source-generated alongside FlattenCoberturaStep_Metadata
internal static class FlattenCoberturaStep_Registration
{
    [ModuleInitializer]
    internal static void Register() =>
        Flowthru.Step.StepMetadataRegistry.Register(
            typeof(FlattenCoberturaStep),
            FlattenCoberturaStep_Metadata.CodeVersion);
}
```

At `FlowBuilder.AddStep` time, the framework walks the
`transform.Method.DeclaringType` upward through compiler-generated
`<>c__DisplayClass*` companions until it finds a type that's in the
registry. If found, the registered `CodeVersion` is threaded through
the `Step` constructor automatically. If not found (inline lambda,
unattributed class, foreign assembly), `CodeVersion` stays `null` and
the step is uncacheable — fail-safe.

### Hidden `codeVersion:` escape hatch

The auto-discovered overload is the only one Flow developers see.
A second overload, marked `[EditorBrowsable(EditorBrowsableState.Never)]`,
accepts an explicit `codeVersion:` argument for power-user override
(e.g., a flow author asserting "this refactor was cosmetic; don't bust
the cache" or "rev this even though the source is unchanged").

The hidden overload doesn't appear in IntelliSense. Users who know it
exists (because they read the cache section of the docs) can still
call `AddStep(..., codeVersion: "v2")` by name; overload resolution
picks the hidden form because only it has a `codeVersion` parameter.

### Manifest symmetry

`CacheManifest` gains a second dictionary alongside the existing one:

```csharp
public sealed record CacheManifest(
    int SchemaVersion,
    IReadOnlyDictionary<string, NodeFingerprint> Steps,
    IReadOnlyDictionary<string, NodeFingerprint> Items
) : IStructuredSerializable;
```

(The pre-Phase-8 `Entries` property becomes `Steps`; an `Items`
property is added. The schema-version bump from 1 to 2 covers the
shape change — existing v1 manifests are silently absorbed as empty.)

The pre-flight `CachePlanBuilder` walks the merged DAG once and
records both kinds of fingerprint. Items contribute leaf fingerprints
(via `ISupportsFingerprint`) if external; internally-produced items
inherit freshness from their producer step. Steps compose
`CodeVersion + sorted-input-fingerprints` into a composite hash, as
today.

The cascade rule is unchanged but expressed uniformly: an item is
stale iff its current fingerprint differs from the recorded value
(external) or its producer is stale (internal); a step is stale iff
any input item is stale OR its composite differs from the recorded
value. "Highest-up cache misses" falls out of the topological order.

### `[FlowthruStep]` enforcement

`FT1101` flips `defaultSeverity` from `Warning` to `Error`. The
analyzer's logic doesn't change — inline lambdas remain exempted; only
named classes used in a `transform:` argument are checked.

The forcing function: a flow author writing a step class must
attribute it, and that step automatically participates in the cache
plan with a real `CodeVersion`. The result is that "wrote a step
class" and "step has stable identity" become the same operation, with
no separate ceremony.

## Tasks

1. **`src/core/Flowthru.Core/Step/StepMetadataRegistry.cs`** — new
   static class. Thread-safe `ConcurrentDictionary<Type, string>`.
   Public `Register(Type, string)` and `TryGet(Type, out string)`.

2. **`src/core/Flowthru.Core/Step/StepMetadataResolver.cs`** — new
   internal static helper. `ResolveFromDelegate(Delegate transform)`
   walks `transform.Method.DeclaringType` up the nested-type chain,
   probing the registry at each level. Returns the first match's
   `CodeVersion` or null.

3. **`src/core/Flowthru.Core.SourceGenerators/Step/StepMetadataGenerator.cs`** —
   extend to emit the `[ModuleInitializer]`-attributed registration
   companion alongside the existing `_Metadata` constant.

4. **`src/core/Flowthru.Core.SourceGenerators/Flow/FlowBuilderGenerator.cs`** —
   restructure overload emission:
   - **Visible overloads** (no `codeVersion:` parameter): call
     `StepMetadataResolver.ResolveFromDelegate(transform)` and thread
     the result through the `Step` constructor.
   - **Hidden overloads** (`[EditorBrowsable(Never)]`, with
     `codeVersion:` as a required-positional or named-only parameter):
     pass the explicit value through verbatim.

5. **`src/core/Flowthru.Core/Caching/CacheManifest.cs`** — split
   `Entries` into `Steps` and `Items` dictionaries. Bump
   `CacheManifestSchema.CurrentVersion` to `2`. Update `Empty`.

6. **`src/core/Flowthru.Core/Caching/CachePlanBuilder.cs`** — uniform
   DAG-node walk. Read item fingerprints, compare to
   `manifest.Items`, propagate freshness through producers. Compose
   step composites and compare to `manifest.Steps`. Emit
   `NewItemFingerprints` and `NewStepFingerprints` on the plan
   (consumed by the post-run upsert).

7. **`src/core/Flowthru.Core/Caching/CacheManifestStore.cs`** —
   accept item fingerprints as a separate map; merge into
   `manifest.Items` alongside the existing step-level merge.

8. **`src/core/Flowthru.Core/Caching/CachePlan.cs`** — extend with
   `NewItemFingerprints : IReadOnlyDictionary<string, string>`.
   `FreshStepLabels`, `StaleStepLabels`, `UncacheableStepLabels` stay
   step-only since the scheduler only short-circuits steps.

9. **`src/core/Flowthru.Core/Hosting/FlowthruService.cs`** — post-run
   upsert path threads both kinds of new fingerprints to the store.

10. **`src/core/Flowthru.Core.SourceGenerators/Step/StepDiagnostics.cs`** —
    `FT1101.defaultSeverity` from `Warning` to `Error`. Update XML
    description to reflect the breaking-change rationale.

11. **`examples/advanced/FlowthruCoverage/Flows/CoverageAnalysis/CoverageFlow.cs`** —
    remove all `codeVersion: <Step>_Metadata.CodeVersion` lines.

12. **`examples/advanced/FlowthruCoverage/Flows/Reporting/ReportingFlow.cs`** —
    same.

13. **Tests:**
    - `StepMetadataRegistry`: register, lookup, idempotent re-register,
      missing returns null.
    - `StepMetadataResolver`: resolves from non-capturing lambda;
      resolves from capturing lambda (walks up `<>c__DisplayClass`);
      returns null for inline anonymous lambda; returns null for
      foreign-class delegate.
    - Source-gen snapshot: emitted companion includes
      `[ModuleInitializer]` registration.
    - `FlowBuilder.AddStep` visible overload threads the auto-resolved
      `CodeVersion` onto `IStepNode.CodeVersion`.
    - `FlowBuilder.AddStep` hidden overload still accepts explicit
      `codeVersion:` and overrides the auto-resolved value.
    - FT1101 fires as `Error` (not `Warning`) on an unattributed
      step class.
    - `CacheManifest` round-trips both `Steps` and `Items` dicts.
    - `CachePlanBuilder`: item-level fingerprint mismatch cascades to
      consumer steps; manifest mismatch on a step is independent.
    - End-to-end: a flow with both file-backed and HTTP-backed
      inputs records both in `manifest.Items`; second run is a hit;
      modifying any one input busts only the affected subtree.

## Public Surface Changes

Additive + cleanup:

| Surface | Form | Note |
|---|---|---|
| `Flowthru.Step.StepMetadataRegistry` | New static class | Public so extensions can register too (Python registers from its own loader) |
| Source-gen ModuleInitializer per `[FlowthruStep]` class | New emission | Adds one method per step class; tree-shaken alongside its metadata |
| `FlowBuilder.AddStep<...>(...)` (no `codeVersion:`) | New visible overload set | Replaces the current visible overloads |
| `FlowBuilder.AddStep<...>(..., codeVersion:)` | Hidden overload set | `[EditorBrowsable(Never)]` |
| `CacheManifest.Steps`, `CacheManifest.Items` | New properties | Replaces `Entries` |
| `CacheManifestSchema.CurrentVersion` | Bumped 1 → 2 | Existing manifests invalidated automatically |
| `CachePlan.NewItemFingerprints` | New property | Consumed by post-run upsert |
| `FT1101` severity | `Warning` → `Error` | Build-breaking for unattributed step classes |

Breaking changes:
- `CacheManifest.Entries` is removed in favor of `Steps`/`Items`.
  Existing on-disk manifests are silently re-recorded via schema-bump
  semantics, so no user action is needed.
- FT1101 errors are now build-breaking. Any flow author with an
  unattributed step class must add `[FlowthruStep]`. The fix is the
  obvious one and the analyzer's diagnostic already says what to do.

## Phase Placement (per CONTRIBUTING.md)

| Concern | Phase | Notes |
|---|---|---|
| `[FlowthruStep]` requirement | Build-time (FT1101 Error) | Analyzer fires before pre-flight runs |
| `CodeVersion` capture | Build-time | Source-gen + module initializer |
| `CodeVersion` lookup in `AddStep` | Build-time fold (registry is process-static, populated by module load) | Walk is O(nested-type-depth), one-time per AddStep |
| Manifest read | Pre-flight | Unchanged |
| Cache plan walk | Pre-flight | Uniform across node kinds |
| Cache plan write | Runtime | Unchanged |

## Testing Strategy

Unit tests in `tests/Flowthru.Core.Tests/Step/` and
`tests/Flowthru.Core.Tests/Caching/` per the task list. Source-gen
snapshot tests in `tests/Flowthru.Core.SourceGenerators.Tests/`.
FT1101 regression in the existing `Ft1101*Tests` file (verifies
severity is `Error`).

Smoke test: FlowthruCoverage. Run twice — the first run populates a
v2 manifest with both `Steps` and `Items` populated; the second is a
cache hit. Modify a single Cobertura XML — only the downstream
subtree busts; sibling XMLs remain fresh.

## Confirmation Criteria

- `nx run-many -t build` passes solution-wide.
- `nx run affected -t test` passes; new tests cover every task above.
- FlowthruCoverage CLI invocations succeed with no explicit
  `codeVersion:` lines in either flow.
- `.flowthru/cache.json` after a fresh run shows both `Steps` and
  `Items` populated. Format is JSON-readable for debugging.
- Architecture tests pass (the new `StepMetadataRegistry` lives in
  `Flowthru.Step` namespace per the namespace-layout rule for
  step-related types).

## Risks

- **AOT / trim compatibility.** Module initializers and reflection
  on delegate `Method.DeclaringType` interact awkwardly with the
  .NET AOT pipeline. Mitigation: the generated registration is
  reachable from the step class's own module initializer, so AOT
  rooting via the type's `[DynamicDependency]` keeps the registry
  population intact. Document the requirement; add a single AOT
  smoke test (deferred until AOT is a project-level goal).

- **Lambda lowering shape changes.** Future Roslyn versions may emit
  capturing lambdas differently — e.g., a new nested-type naming
  convention. Mitigation: the resolver walks up `DeclaringType`
  recursively rather than pattern-matching the name; any sensible
  lowering keeps the step class at some ancestor depth.

- **Foreign-assembly step classes.** A step class defined in a
  referenced library that doesn't run our source generator won't be
  in the registry. The flow author can register manually
  (`StepMetadataRegistry.Register(typeof(LibStep), "v1")`) or accept
  the uncacheable verdict. Documented behavior.

- **FT1101-as-error breaks existing projects.** Any in-flight Flowthru
  project that ignored the warning now fails to build. Mitigation:
  the fix is mechanical (add `[FlowthruStep]`); the analyzer's message
  is already actionable; release notes call it out.

- **Items-in-manifest size growth.** Every external input adds one
  entry per run. For a flow with hundreds of inputs the manifest
  grows but each entry is ~100 bytes — even 1000 items is 100 KB
  JSON, well within file-load budget.

## Follow-ups

- A diagnostic provider that renders cache state (per-item + per-step
  with timestamps) for `flowthru` CLI output. Deferred until users
  ask.
- The `cache invalidate` subcommand (still on the original phase plan).
