# Phase 1 — Storage-Medium-Resolver Composition

> **Created:** 2026-05-13
> **Status:** Pending
> **Depends on:** —
> **Unblocks:** Phase 3 (HTTP leaf fingerprinting); fixes magic-atlas's biggest pain point independently of the cache work.

## Motivation

Today the framework has every piece needed for HTTP-backed catalog items to compose with format builders — except the wiring that connects them implicitly. The result is that magic-atlas's [FetchOracleCardsBulkNode.cs:16-19](../../../docs/reference/misc/external/magic-atlas/repo/libs/atlas-flows/Flows/Ingest/Nodes/FetchOracleCardsBulkNode.cs#L16-L19) calls out the gap by name:

> "Flowthru's `UseHttp` cache covers HTTP-backed catalog items, not in-step `HttpClient` calls — adding caching here would mean either reaching through `IStorageMediumResolver` or rolling our own keyed file cache."

The downstream then ships a 158-line [FilesystemHttpCacheHandler](../../../docs/reference/misc/external/magic-atlas/repo/tests/atlas-flow-test/FilesystemHttpCacheHandler.cs) that re-implements (worse) what [CachedHttpStorageMedium](../../../src/extensions/Flowthru.Extensions.Http/Data/Storage/Http/CachedHttpStorageMedium.cs) already provides — no conditional GETs, no ETag/Last-Modified, full 165 MB re-downloads after 24 hours.

## Current State

