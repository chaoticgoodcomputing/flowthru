# Phase 3 — `ISupportsFingerprint` + Leaf Adapter Implementations

> **Created:** 2026-05-13
> **Status:** Pending
> **Depends on:** Phase 1 (for HTTP-backed items to be first-class fingerprint contributors).
> **Unblocks:** Phase 6 (the cache plan needs leaf fingerprints to compose).

## Motivation

For the cache plan to decide "this step's inputs are unchanged, skip it," each input item must expose a stable, cheap-to-compute identity. The natural place is the **storage adapter** — it already knows the medium's identity (file path, HTTP URL, DB table) and can derive a metadata-level fingerprint without loading the data.

Per the locked design, the catalog item *declares its cache participation* by implementing this capability. Presence of the interface is the opt-in; absence means the consuming step is uncacheable.

## Scope

**In scope:**
- New capability interface `ISupportsFingerprint` in Core.
- Leaf implementations for: File-backed adapters, HTTP-backed adapter (lifts existing `CachedHttpStorageMedium` metadata), EFCore adapter, Parquet adapter.
- Adapter audit + diagnostics: any catalog item used as input to a step in the merged flow that implements this capability is fingerprintable.
- Test coverage per adapter.

**Out of scope:**
- The cache plan composition itself (Phase 6).
- Step `CodeVersion` (Phase 4).
- Cache manifest storage (Phase 6).
- In-memory adapters — deliberately do not implement this; they have no stable identity across runs.

## Design

### The capability

```csharp
namespace Flowthru.Data.Storage;

/// <summary>
/// Optional capability — an adapter implementing this interface
/// declares that the item it backs participates in Flowthru's
/// cache plan. The returned fingerprint must be:
///
/// 1. <b>Stable</b> — repeated calls without intervening state change
///    return the same value.
/// 2. <b>Sensitive</b> — any change to the medium's content
///    (or to anything observable through a Load() call) changes the
///    fingerprint.
/// 3. <b>Cheap</b> — derivable without loading the data. Storage
///    metadata (mtime, size, ETag, MAX(updated_at)) is appropriate;
///    streaming the full content is not.
///
/// Implementations should NOT throw on transient errors — return a
/// FlowIO failure so the cache plan can record "fingerprint unknown"
/// and treat the dependent step as a cache miss without aborting
/// pre-flight.
/// </summary>
public interface ISupportsFingerprint
{
    FlowIO<string> Fingerprint();
}
```

The interface lives at the storage-adapter layer (alongside `IHasEfficientCount`), not at the `IItem<T>` layer. The framework discovers the capability by:

```csharp
internal static FlowIO<string>? TryFingerprint(IItem item) =>
    (item is { } and Item<object> wrapper && wrapper.Storage is ISupportsFingerprint fp)
        ? fp.Fingerprint()
        : null;
```

(Exact reflection details TBD — the storage adapter is private to the item; we may need an `IItem.TryGetFingerprint()` shim that delegates. See task list.)

### Fingerprint composition rules (per leaf type)

