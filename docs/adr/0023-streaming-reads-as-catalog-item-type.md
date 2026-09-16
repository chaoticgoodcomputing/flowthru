---
status: proposed
---

# Streaming is a `FlowSource<T>` catalog payload — a minimal, vendored Prelude primitive consumed by compiling back into `FlowIO`

A Parquet read carries two O(file) memory costs: the medium buffers the whole S3 object into a `MemoryStream` (the [ADR-0020](./0020-s3-storage-medium-via-gateway-seam.md) / #105 seek fix), and the read path materialises the decoded rows into a `List<TRow>` (`EnumerableContainerAdapter.FromRows`). Under the `ParallelFlowScheduler` a whole layer of such reads runs at once, so peak memory scales with layer width — the #111 crash-loop on a 1 GB Fargate task. #111's first remedy (a memory-domain concurrency cap on `s3:read`) shipped. **This ADR is the principled second remedy: bound per-read memory to O(batch) so Flowthru can process large datasets on memory-constrained hosts (AWS Lambda, lightweight ECS) rather than over-provisioning to hold buffers.**

The naive encoding — a bare `IAsyncEnumerable<TRow>` payload — fails, because a raw async-enumerable is an *un-enveloped* effect: it enumerates in user step code, throws instead of returning error-values, and has no principled resource lifetime, breaking the three guarantees `FlowIO` exists to provide (typed errors, deterministic disposal, cancellation). Every mature effectful-streaming system (fs2, ZIO `ZStream`, conduit, Rust `Stream`) solves this the same way: the stream type carries the effect envelope inside itself, and its only exit is *compiling* back into the base effect. Flowthru owns that base effect — `FlowIO<A>`, itself a de-HKT'd fork of LanguageExt's `IO`. **The decision is to grow its streaming sibling, `FlowSource<T>`, as a minimal, purpose-built, vendored Prelude primitive** — de-HKT'd to compose with `FlowIO` natively — and make it the catalog streaming payload. This expands the deliberately-minimal Prelude: "Streams" moves from *Excluded (and not planned)* to a documented primitive, scoped to the minimum shape the streaming grain needs.

## Decided

### The type and how it is obtained

- **`FlowSource<T>` is the streaming catalog payload and a new Prelude primitive — vendored minimally, not a `SourceT` port.** LanguageExt's `SourceT` is the reference model, but a faithful fork is a 34-node DSL + a transducer protocol + a bracket built on HKT machinery `FlowIO` lacks — "larger than the entire current Prelude." We fork the *design*, not the code: a purpose-built pull source + compile driver + a handful of combinators, de-HKT'd, failing with `RuntimeError`. Combinators grow on demand (the Prelude's "minimum shape earns its seat" discipline).
- **The minimum surface, enumerated** (because this overturns the Prelude's minimalism policy, the true footprint is on the table): `FlowSource<T>`; the compile driver with terminals `Drain` / `Fold` / `ToList` / `Into`; the combinators `Map` / `Where` / `Fold`; the error bridges `Attempt` / `Rethrow` / `SkipErrors`; `FlowSink<T>` (the `Into` target); and a pull-scoped bracket. The bracket **reuses/extends the Prelude's existing `FlowResource` machinery** ([FlowResource.cs](/src/core/Flowthru.Core/Prelude/FlowResource.cs) — "modelled on Haskell `bracket` / cats-effect `Resource`", LIFO release) rather than inventing a parallel `Scope`; `FlowResource` is flow-scoped today, so the work is a pull-scoped variant on the same primitive, not a new one.
- **Consumption is compile-to-`FlowIO`; that is the sole exit.** `FlowSource<T>` is a pure description whose only public consumption path is `.Compile()`, whose terminals each return a `FlowIO<…>`. It **never** exposes a public `IAsyncEnumerable` / `GetAsyncEnumerator`. Enumeration lives inside the compiled `FlowIO`'s thunk, so the framework — not the caller — owns the `try/finally`, keeping errors-as-values, disposal, and cancellation inside the envelope by construction.

### Errors, resources, fan-out

- **Error channel: terminal `RuntimeError` by default; per-item opt-in.** A mid-stream failure aborts the stream and surfaces at compile as `EffResult.Err` (silent loss of a corrupt partition must be opt-in). For "dead-letter the bad rows, load the rest," the element type is explicitly `FlowSource<EffResult<TRow>>`; `Attempt` / `Rethrow` / `SkipErrors` bridge the two.
- **Typed schema-mismatch translation lives in the storage layer, not the Prelude driver.** The Prelude is language-level and cannot reference `SchemaMismatchException` (a `Flowthru.Data.Storage` type), so a generic driver catch would flatten it to `RuntimeError.External`, regressing the typed `SchemaMismatch` the eager path preserves via `TranslateSchemaMismatch`. The translation is therefore performed in the `ComposedStorageAdapter`-owned per-pull producer (which *can* see the exception) or exposed as a `FlowSource` terminal `MapError` the adapter applies.
- **Acquisition is deferred to first pull; fan-out re-acquires.** The medium (+ temp spill) opens on the first pull inside the bracket, never at construction — so a `FlowSource` built but never compiled leaks nothing. A `FlowSource` compiled twice (an item feeding two steps) **re-acquires and re-reads**, matching today's eager multi-consumer behaviour (each `Load()` re-opens the medium). A genuinely non-replayable source is a documented limitation, not a silent failure.
- **Resource safety without RAII: the pull-scoped bracket + a `try/finally` driver** guarantee prompt, deterministic release on completion, error, cancellation, and early break. Uninterruptible acquire (the fs2 #2966 hazard) has no masking primitive in `FlowIO` today; v1 states the cancel-mid-acquire leak window honestly and covers it with the GC-finalizer backstop rather than claiming a mask it doesn't have.
- **Backpressure is pull-based and free for fused pipelines** (`MoveNextAsync` is demand-driven; a slow sink paces a fast reader with no buffer). A bounded `Channel<T>` is introduced only if concurrency is later added.

### Catalog surface and the seam

- **`.AsStream()` is a container-axis modifier returning a read-only view.** `IItem<IEnumerable<T>>.AsStream() → IReadOnlyItem<FlowSource<T>>` — read-only because composed streaming *writes* are out of scope (below) and `IItem<T>` is read+write, so a writable `FlowSource` item would type a `Save` it has no path for (sharpest in core JSON). It mirrors the `Constrain()` / `WithMaxInspectionLevel()` derived-view idiom.
- **The seam is a capability interface on `ComposedStorageAdapter`, and streaming `Load()` is a new deferred path — not an `IContainerAdapter`.** `.AsStream()` needs to reach the medium and the `IFormatStreamReader` (both private today), and must compose through `ConstrainedStorageAdapter`; a capability interface (exposing medium + reader) provides this. Streaming `Load()` cannot reuse the eager path or an `IContainerAdapter` — `IContainerAdapter.FromRows` returns an eager `Task<TContainer>` that cannot express deferred acquisition — so it is a distinct `Load()` that returns a `FlowSource` description bracketing `medium.ReadStream()` on first pull. (Exact interface shapes are issue-level.)
- **`.AsStream()` is gated to composed `IFormatStreamReader` formats; on a direct adapter it is a design-time/pre-flight error, never a silent materialize.** EFCore / Sheets / GQL are direct `IStorageAdapter<IEnumerable<T>>`s with no format serializer, so `.AsStream()` on them cannot stream; the call must fail loudly rather than wrap an eager `Load()` in a `FlowSource` (which would be O(dataset) masquerading as streaming — the exact dishonesty this ADR exists to kill).

### Container-kind integration — supersede the old machinery

- **`FlowSource<T>` resolves to a new `StepContainerKind.Source` (row type `T`).** Because `FlowSource` never exposes `IAsyncEnumerable`, `Item.Introspection.ContainerKindOf`/`RowTypeOf` would otherwise misclassify it as `Singleton` with row-type `FlowSource<T>`, and extension marshalling would believe a step can shove it across a process boundary as one "row." `Item.Introspection` gains a `FlowSource` case. This **corrects the earlier claim that the introspection layer is payload-opaque** — it structurally inspects payload types and must learn this one.
- **The existing `StepContainerKind.AsyncStream` + `IAsyncStreamMarshaller` are removed (superseded, not deprecated).** They are the bare-`IAsyncEnumerable` container kind this ADR rejects as un-enveloped, and they are unused scaffolding (a bodyless marker with no producer). `FlowSource` is the single streaming container kind. **This also resolves the `.AsStream()`⇄`AsyncStream` verb/noun collision.**
- **The Python extension is migrated onto `FlowSource` in the same coordinated change.** Python is the only `IStepExtension` referencing the removed markers, so removal and migration must land together (or Python first). This is a benefit, not just a cost: Python's streaming today materialises via `ArrowMarshaller.ToList()` (O(dataset)); on `FlowSource` it pulls chunk-at-a-time, marshalling each Arrow batch incrementally (O(chunk)) — the stronger envelope strictly improves it.

### Ergonomics — where materialisation happens, and the intent-level path

- **Materialisation is a per-edge wiring choice, not an in-step call.** A step is a pure `Func<TIn, TOut>` the framework lifts into `FlowIO`; there is no overload where the transform returns `FlowIO<TOut>`, so `source.Compile().ToList()` in a step body does not typecheck. The rule: **wire the eager base item to materialise** (the step body is unchanged, `Load()` buffers), **wire the `.AsStream()` view to stream**. A `GroupBy`/join/sort step simply consumes the eager view. The forced-upstream case (an upstream step already emitted a `FlowSource` into an intermediate item) is **forbidden in v1** — the eager-view path covers every real need — rather than adding a `Func<FlowSource<T>, FlowIO<TOut>>` step overload.
- **An intent-level `AddBulkLoad(source.AsStream(), sink)` helper is the median path.** It emits an on-DAG identity `FlowSource` step (like the existing `PassthroughInputToOutputStep`), so the common source→sink bulk-load gets scheduling, caching, pre-flight, and the `s3:read` cap — while the Flow dev writes intent, not `Compile`/`Into` mechanics. The off-DAG `Load().Compile().Into(sink)` form is **not** the sanctioned surface: it bypasses the scheduler, cache, pre-flight, the Mermaid graph, and the memory cap, and is removed from this ADR.
- **A human-readable mismatch analyzer.** An `FTxxxx` diagnostic maps an eager↔streaming item/transform pairing mismatch to guidance, instead of a raw `CS1503`/`CS0411` on a source-generated overload. (Distinct from the trait-honesty analyzer below.)

### Sink, scheduler, formats, tooling

- **The streaming sink is `IFlowSink<T>` with a batch lifecycle** (open transaction → write batches → commit; dispose-on-error → rollback), driving `EFCore.BulkExtensions` **per batch inside one transaction** so the write is genuinely O(batch) *and* `IsTransactional=true` stays honest. This is a real `EFCoreStorageAdapter`/`BulkSave` refactor (their one-shot `Func<TContext, IEnumerable<T>, …>` saveFunc cannot host it), scoped as its own issue — because as-is, handing `BulkInsertAsync` a lazy enumerable re-materialises it (O(dataset)) and the "O(batch) end-to-end" claim fails.
- **The `s3:read` cap stays; it now gates temp-file disk.** The earlier "streaming relaxes the cap" claim is dropped (the cap is a medium property; container kind is invisible there; "relax" is a join against a conservative-meet model; and the temp spill consumes one-object-of-disk per read). The stale `ParallelFlowScheduler` comment claiming item-derived keys are "a later slice" (contradicted by the live `ConflictKeys.Of` wiring) is corrected.
- **Documented limitation — fused steps do not overlap read and write.** Under whole-step conflict gating a fused streaming step holds *both* its `s3:read` and `efcore:write` keys for the entire stream lifetime, so a sink more parallel than the reader cannot use its parallelism (≈ N·Tw where an eager decompose could approach Tw). This is *not* a memory or correctness regression — peak stays O(batch) — and is accepted for v1 as an explicit documented limitation, because the alternative (decompose to intermediate storage) costs the very memory streaming exists to save.
- **Seek-required formats spill via a core, `FlowResource`-registered "make-seekable" primitive** — not a per-extension `MemoryStream` (Parquet and Excel both need it; a per-extension temp file would live outside the bracket the ADR centralises). When `!stream.CanSeek`, copy to a bounded temp `FileStream`: peak RAM = one row group, peak disk = one object. S3 byte-range reads are a follow-up.
- **First formats: JSON and Parquet.** JSON is core's vanilla builtin — it already reads incrementally via `DeserializeAsyncEnumerable` but dishonestly declares `CanStream=false` and omits `IFormatStreamReader`, so its work is trait/marker honesty + `FlowSource` binding (proving the grain lives in **core**). Parquet adds the temp-spill and row-group yield. CSV already line-streams. JSON is forward-only, so it does not exercise the spill primitive.
- **Cancellation: `IFormatRowReader.DeserializeRows` gains an `[EnumeratorCancellation]` token** — a breaking signature change across every format serializer (there is not one `[EnumeratorCancellation]` in-tree today), required for a cancellable mid-enumeration read.
- **Trait-honesty analyzer + FUnit streaming support + Prelude-scope docs.** An analyzer enforcing "declared `CanStream` ⇒ `Load` actually streams" (traits are decorative today — EFCore declares `CanStream=true` while materialising); a FUnit affordance (`Samples → FlowSource`, an async compile/drain invoke unwinding `EffResult`, assertions for *both* error modes, and a hook to observe bracket release / mid-stream cancellation); and the Prelude `README.md` + `core-architecture.md §1.2` scope update.

## Public API surface (Flow / Catalog developer)

```csharp
// Declare a streaming source — one modifier, identical across streamable formats:
var orders = ItemFactory.Enumerable.Json<Order>   ("orders", "data/orders.json").AsStream();  // IReadOnlyItem<FlowSource<Order>>
var events = ItemFactory.Enumerable.Parquet<Event>("events", "s3://bucket/events.parquet").AsStream();

// The #111 win — stream a source straight to a bulk sink, no FlowSource code, on-DAG:
flow.AddBulkLoad(from: events, to: eventsTable);        // eventsTable backed by IFlowSink

// A stateless transform — lazy combinators, no hand-rolled await foreach:
[FlowthruStep]
public static Func<FlowSource<RawEvent>, FlowSource<Event>> Create() =>
    raw => raw.Map(Normalize).Where(e => e.IsValid);

// Materialise for a GroupBy/join — a VISIBLE wiring choice: consume the EAGER base item.
// The step body is ordinary IEnumerable LINQ, unchanged; there is no in-step .Compile().
flow.AddStep(inputs: eventsEager, outputs: countryTotals, transform: SummarizeByCountry.Create());

// Dead-letter mode — the element type shows it:
[FlowthruStep]
public static Func<FlowSource<EffResult<Event>>, FlowSource<Event>> Create() =>
    src => src.SkipErrors(onError: e => Log.Warn("dropped {Row}", e));
```

The common cases (declare a stream, bulk-load to a sink, stateless map/filter) require no FP ceremony; `FlowSource`, `.Compile()`, and the bridges surface only when genuinely needed. Vocabulary discipline holds — `O(batch)`, replayability, and row-group sizing stay out of the Flow/Catalog glossary.

## Extension surface (Extension developer)

- **A streaming source is a format+medium capability; a streaming sink is a storage-adapter capability (`IFlowSink<T>`)** — a real batch-lifecycle contract, *not* the removed `IAsyncStreamMarshaller` marker.
- **Python migrates onto `FlowSource`** — chunk-wise Arrow marshalling replacing `ArrowMarshaller.ToList()`; coordinated with the removal of the old markers.
- **Push-parallel engines (Spark) route to `Queryable`/pushdown, not `FlowSource`** (pull/sequential/single-consumer vs push/partition-parallel; `toLocalIterator` collapses Spark's parallelism). Honest caveat: `StepContainerKind.Queryable` / `IQueryableMarshaller` is *today an empty capability witness with no producer* — routing Spark there defers it to a not-yet-substantive surface, not a ready one. Continuous Structured Streaming stays out by the not-orchestration principle.

## Core invariant (Core developer)

- **Core treats item payloads opaquely in dispatch, DAG, cache, and pre-flight** (wire by label, fingerprint by source, inspect via a throwaway stream) — but **`Item.Introspection` is the one payload-structural exception**, and it learns a `FlowSource`/`Source` case. `FlowSource`'s compile-to-`FlowIO` exit keeps enumeration inside the envelope, so errors-as-values / disposal / cancellation are preserved by construction.
- **Future core development does not have to make everything streaming.** The burden is confined to the `FlowSource` compile driver (Prelude), the streaming `Load()` path (`ComposedStorageAdapter`), and the `Item.Introspection` case — not the scheduler, DAG, or cache.

## Considered options

- **Bare `IAsyncEnumerable<TRow>` as the payload.** Rejected: un-enveloped — enumeration escapes the `FlowIO` envelope, losing typed errors, deterministic disposal, and cancellation. `FlowSource` re-envelopes it.
- **A `LoadStream()` side-method on the eager item.** Rejected: streaming stays invisible to the type system and DAG.
- **Depend on the `LanguageExt.Streaming` NuGet package instead of vendoring.** Rejected on four independent grounds, verified mid-2026: (1) **no stable v5 exists** — `LanguageExt.Streaming` is a v5-only package that has never shipped a stable release (latest `5.0.0-beta-77`), so a *stable* Flowthru would ride a churning, unlistable beta (→ downstream `NU1102`); (2) **v4/v5 cannot coexist** — NuGet enforces one version per package ID with no supported side-by-side mechanism, so Flowthru-on-v5 breaks every downstream still on stable `LanguageExt` v4, *permanently*, which is the exact audience a coexistence-friendly library protects; (3) **IL-merge/privatize is non-viable** — Flowthru exposes these as public Prelude vocabulary (internalise → unusable; rename → different CLR identity, no interop), and v5's `static abstract` traits + source generators + module initializers are a worst-case merge target; (4) **HKT impedance** — `SourceT<M,A>` requires `M : MonadIO<M>`, which `FlowIO` deliberately is not, so even a clean dependency would force a pervasive `IO↔FlowIO` bridge or reverse `FlowIO`'s de-HKT. Vendoring gives these types a distinct namespace and CLR identity that collides with no one. This is the established `FlowIO` precedent.
- **A faithful `SourceT` fork (full 34-node DSL + transducers).** Rejected in favour of a minimal purpose-built `FlowSource`: the faithful fork is "larger than the entire current Prelude," most of it unused; combinators grow on demand.
- **Deprecate rather than remove `AsyncStream`/`IAsyncStreamMarshaller`.** Rejected: it is unused scaffolding and its one real consumer (Python) strictly benefits from migrating to the stronger `FlowSource` envelope, so a clean removal + migration beats a deprecation cycle.
- **Streaming composed file/S3 *writes* now.** Deferred: file-write streaming (temp + atomic rename — streaming *and* atomic) and S3 multipart (which forfeits single-PUT atomicity) are distinct problems with their own tradeoffs.
- **"Streaming relaxes the `s3:read` cap."** Rejected: unimplementable (cap is a medium property; container kind invisible there) and directionally wrong (a join against a conservative-meet model). The cap stays and gates disk.

## Consequences

- **Flow developer** — additive and opt-in. `.AsStream()` a source; `AddBulkLoad` for the common case; lazy combinators for stateless transforms; materialise by wiring the eager view. A wrong eager/streaming pairing is a readable analyzer diagnostic. FUnit gains streaming support.
- **Catalog developer** — one derived-view modifier, capability-gated, reused across formats; read-only streaming items.
- **Extension developer** — the `IFlowSink<T>` streaming sink (`EFCore.Bulk`, a per-batch/transaction refactor) is the real work; format serializers gain the streaming-decode path + a cancellation token; **Python is migrated onto `FlowSource`** alongside the removal of `AsyncStream`/`IAsyncStreamMarshaller`.
- **Core developer** — the minimal `FlowSource` Prelude primitive + compile driver + the `FlowResource`-derived pull bracket; the streaming `Load()` path + `.AsStream()` seam through `ComposedStorageAdapter`/`ConstrainedStorageAdapter`; the `Item.Introspection` `Source` case + removal of the old kind; the core make-seekable spill; the `DeserializeRows` cancellation break; the two analyzers; the Prelude scope/doc update.

## Anchor code

- `src/core/Flowthru.Core/Prelude/FlowSource.cs` **(new)**, the compile driver + terminals + bridges, and the pull-scoped bracket derived from `Prelude/FlowResource.cs`; `Prelude/README.md` + `docs/explanation/advanced/core-architecture.md §1.2` scope update.
- `src/core/Flowthru.Core/Data/Catalog/ComposedStorageAdapter.cs` + `ConstrainedStorageAdapter.cs` + `CatalogItemExtensions.cs` — the `.AsStream()` capability seam, the deferred streaming `Load()`, and the storage-layer schema-mismatch translation.
- `src/core/Flowthru.Core/Data/Catalog/Item.Introspection.cs` + `StepContainerKind.cs` + `Step/.../Marshalling/IContainerMarshaller.cs` — add `Source`, remove `AsyncStream`/`IAsyncStreamMarshaller`.
- `src/extensions/Flowthru.Extensions.Python/…` — migrate marshalling onto `FlowSource` (chunk-wise Arrow), landing with the marker removal.
- `src/core/Flowthru.Core/Data/Storage/JsonFormatSerializer.cs` — `CanStream`/`IFormatStreamReader` honesty, `FlowSource` binding, cancellation token (core's first streaming format).
- `src/extensions/Flowthru.Extensions.Parquet/.../ParquetFormatSerializer.cs` — use the core make-seekable spill; cancellation token.
- `src/extensions/Flowthru.Extensions.EFCore.Bulk/BulkSave.cs` + `EFCoreStorageAdapter.cs` — the `IFlowSink<T>` per-batch/transaction sink.
- `src/core/Flowthru.Core/Flow/ParallelFlowScheduler.cs` + `.../S3ReadDependency.cs` — keep the cap; correct the stale comment; document the fused-step key-hold limitation.
- FUnit harness + a new mismatch analyzer + the trait-honesty analyzer; `tests/…/Backends/MinioContainerBackend.cs` — the constrained-memory regression tier.
