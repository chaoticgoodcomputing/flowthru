# Smart Caching and Slicing — Phase Plan

> **Created:** 2026-05-13
> **Status:** Drafted, awaiting implementation kickoff
> **Motivation:** Three QoL gaps surfaced while building [magic-atlas](../../../docs/reference/misc/external/magic-atlas/repo) on Flowthru:
>
> 1. **Heavy steps re-run unnecessarily.** Model training, large embedding passes, and clustering re-execute on every `flowthru run` even when inputs are unchanged.
> 2. **HTTP caching is reinvented downstream.** magic-atlas ships a `FilesystemHttpCacheHandler` because format serializers don't compose with `IStorageMediumResolver` — re-implementing what [Flowthru.Extensions.Http](../../../src/extensions/Flowthru.Extensions.Http) already provides, and worse (no conditional GETs).
> 3. **Slicing can't express exclusions.** Today users can say "run to step X" but not "…and exclude flow Y" — pushing them to multiple separate invocations or hand-edited entry points.
>
> **Design conversations:** See conversation history culminating in the locked design captured below.
>
> **Tests:** Use the VSCode `runTests` tool to execute tests; `nx run affected -t test` from the CLI.

---

## Core Promise Alignment

Per [CONTRIBUTING.md](../../../CONTRIBUTING.md), each piece of caching machinery lands at the earliest possible error phase:

| Concern | Phase | Notes |
|---|---|---|
| Selector parse + validate | Build-time / Pre-flight | Grammar-free flag composition for v1; parse-then-validate before any step runs |
| Step `CodeVersion` derivation | Build-time | Roslyn source generator emits a constant; author writes nothing |
| Item leaf fingerprint contract | Build-time | `ISupportsFingerprint` is a compile-time capability on adapters |
| Fingerprint reads + cache plan composition | Pre-flight | Cache plan is generated before any step's transform runs |
| Cache hit/miss decision | Pre-flight | Plan is recorded in `FlowMetadataContext` for the scheduler |
| Cache read (skip step) | Runtime | `FlowIO` — single branch in [ParallelFlowScheduler](../../../src/core/Flowthru.Core/Scheduler/ParallelFlowScheduler.cs) |
| Cache write (update manifest) | Runtime | `FlowIO` — framework-side post-step hook |
| Cache invalidation (CLI) | Build-time + Pre-flight | Selector parses + validates before any manifest mutation |

**Design invariant preserved:** A flow that passes pre-flight will complete. Cache decisions are pre-flight artifacts; the scheduler executes them without re-deciding.

---

## Locked Design Summary

A **catalog item** declares cache participation by implementing `ISupportsFingerprint`. A **step** is opaquely fingerprinted by the framework via a `CodeVersion` emitted by the source generator (or by the step extension for non-C# steps). `AddStep`, transform functions, and `NodeTraits` know nothing about caching.

Per-DAG-node fingerprinting:
- **Item nodes** fingerprint themselves through their adapter (file mtime+size, HTTP ETag/Last-Modified, DB `MAX(updated_at)`, parquet footer).
- **Step nodes** carry a `CodeVersion` stamped at build (C#) or startup (Python).

At pre-flight, Flowthru walks the effective (sliced) DAG in topological order:
- An **item** is *fresh* iff its current fingerprint matches the recorded value (or its producer step is fresh).
- A **step** is *fresh* iff every input is fresh, its `CodeVersion` matches the recorded value, every output `Exists()`, and it has no `ServiceDependencies` and only fingerprintable inputs.

Steps marked fresh are skipped at runtime as `StepResult.Succeeded(reason: Cached)`. Steps that run successfully upsert their node fingerprints into the cache manifest.

The cache manifest is a framework-managed `IItem<CacheManifest>` — default JSON at `.flowthru/cache.json`, override to SQLite/PG/etc. via the standard item-builder syntax.

---

## Phase Plan

| # | Phase | Status | Document | Depends On |
|---|---|---|---|---|
| 1 | Storage-medium-resolver composition | Pending | [phase-1-storage-medium-resolver.md](phase-1-storage-medium-resolver.md) | — |
| 2 | `FlowSliceStrategy.Not` + `--exclude` flag | Pending | [phase-2-slice-algebra-not.md](phase-2-slice-algebra-not.md) | — |
| 3 | `ISupportsFingerprint` + leaf adapter implementations | Pending | [phase-3-supports-fingerprint.md](phase-3-supports-fingerprint.md) | Phase 1 (HTTP) |
| 4 | `CodeVersion` via `StepMetadataGenerator` + Python extension | Pending | [phase-4-code-version.md](phase-4-code-version.md) | — |
| 5 | Reintroduce config-as-catalog | Pending | [phase-5-config-as-catalog.md](phase-5-config-as-catalog.md) | — |
| 6 | Cache manifest + pre-flight cache-plan walk + scheduler branch | Pending | [phase-6-cache-manifest.md](phase-6-cache-manifest.md) | Phases 3, 4 |
| 7 | CLI cache surface (`--no-cache`, `flowthru cache invalidate`) | Pending | [phase-7-cli-cache-surface.md](phase-7-cli-cache-surface.md) | Phases 2, 6 |

Phases 1, 2, 4, and 5 can land in any order. Phase 3 needs Phase 1 to make HTTP-backed items first-class. Phase 6 needs Phases 3 and 4 (leaf and step fingerprints). Phase 7 needs Phases 2 and 6 (slice algebra and cache plan).

Each phase ships user-visible value on its own — Phase 1 fixes magic-atlas's biggest pain point even before the cache work lands, Phase 2 ships `--exclude` independently, etc.

---

## Out of Scope (deferred, but kept on the roadmap)

- **Selector grammar parser** (Dagster/dbt-style expression DSL). Current flag system suffices through this plan; revisit when CI scripts start hitting the limits.
- **CLI source-gen from `FlowthruService`.** Closes the parity gap structurally via a `[CliOption]` attribute precedent; defer until the next missing flag surfaces, then do both at once.
- **Cache manifest migrations.** v1 stance is "schema-version bump → full-project miss with a one-line notice." Build migrations when there's a real cost case.
- **`flowthru cache purge` (output deletion).** Out of scope; users handle output deletion outside Flowthru. Manifest invalidation is sufficient — re-runs follow each item's existing save behavior (drop+write, upsert, etc.).

---

## Friction Resolution Map

| ID | Friction | Resolved By |
|---|---|---|
| C1 | magic-atlas's `FilesystemHttpCacheHandler` re-implements what Flowthru.Extensions.Http already provides | Phase 1 |
| C2 | Long-running training/embedding steps re-execute on every run | Phases 3 + 4 + 6 |
| C3 | HTTP responses re-downloaded fully after 24h with no conditional-GET | Phase 1 (uses extension's ETag/Last-Modified path) |
| C4 | Cannot say "run to X but exclude flow Y" without multiple invocations | Phase 2 |
| C5 | No CLI surface for invalidating specific cached outputs | Phase 7 |
| C6 | Lost config-as-catalog primitive blocks treating `IConfiguration` as fingerprintable input | Phase 5 |
| C7 | Step authors can't reason about cache invalidation when code changes | Phase 4 |