- [IStorageMediumResolver](../../../src/core/Flowthru.Core/Data/Storage/IStorageMediumResolver.cs) is registered in DI by [ServiceCollectionExtensions.cs:46](../../../src/core/Flowthru.Core/Hosting/ServiceCollectionExtensions.cs#L46), aggregating `IStorageMediumProvider` registrations from extensions (`UseHttp()`, future `UseS3()`, etc.).
- [JsonArrayBuilder](../../../src/core/Flowthru.Core/Data/Catalog/JsonExtensions.cs#L85-L150) and [CsvBuilder](../../../src/extensions/Flowthru.Extensions.Csv/Data/Catalog/CsvExtensions.cs) accept `.WithResolver(resolver)` but **fall back to `StorageMediumResolver.Filesystem`** when not called, which only handles `file://`.
- [JsonSingletonBuilder](../../../src/core/Flowthru.Core/Data/Catalog/JsonExtensions.cs#L42-L82) doesn't accept a resolver at all — `AtPath()` always materializes a `SingletonJsonAdapter<T>` directly against the literal path.
- Catalog items are materialized inside `CatalogAbstract.CreateItem<T>(factory, propertyName)` ([CatalogAbstract.cs:46-56](../../../src/core/Flowthru.Core/Data/Catalog/CatalogAbstract.cs#L46-L56)) — the factory closure has no access to the host's `IServiceProvider`, so it can't pull the DI-registered resolver.

The gap is structural: there's no path from "user calls `UseHttp()`" to "every catalog item built afterward automatically uses the HTTP-aware resolver."

## Scope

**In scope:**
- Plumb the DI-registered `IStorageMediumResolver` through to catalog-item materialization implicitly, so users don't manually call `.WithResolver(...)` per item.
- Bring `JsonSingletonBuilder<T>` to parity with `JsonArrayBuilder<TRow>` (and audit Parquet, Excel, XML, Text for the same gap).
- Verify magic-atlas's three Ingest nodes can be expressed as HTTP-backed catalog items with the existing `Item.Of<T>().Json().AtPath("https://…")` syntax.

**Out of scope:**
- The Cached `HttpStorageMedium` itself — already complete and well-tested.
- magic-atlas refactor — handled as a downstream demonstration, not as part of this phase.
- New media types beyond what extensions already provide.

## Design

### Implicit resolver propagation

The catalog factory closure needs access to the DI-registered resolver. Two viable shapes:

**(a) Pass `IServiceProvider` through `CreateItem<T>`.** The factory closure becomes `Func<IServiceProvider, IItem<T>>`. Pro: most explicit. Con: every catalog property declaration mentions DI.

**(b) Set an ambient resolver during catalog materialization.** `CatalogAbstract` reads the resolver once from its DI context at construction time and stores it; builders read from an `AsyncLocal<IStorageMediumResolver>` slot that `CatalogAbstract.CreateItem<T>` sets before invoking the factory. Pro: invisible to the user — `Item.Of<T>().Json().AtPath("https://…").Build()` works without further ceremony. Con: implicit ambient state.

**Recommendation: (b).** The fail-fast version: if a builder resolves a URI with a non-`file://` scheme and the ambient resolver is `StorageMediumResolver.Filesystem`, it throws with a diagnostic that names the scheme and the relevant `UseXxx()` registration. This is the same diagnostic [StorageMediumResolver.Resolve](../../../src/core/Flowthru.Core/Data/Storage/StorageMediumResolver.cs#L70) already emits — we just make the wiring automatic.

### Builder parity

Every format builder that currently materializes an adapter from a literal path must:
1. Accept an `IStorageMediumResolver` (optional `.WithResolver`).
2. Default to the ambient resolver (set by `CatalogAbstract.CreateItem<T>`), falling back to `StorageMediumResolver.Filesystem`.
3. Materialize the adapter through a `ComposedStorageAdapter` (`medium` + `serializer` + `container`) rather than a direct `SingletonJsonAdapter`-style construction.

The pattern is already in place for `JsonArrayBuilder` ([JsonExtensions.cs:131-139](../../../src/core/Flowthru.Core/Data/Catalog/JsonExtensions.cs#L131-L139)); singleton-shape builders need to be brought to match.

## Tasks

1. **`src/core/Flowthru.Core/Data/Storage/StorageMediumResolver.cs`** — Add a public `Current` accessor backed by an `AsyncLocal<IStorageMediumResolver?>` slot, plus an `internal` `Push/Pop` scope helper used by `CatalogAbstract`.

2. **`src/core/Flowthru.Core/Data/Catalog/CatalogAbstract.cs`** — Inject `IServiceProvider` via constructor (or its existing equivalent). Capture the DI-resolved `IStorageMediumResolver`. Wrap factory invocation inside `CreateItem<T>` with a `StorageMediumResolver.PushAmbient(_resolver)` using-scope.

3. **`src/core/Flowthru.Core/Data/Catalog/JsonExtensions.cs`** — Bring `JsonSingletonBuilder<T>` to parity:
   - Add `_resolver` field and `WithResolver` method.
   - Reshape `Build()` to use `(_resolver ?? StorageMediumResolver.Current ?? StorageMediumResolver.Filesystem).Resolve(_path)` → `ComposedStorageAdapter` with a singleton-shape container adapter.
   - Audit and apply the same pattern in `JsonArrayBuilder` so the ambient fallback works (currently only honors the explicit `.WithResolver(...)`).

4. **`src/extensions/Flowthru.Extensions.Csv/Data/Catalog/CsvExtensions.cs`** — Same ambient-fallback adjustment as `JsonArrayBuilder` above.

5. **`src/extensions/Flowthru.Extensions.Parquet/`** + **`src/extensions/Flowthru.Extensions.Excel/`** + **`src/extensions/Flowthru.Extensions.Xml/`** — Audit each format-builder for parity. Apply the same pattern.

6. **`src/core/Flowthru.Core/Hosting/FlowthruService.cs`** — Confirm catalog construction occurs inside a scope where `CatalogAbstract` can resolve `IStorageMediumResolver`. May require a small wiring adjustment in `MergedFlow` construction.

7. **Tests:**
   - Unit test: builder `.AtPath("https://example.com/data.json")` without explicit `.WithResolver` resolves to an HTTP-backed adapter when `UseHttp()` is registered.
   - Unit test: builder `.AtPath("https://…")` without `UseHttp()` registered throws with the expected diagnostic naming the scheme.
   - Integration test (slow): a small flow with an HTTP-backed JSON item executes end-to-end against a test HTTP server.

## Public Surface Changes

None breaking. New surfaces:
- `StorageMediumResolver.Current` (read-only ambient accessor).
- `JsonSingletonBuilder<T>.WithResolver(...)` (parity addition).

No type-signature changes to existing extension or item-builder APIs.

## Phase Placement (per CONTRIBUTING.md)

- **Compile-time:** Builder `.AtPath(...)` type-checks the path argument; the resolver dispatch is a runtime concern (we don't know the scheme until materialization).
- **Pre-flight:** `IItem.Exists()` on an HTTP-backed item issues a HEAD/conditional-GET probe — already implemented in [HttpStorageMedium](../../../src/extensions/Flowthru.Extensions.Http/Data/Storage/Http/HttpStorageMedium.cs). Schema drift surfaces here.
- **Runtime:** `IItem.Load()` / `Save()` follow existing `FlowIO` semantics. No change.

## Testing Strategy

Per [tests/README.md](../../../tests/README.md):
- New unit tests in `tests/Flowthru.Core.Tests/Data/Catalog/JsonExtensionsTests.cs` (singleton parity + ambient resolver pickup).
- New unit tests in `tests/Flowthru.Extensions.Http.Tests/` for the implicit-resolver path.
- Add a slow-tagged integration test that pulls a small HTTP resource via a `TestServer`.

## Confirmation Criteria

- `nx run-many -t build` passes with no errors.
- `nx run affected -t test` passes; coverage on `JsonSingletonBuilder` reaches the project's standard threshold.
- A demonstrator catalog declares `Item.Of<MyType>("x").Json().AtPath("https://example.com/x.json").Build()` and successfully loads in an integration test, without a manual `.WithResolver(...)` call.
- magic-atlas can express its three Ingest fetch nodes as HTTP-backed catalog items (verified as a follow-up downstream change — not part of this phase's confirmation, but a documented expected outcome).

## Risks

- **Ambient state via `AsyncLocal`** is non-obvious. Mitigation: document the scope clearly on `CatalogAbstract.CreateItem` XML doc; throw a diagnostic if a builder reaches `Build()` outside any scope.
- **Format builders not yet audited** (Parquet, Excel, XML) may have idiosyncrasies that don't fit the singleton/array pattern. Mitigation: each extension is a separate task above; audit one at a time.

## Follow-ups (separate phases / docs)

- Phase 3 lifts the HTTP medium's `ETag`-recording behavior into `ISupportsFingerprint` so HTTP catalog items contribute leaf fingerprints to the cache plan.
- magic-atlas migration to HTTP-backed catalog items — track as a downstream task once this phase ships.