| Adapter | Fingerprint Source | Stability Note |
|---|---|---|
| File-backed (JSON, CSV, Parquet, Text, Binary) | `SHA256(path + ":" + last-write-time-utc-ticks + ":" + length)` | mtime+size is fast and catches most edits. Doesn't catch in-place byte edits that preserve size+mtime — rare, acceptable. |
| HTTP-backed | `SHA256(ETag ?? Last-Modified ?? "")` from `CachedHttpStorageMedium` metadata | Already recorded in `.meta.json` ([CachedHttpStorageMedium.cs:209-215](../../../src/extensions/Flowthru.Extensions.Http/Data/Storage/Http/CachedHttpStorageMedium.cs#L209-L215)). Servers without ETag/Last-Modified produce empty fingerprint → caller treats as uncacheable. |
| EFCore enumerable | `SHA256(count + ":" + max-updated-at)` via small framework-emitted query | Requires the table to have a timestamp column. Adapter exposes `WithFingerprintColumn(t => t.UpdatedAt)` configurator. Absent column → adapter does not implement the interface (uncacheable). |
| Parquet file | Same as File (mtime+size) | Parquet footer hash is more precise but slower; defer until we see real demand. |
| Directory-of-files | `SHA256(SHA256-per-file)` | Adapter walks the directory, hashes each child, composes. |
| In-memory | Not implemented | In-memory items have no cross-run identity. |

### Catalog-item surfacing

The IItem<T> interface gains a non-breaking accessor:

```csharp
namespace Flowthru.Data.Catalog;

public interface IItem
{
    // ... existing surface

    /// <summary>
    /// Returns a fingerprint if this item's storage adapter implements
    /// <see cref="ISupportsFingerprint"/>; otherwise null. The framework
    /// uses this in pre-flight cache planning.
    /// </summary>
    FlowIO<string>? TryGetFingerprint();
}
```

`Item<T>` ([Item.cs](../../../src/core/Flowthru.Core/Data/Catalog/Item.cs)) implements this by delegating to `_storage as ISupportsFingerprint`.

## Tasks

1. **`src/core/Flowthru.Core/Data/Storage/ISupportsFingerprint.cs`** — New file, interface declaration as above.

2. **`src/core/Flowthru.Core/Data/Catalog/IItem.cs`** — Add `TryGetFingerprint()` method to base interface, with default body returning `null` (so existing adapters continue to compile).

3. **`src/core/Flowthru.Core/Data/Catalog/Item.cs`** — Implement `TryGetFingerprint()` by delegating to `_storage as ISupportsFingerprint`.

4. **`src/core/Flowthru.Core/Data/Storage/FileStorageMedium.cs`** — Implement `ISupportsFingerprint`: `FlowIO.From(() => $"{File.GetLastWriteTimeUtc(_path):O}:{new FileInfo(_path).Length}")`.

5. **`src/core/Flowthru.Core/Data/Storage/ComposedStorageAdapter.cs`** — Implement `ISupportsFingerprint` by delegating to the underlying medium if it supports it.

6. **`src/extensions/Flowthru.Extensions.Http/Data/Storage/Http/CachedHttpStorageMedium.cs`** — Implement `ISupportsFingerprint`. On cache hit (304 path), return `SHA256(ETag ?? LastModified ?? "")`. On miss, perform a HEAD or conditional GET and return the resulting validator hash.

7. **`src/extensions/Flowthru.Extensions.Http/Data/Storage/Http/HttpStorageMedium.cs`** (uncached) — Implement with a HEAD request returning ETag/Last-Modified. If the server provides neither, return failure (FlowIO failure surfaces as "fingerprint unknown").

8. **`src/extensions/Flowthru.Extensions.EFCore/Data/Storage/EFCoreStorageAdapter.cs`** — Add `WithFingerprintColumn(Expression<Func<T, DateTime>> column)` configurator. Implement `ISupportsFingerprint` only when the column is set. Fingerprint emits a `SELECT COUNT(*), MAX(<column>) FROM <table>` query.

9. **`src/extensions/Flowthru.Extensions.Parquet/`** — File-backed parity (mtime+size for v1).

10. **`src/core/Flowthru.Core/Data/Storage/DirectoryStorageAdapter.cs`** — Compose per-file fingerprints into a directory fingerprint.

11. **Tests:**
    - Per-adapter unit tests covering: stable repeat calls, sensitivity to content/metadata change, behavior when fingerprint source is unavailable.
    - Cross-adapter conformance test: every adapter that claims to support fingerprinting passes a shared test suite (similar pattern to extension conformance kits in [docs/scratch/archive/extension-conformance-kits.md](../archive/extension-conformance-kits.md) if applicable).

## Public Surface Changes

Additive:
- `ISupportsFingerprint` interface (new).
- `IItem.TryGetFingerprint()` method (default implementation returns null — non-breaking).
- `EFCoreStorageAdapter.WithFingerprintColumn(...)` configurator (new opt-in surface).

No breaking changes.

## Phase Placement (per CONTRIBUTING.md)

- **Compile-time:** Capability interface check (`item.TryGetFingerprint() is not null`) for a step's cacheability is a runtime concern, not compile-time — adapters are pluggable. However, an analyzer could warn when a known-uncacheable adapter is wired into a step that's used by a cacheable downstream step. Defer that to Phase 6.
- **Pre-flight:** Fingerprint reads happen here. Failures surface as `PreFlightError.InspectionFailed` for the item, scoped to "fingerprint unavailable — dependent step uncacheable."
- **Runtime:** No participation. Fingerprints are read pre-flight and recorded post-step.

## Testing Strategy

- Per-adapter conformance suite: shared parametrized tests validate stability and sensitivity for every implementer.
- HTTP tests use a `TestServer` that controls ETag/Last-Modified responses.
- EFCore tests use the existing test-DB pattern; verify the fingerprint query is emitted only once per pre-flight even if the item appears as input to multiple cacheable steps.
- File tests verify the mtime+size edge case (preserved mtime+size on in-place edit → false hit) is documented, not silently incorrect.

## Confirmation Criteria

- `nx run-many -t build` passes.
- `nx run affected -t test` passes; conformance suite passes for every listed adapter.
- A minimal hand-test pipeline with one File-backed input and one Step verifies the fingerprint changes when the file is touched and stays stable otherwise.
- HTTP-backed items return the same fingerprint across runs against an unchanged server response (verified against magic-atlas's Scryfall endpoint as a downstream sanity check, not a CI dependency).

## Risks

- **Mtime+size collisions:** in-place byte edits preserving file metadata produce a false hit. Mitigation: documented limitation. Users who need exact content guarantees can wrap the adapter in a content-hash variant later.
- **HTTP servers without validators:** some endpoints don't send ETag or Last-Modified. Mitigation: fingerprint returns failure → item is uncacheable → consuming step is uncacheable. Documented in the HTTP extension's adapter docs.
- **EFCore tables without timestamp columns:** common in legacy schemas. Mitigation: explicit `WithFingerprintColumn` opt-in — silence is "don't cache."
- **Fingerprint cost in pre-flight:** every cacheable-step input fingerprints at pre-flight. Per Phase 6's plan, this is bounded to inputs of cacheable steps in the slice; users who don't opt into caching pay no cost.

## Follow-ups

- Phase 6 composes leaf fingerprints into per-step composite hashes.
- A future "deep fingerprint" variant could hash file contents; defer until a real false-hit incident demands it.
