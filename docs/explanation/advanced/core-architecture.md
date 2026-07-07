---
title: Core Architecture for Contributors
description: How Flowthru's functional core is shaped, how extensions hook into it, and why Flow and Catalog Developers never see any of it. Written for prospective Core Contributors who know FP basics (closure, currying, monads, side effects) but haven't yet seen those primitives organised into a working framework.
---

This document explains the *why* behind Flowthru's core. It is the architectural companion to [CONTRIBUTING.md](/CONTRIBUTING.md) — that file tells you the rules; this file tells you why those rules produce the shape they do.

The intended reader knows FP basics (closures, currying, monads as `Bind`-able effect values, side effects as a thing to push to the edges) but hasn't necessarily seen those primitives composed into a framework before. By the end of this doc you should be able to answer:

1. **What is the core's design, and why is it shaped this way?**
2. **How do extensions hook into it without becoming part of it?**
3. **Why don't Flow and Catalog Developers ever name any of this?**

We'll take those questions in order.

## 1. The Core's Design

### 1.1 Start from the promise, not the primitives

Flowthru's promise — restated from [CONTRIBUTING.md](/CONTRIBUTING.md) — is:

> If an error can occur in the flow they've created, it will occur as soon in the development process as possible.

Errors are then sorted into three phases — **build-time**, **pre-flight**, and **runtime** — with a strong preference for the earliest phase that can plausibly catch the error. Each phase has different failure semantics, and so each phase calls for a *different* primitive:

| Phase      | What can fail                                                           | What we want from a primitive                                                           |
| ---------- | ----------------------------------------------------------------------- | --------------------------------------------------------------------------------------- |
| Build-time | Schema mismatch, miswired step, unsupported format pairing              | The compiler refuses to emit; no primitive at runtime — diagnostics + types do the work |
| Pre-flight | Missing files, unreachable databases, schema drift, duplicate producers | **Accumulate every error at once** so the user fixes them in one pass                   |
| Runtime    | Network drops, OOM, transform exceptions                                | **Capture errors as values** so they can't be silently dropped or thrown into the void  |

The FP layer in [src/core/Flowthru.Core/Prelude/](/src/core/Flowthru.Core/Prelude/) is not chosen for its own sake — each primitive is the *minimum* shape that makes one of those guarantees mechanically true. Read the Prelude with that lens and the choices stop looking like style.

### 1.2 The Prelude — a deliberately small subset

The Prelude is derived from a small slice of [LanguageExt](https://github.com/louthy/language-ext), but the [Flowthru.Core README](/src/core/Flowthru.Core/README.md) is emphatic that this is **not a port**. We took only the FP primitives Flowthru actively uses, and we own them going forward. No HKTs (`K<F, A>`), no generic `Functor<F>` / `Applicative<F>` / `Monad<F>` typeclasses, no monad transformer stack, no `Either` / `Option` / `Try` / `Fin`, no `Seq` / `Lst` / `Iterable`. Every additional abstraction would buy generality we don't need at the cost of API surface a future contributor would have to learn.

The five primitives that survived the cut are:

#### `FlowIO<A>` — [Prelude/FlowIO.cs](/src/core/Flowthru.Core/Prelude/FlowIO.cs)

The runtime effect type. `FlowIO<A>` is a description of a computation that, when run, yields either a value of type `A` or a `RuntimeError`. It is structurally a `Func<CancellationToken, Task<EffResult<A>>>`, but you should not think of it as a function: think of it as a **Kleisli arrow** in the category of Flowthru effects.

The constructors are deliberately spare:

- `Pure(value)` — lift a value into a successful effect
- `Fail(error)` — lift an error into a failing effect
- `Lift(Func<A>)` / `LiftAsync(Func<CancellationToken, Task<A>>)` — wrap a side-effecting function; thrown exceptions are captured as `RuntimeError.External`, cancellation as `RuntimeError.Cancelled`. This is the **only** place untyped exceptions enter the typed-error world.

The combinators are the standard four:

- `Map(f)` — change the success value
- `Bind(f)` — sequence a dependent effect; LINQ syntax (`from … in …`) lowers to this
- `MapError(f)` — change the failure
- `Catch(handler)` — recover from failure

Two consequences fall out for free:

**Side effects can't be accidentally dropped.** A `FlowIO<A>` value held in a variable does nothing until `Run` is called. There is no `var = doThing()` shape that performs an I/O and forgets the result. The framework is the only thing that calls `Run`, at one specific point in the scheduler, and the result is structurally inspected, not thrown.

**Errors can't be silently swallowed.** A failing `FlowIO<A>` is still a value; consumers must pattern-match `EffResult<A>` to get at the success. Forgetting to handle the failure is a compile-time error at every consumer site.

#### `FlowSource<A>` — [Prelude/FlowSource.cs](/src/core/Flowthru.Core/Prelude/FlowSource.cs)

The streaming sibling of `FlowIO<A>`: where `FlowIO<A>` describes a computation yielding *one* value-or-error, `FlowSource<A>` describes a lazy, resource-safe stream of *many* `A` values. Its defining rule is that its **only** consumption path is `.Compile()`, whose terminals (`Drain`, `Fold`, `ToList`) each return a `FlowIO`. Enumeration therefore always runs *inside* the effect envelope, so `FlowIO`'s guarantees extend to streams:

- **Errors are values.** A mid-stream failure surfaces at compile as a terminal `RuntimeError`; it never escapes as a thrown exception into consumer code. `Attempt` moves failures in-band (`FlowSource<EffResult<A>>`) for dead-lettering, with `SkipErrors` / `Rethrow` to collapse back.
- **Resources release deterministically.** The byte source is a `FlowResource` (below) acquired on the *first pull* and released on every exit path — completion, failure, cancellation. A source built but never run acquires nothing.
- **Backpressure is pull-based.** A slow consumer paces a fast producer with no buffering, so a read → transform → write pipeline runs in bounded (`O(batch)`) memory rather than `O(file)`.

This is why a streaming catalog read can process a dataset larger than RAM. The full rationale — including why we vendor a minimal `FlowSource` rather than take a LanguageExt dependency — is [ADR-0023](/.claude/docs/adr/0023-streaming-reads-as-catalog-item-type.md).

#### `EffResult<A>` — [Prelude/EffResult.cs](/src/core/Flowthru.Core/Prelude/EffResult.cs)

The closed sum returned by `FlowIO<A>.Run`:

```csharp
public abstract record EffResult<A>
{
  private EffResult() { }                            // closed
  public sealed record Success(A Value)            : EffResult<A>;
  public sealed record Failure(RuntimeError Error) : EffResult<A>;
}
```

The private constructor is load-bearing. Because no derived case can be added outside this file, every consumer's `switch` on `EffResult<A>` is exhaustive *forever*; if Flowthru ever added a third case (it won't), every site would surface as a compile diagnostic until handled. The same construction pattern is used in [`RuntimeError`](/src/core/Flowthru.Core/Validation/Runtime/RuntimeError.cs), [`PreFlightError`](/src/core/Flowthru.Core/Validation/PreFlight/PreFlightError.cs), [`Validated`](/src/core/Flowthru.Core/Prelude/Validated.cs), [`DependencyAnalyzer.Result`](/src/core/Flowthru.Core/Flow/DependencyAnalyzer.cs), and [`StepResult`](/src/core/Flowthru.Core/Flow/StepResult.cs). When you see `private Foo() { }` with `sealed record` cases, that's a closed sum. Treat it as a non-extensible algebraic data type.

#### `Validated<TError, TValue>` — [Prelude/Validated.cs](/src/core/Flowthru.Core/Prelude/Validated.cs)

The pre-flight error-accumulating applicative. Conceptually similar to a `Result` / `Either`, but with one critical difference: when both sides of a `Zip` are `Invalid`, the resulting `Invalid` carries errors from **both**:

```csharp
// ZipAll across a list of Validated produces every error at once
Validated.ZipAll(checks)        // Invalid([err1, err2, err3, …])
```

This is the right shape for pre-flight specifically because of the user experience contract from CONTRIBUTING.md: pre-flight is "tolerable but aggravating," and the way to make it less aggravating is to surface every problem in one pass instead of one-per-rerun. A monadic `Either` would short-circuit on the first error and force the user to discover-fix-rerun N times. `Validated`'s applicative `Zip` is what lets the pre-flight pipeline accumulate.

(Note: `Validated`'s LINQ syntax is monadic — it short-circuits — because LINQ is a sequencing notation. Use `Zip` / `ZipAll` explicitly when you want accumulation.)

#### `FlowResource<TScope>` — [Prelude/FlowResource.cs](/src/core/Flowthru.Core/Prelude/FlowResource.cs)

A pair of effects: an `acquire` step that produces a scope, and a `release` step that disposes it. Modelled on Haskell's `bracket` / cats-effect's `Resource`. The framework guarantees release runs on every exit path — success, failure, cancellation — and the release closure receives the body's `RuntimeError?` so it can apply policies like "preserve files on failure."

The fact that acquire and release are **bundled into one value** is the point. A catalog can't accidentally publish an acquire without a release; the framework can't run one without preparing the other.

### 1.3 The phase ADTs

`FlowIO<A>` and `Validated<E, T>` are language-level primitives with two type parameters — the value, and the error. The Flowthru-specific error types fill in those parameters:

- [`RuntimeError`](/src/core/Flowthru.Core/Validation/Runtime/RuntimeError.cs) is the failure type of every `FlowIO<A>`. Its closed-sum cases name every distinct way Flowthru execution can fail at runtime: `External` (act of God), `StepFailed` (attribution), `Cancelled` (control flow), `InvariantViolated` (a Flowthru bug — see §1.4), `ConstraintViolated` (a deliberately-narrowed catalog item was used wrong), `SchemaMismatch` (format adapter saw a structural mismatch mid-stream), and `ExtensionError` (the open extension point — see §2).
- [`PreFlightError`](/src/core/Flowthru.Core/Validation/PreFlight/PreFlightError.cs) is the error type of every pre-flight `Validated`. Its cases are the failure modes detectable *before* any pipeline logic runs: `DuplicateProducer`, `CircularDependency`, `MissingInput`, `SchemaDrift`, `InspectionFailed`, `RegistrationCheckFailed`, and the open extension variant `External`.

The split between *language-level* primitives (in `Flowthru.Prelude`) and *phase-specific* ADTs (in `Flowthru.Validation.*`) is intentional. The Prelude does not know about Flowthru; the phase ADTs *are* Flowthru. Adding a new pre-flight failure mode means editing `PreFlightError`, not `Validated`.

### 1.4 The `InvariantViolated` case is a tripwire

`RuntimeError.InvariantViolated` deserves special attention. Its presence at runtime means a pre-flight check that *should* have caught the condition was missing or wrong:

> A flow that passes pre-flight should always complete successfully. If it doesn't, that's a bug in Flowthru.

Surfacing this as a typed value (rather than letting it disappear into a generic exception) is what makes that invariant operationally checkable. The renderer treats it differently from `External` — "please file an issue" rather than "your flow is broken." If you find yourself reaching for `InvariantViolated` during a fix, stop and ask whether the condition belongs in pre-flight instead.

### 1.5 The bipartite DAG: places and arrows

A Flowthru pipeline is a [Petri-net-like](https://en.wikipedia.org/wiki/Petri_net) bipartite graph: items are *places* (where data lives), steps are *arrows* (how data moves between places). The two facets are reflected in the type hierarchy under [INode](/src/core/Flowthru.Core/Data/Catalog/INode.cs):

- [`IItem<T>`](/src/core/Flowthru.Core/Data/Catalog/IItem.cs) is the place archetype. It exposes `Load(): FlowIO<T>` and `Save(T): FlowIO<FlowUnit>` — Kleisli arrows in and out of the catalog. The untyped `IItem` interface exists so the engine can iterate over a step's `Inputs` / `Outputs` without naming each item's element type.
- [`IStepNode<TIn, TOut>`](/src/core/Flowthru.Core/Step/IStepNode.cs) is the arrow archetype. It carries a `Transform: Func<TIn, FlowIO<TOut>>` — the Kleisli arrow this step represents — plus the declared `Inputs` and `Outputs` (lists of `IItem`s) the dependency analyzer needs.

Why bipartite? Because the two key invariants — single-producer per item, no cycles — are statements about how arrows connect places. Encoding each as its own kind of vertex makes those invariants natural to express:

- **Single producer:** "no two arrows have the same place as their target." Mechanised in [DependencyAnalyzer](/src/core/Flowthru.Core/Flow/DependencyAnalyzer.cs) — a duplicate output label is rejected as `Result.DuplicateProducer` before any topological sort runs.
- **No cycles:** Kahn's algorithm over the arrow→arrow adjacency derived from the producer map; a residual non-zero in-degree at the end yields `Result.CycleDetected`.

Both are pre-flight failures. Both surface as cases on `DependencyAnalyzer.Result`'s closed sum. Neither can ever reach runtime.

### 1.6 `FlowBuilder` is an algebra; `BuiltFlow` is a program

`FlowBuilder` is the construction algebra for flows — a small set of operations (`AddStep<…>`, `Add(IStepNode)`, `Build`) that produce an immutable [`BuiltFlow`](/src/core/Flowthru.Core/Flow/BuiltFlow.cs). The `AddStep<…>` overload matrix — every input arity × output arity × (sync, async, async-with-cancellation) — is **emitted by the source generator** in [FlowBuilderGenerator.cs](/src/core/Flowthru.Core.SourceGenerators/Flow/FlowBuilderGenerator.cs), not hand-written, because (a) hand-writing 75+ shapes is a lot of boilerplate, and (b) doing so in a generator lets us widen the matrix later without disturbing call sites.

The name *algebra* matters. Construction returns a description, not an action: a `BuiltFlow` is a value you can inspect, slice, persist, or run multiple times. Construction itself is total — the only way `Build()` fails is by raising `FlowBuildException` for one of `DependencyAnalyzer`'s closed-sum violation cases, and even those are pre-flight, not runtime.

The runtime — [`ParallelFlowScheduler`](/src/core/Flowthru.Core/Flow/ParallelFlowScheduler.cs) — interprets the `BuiltFlow` by walking the topological order, calling `step.Execute().Run(ct)` on each, and collecting `EffResult<FlowUnit>`s into [`StepResult`](/src/core/Flowthru.Core/Flow/StepResult.cs) (another closed sum: `Succeeded | Failed | Skipped`). The scheduler does not know about the FP types beyond `EffResult` pattern-matching — interpretation is straightforward by design, because the description side carries all the structure.

This separation — **build a description, then interpret it** — is what makes the same `BuiltFlow` runnable in the parallel scheduler, in dry-run mode, sliced to a subgraph, or by a future scheduler we haven't written yet. None of those interpreters need to be aware of each other.

## 2. How Extensions Extend the Core

A useful slogan: **Core defines closed sums for the things it must reason about exhaustively, and open extension points for the things it cannot anticipate.** Extensions live entirely in the open extension points.

There are four such points, in roughly increasing depth:

### 2.1 New formats and storage mediums

The most common extension shape: support for CSV, Parquet, Excel, EFCore, HTTP. Look at [Flowthru.Extensions.Csv/Data/Catalog/CsvItemFactoryExtensions.cs](/src/extensions/Flowthru.Extensions.Csv/Data/Catalog/CsvItemFactoryExtensions.cs) for the canonical shape:

```csharp
namespace Flowthru.Data.Catalog;   // *Core's* namespace, not the extension's

public static class CsvItemFactoryExtensions
{
  public static IItem<IEnumerable<TRow>> Csv<TRow>(
    this EnumerableItemFactory _,
    string label, string filePath, …)
    where TRow : notnull, IFlatSchema, ITextSerializable
    => new Item<IEnumerable<TRow>>(
         label,
         new ComposedStorageAdapter<IEnumerable<TRow>, TRow>(
           new FileStorageMedium(filePath),
           new CsvFormatSerializer<TRow>(),
           new EnumerableContainerAdapter<TRow>()));
}
```

A few things to notice:

- The extension method hangs off [`EnumerableItemFactory`](/src/core/Flowthru.Core/Data/Catalog/ItemFactory.cs) — a *factory anchor* that exists specifically so extensions can attach smart constructors. Core ships JSON and Memory by extension methods on the same anchor; extensions add CSV, Parquet, EFCore, etc. End users see a uniform `ItemFactory.Enumerable.<format>(…)` surface.
- The generic constraints (`IFlatSchema`, `ITextSerializable`) come from the schema source generator's marker interfaces — that's how "you can't save a nested schema as CSV" becomes a build error rather than a runtime check. The extension does not have to police this itself; the type system does.
- Composition is preferred where possible: `ComposedStorageAdapter` combines a [medium](/src/core/Flowthru.Core/Data/Storage/IStorageMedium.cs), a [format](/src/core/Flowthru.Core/Data/Storage/IFormatSerializer.cs), and a [container](/src/core/Flowthru.Core/Data/Storage/IContainerAdapter.cs). New format × existing mediums (or vice versa) costs one implementation, not M×N. See [storage-composition.md](./storage-composition.md) for the full picture.
- For storage shapes where composition doesn't apply — databases, HTTP — implementations satisfy [`IStorageAdapter<T>`](/src/core/Flowthru.Core/Data/Storage/IStorageAdapter.cs) directly. EFCore in [EFCoreStorageAdapter.cs](/src/extensions/Flowthru.Extensions.EFCore/Data/Storage/EFCore/EFCoreStorageAdapter.cs) is the canonical example.

The adapter's `Load` and `Save` return `FlowIO<…>`. This is the one place extension authors *do* see Prelude types — they're writing the leaf effects that Core's machinery composes.

### 2.2 New step archetypes

Core ships exactly one step archetype: `[FlowthruStep]` over a static class with a `Create()` factory returning a `Func<TIn, TOut>`. Extensions can add others by implementing [`IStepNode`](/src/core/Flowthru.Core/Step/IStepNode.cs) directly and offering a custom `AddPythonStep` / `AddSparkStep` / etc. that calls `FlowBuilder.Add(IStepNode)` — the hand-written half of [FlowBuilder.cs](/src/core/Flowthru.Core/Flow/FlowBuilder.cs) exposes that escape hatch deliberately.

An archetype extension takes on more responsibility: it needs to wrap its underlying machinery (a Python interpreter, a Spark session) into a `FlowIO`-typed `Execute()`, declare its inputs and outputs the same way the standard `Step<TIn, TOut>` does, and surface its errors through `RuntimeError` (typically by way of `ExtensionError` — see §2.4).

### 2.3 Registration-time validation hooks

Extensions can plug into the pre-flight phase by registering `IRegistrationValidationHook`s on the host builder. The canonical example is [`VerifyEFCoreConnection<TContext>()`](/src/extensions/Flowthru.Extensions.EFCore/Hosting/EFCoreFlowthruBuilderExtensions.cs):

```csharp
return builder.RegisterValidationHook(id, services =>
  FlowIO.LiftAsync<Validated<PreFlightError, FlowUnit>>(async ct =>
  {
    var factory = services.GetService<IDbContextFactory<TContext>>();
    if (factory is null)
      return Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.RegistrationCheckFailed(...));
    // … probe live connection, return Pure(Default) or Fail(...) …
  }, source: id));
```

Hooks run once per process at host startup (or eagerly via `ValidateRegistrationAsync`). They return `FlowIO<Validated<PreFlightError, FlowUnit>>` — note the layering: the **outer** `FlowIO` is for the act of running the check (it can fail with an act-of-God `RuntimeError`), the **inner** `Validated` is for the check's result (pre-flight semantics, accumulating). Hooks are how an extension says "before any flow runs, check that *my* preconditions hold."

### 2.4 New error categories — without touching the closed sum

Extensions never add cases to `RuntimeError` or `PreFlightError` directly. The closed sums are Core's responsibility; extending them would defeat the exhaustive-matching guarantee the closed-ness exists to provide. Instead, both ADTs include an open variant — `RuntimeError.ExtensionError(IExtensionRuntimeError)` and `PreFlightError.External(IExtensionPreFlightError)` — that wraps an extension-implemented interface:

```csharp
public interface IExtensionRuntimeError
{
  string Message { get; }
  string Category { get; }       // routes through Core's classifier
  string DiagnosticCode { get; } // FT4xxx range, see diagnostics docs
}
```

Extensions speak Core's error language by satisfying the interface, not by extending the sum. Core's classifier and renderer dispatch generically on `Category` and `DiagnosticCode`. The closed-sum invariant is preserved; the error vocabulary is open.

This pattern — closed sum with one open variant — recurs throughout the codebase. When you see it, the open variant is the deliberate extension point. Reach for it when you genuinely need to extend; do not reach for adding a new closed-sum case.

## 3. The Boundary

The third question — and the one most worth getting right — is **why none of the FP machinery above leaks into the Flow Developer or Catalog Developer experience.** The answer is roughly: source generators, smart constructors, and the canonical step shape.

### 3.1 What a Flow Developer actually writes

This is a real, complete step from the Iris starter ([SplitAndEncodeStep.cs](/examples/starter/IrisFUnit/Flows/DataEngineering/Steps/SplitAndEncodeStep.cs)):

```csharp
[FlowthruStep]
public static class SplitAndEncodeStep
{
  public record Options { public double TestDataRatio { get; init; } = 0.2; }

  public static Func<
    (IEnumerable<IrisRawSchema> Data, Options Options),
    (IEnumerable<IrisFeatureSchema> Features,
     IEnumerable<FeatureVectorSchema>  TrainX,
     IEnumerable<TargetLabelSchema>    TrainY,
     IEnumerable<FeatureVectorSchema>  TestX,
     IEnumerable<TargetLabelSchema>    TestY)
  > Create() =>
    input => { /* … pure C# transformation … */ };
}
```

There is no `FlowIO`, no `Bind`, no `EffResult`, no `RuntimeError`, no `Validated`, no LINQ-monadic syntax. The step is a `Func<TIn, TOut>`. The user writes a pure function from one tuple of schemas to another tuple of schemas. They're a data scientist; they're writing the transformation, not the plumbing.

### 3.2 What the framework does with it

The boundary lives in [FlowBuilderGenerator.cs](/src/core/Flowthru.Core.SourceGenerators/Flow/FlowBuilderGenerator.cs). When the user calls:

```csharp
pipeline.AddStep<IEnumerable<IrisRawSchema>, IEnumerable<IrisFeatureSchema>, …>(
  label:     "SplitAndEncode",
  transform: rawData => splitTransform((rawData, splitOptions)),
  input1:    catalog.IrisRaw,
  output1:   catalog.IrisFeatures,
  output2:   catalog.TrainX,
  …);
```

the generated overload — emitted by `FlowBuilderGenerator` for the user's specific arity — does three things:

1. **Wraps the user's transform with `FlowIO.Lift`.** Synchronous transforms become `FlowIO.Lift(() => transform(input))`; async transforms become `FlowIO.LiftAsync(_ => transform(input))`. Any exception thrown by the user becomes a `RuntimeError.External` value — failures-as-values, captured at the boundary.
2. **Builds a `loadInputs` closure** that chains the catalog items' `Load()` calls through `Bind`, materialising the input tuple: `input1.Load().Bind(v1 => input2.Load().Bind(v2 => Pure((v1, v2))))`.
3. **Builds a `saveOutputs` closure** that chains the outputs' `Save(...)` calls through `Bind`, propagating the first failure and short-circuiting the rest.

The resulting [`Step<TIn, TOut>`](/src/core/Flowthru.Core/Step/Step.cs) holds these as private fields, and its `Execute()` method composes them in three lines of LINQ:

```csharp
public FlowIO<FlowUnit> Execute() =>
  from input  in _loadInputs()
  from output in Transform(input)
  from _      in _saveOutputs(output)
  select FlowUnit.Default;
```

This is the only place in the framework where the user's transform meets the FP runtime. The Kleisli composition `load >=> transform >=> save` is what the engine ultimately runs. The user never sees it; the source generator wrote it for them.

### 3.3 Catalogs: the same trick on the data side

Catalog Developers have a parallel experience. They write:

```csharp
public IItem<IEnumerable<IrisRawSchema>> IrisRaw =>
  CreateItem(() => ItemFactory.Enumerable.Json<IrisRawSchema>(
    label: "IrisRaw",
    filePath: $"{_basePath}/_01_Raw/Datasets/iris.json"));
```

The smart constructor returns an `IItem<IEnumerable<IrisRawSchema>>` whose `Load()` and `Save(…)` are `FlowIO`-typed. But the catalog developer never calls those methods — they hand the `IItem<…>` to a step's `AddStep` call, and the framework calls `Load`/`Save` from inside the generated `loadInputs` / `saveOutputs` closures. From the catalog developer's perspective, an `IItem<T>` is a typed handle to a place data lives. The fact that it's also a Kleisli arrow into the runtime is a framework concern.

[CatalogAbstract](/src/core/Flowthru.Core/Data/Catalog/CatalogAbstract.cs) caches each property's item by name on first access, so the DAG sees one stable `IItem<T>` instance per catalog property — object identity is what makes catalogs usable as DAG vertices.

### 3.4 The compact statement of the boundary

The boundary is enforced by *what each role names*:

| Role                | Names                                                                                                         | Does not name                                                                                           |
| ------------------- | ------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| Flow Developer      | `Func<TIn, TOut>`, schemas, `[FlowthruStep]`, `IItem<T>` (held, not invoked)                                  | `FlowIO`, `EffResult`, `Bind`, `Validated`, `RuntimeError`                                              |
| Catalog Developer   | `IItem<T>`, `ItemFactory.<Cardinality>.<Format>(...)`, `CatalogAbstract`, schema attributes                   | `FlowIO` (returned but not invoked), `IStorageAdapter`, `RuntimeError`                                  |
| Extension Developer | `IItem<T>`, `IStorageAdapter<T>`, `FlowIO`, `Validated`, `IExtensionPreFlightError`, `IExtensionRuntimeError` | The closed-sum cases of `RuntimeError` / `PreFlightError` (write through `External` / `ExtensionError`) |
| Core Developer      | Everything                                                                                                    | —                                                                                                       |

When you contribute to Core, ask: *which row of that table is this name supposed to live in?* If you find yourself making a Flow Developer name a `FlowIO`, you've broken the boundary. If you find yourself making an extension author depend on a closed-sum case directly, you've broken a different invariant. The role analysis from CONTRIBUTING.md and the boundary above are the same boundary, viewed from two directions.

### 3.5 Source generators do most of the boundary work

The four source generators in [src/core/Flowthru.Core.SourceGenerators/](/src/core/Flowthru.Core.SourceGenerators/) each enforce a piece of this boundary:

- [FlowBuilderGenerator](/src/core/Flowthru.Core.SourceGenerators/Flow/FlowBuilderGenerator.cs) — emits the `AddStep<…>` matrix that translates raw `Func<TIn, TOut>` into `FlowIO`-wrapped Kleisli arrows. This is the single largest contributor to the boundary.
- [SchemaInterfaceGenerator](/src/core/Flowthru.Core.SourceGenerators/Schema/SchemaInterfaceGenerator.cs) — analyses `[FlowthruSchema]` records, classifies them as flat / nested, and emits the marker interfaces (`IFlatSchema`, `INestedSchema`, `ITextSerializable`, `IStructuredSerializable`) that gate which adapters will accept the schema. This is what makes "save nested schema as CSV" a build error rather than a runtime explosion.
- [CatalogPropertyGenerator](/src/core/Flowthru.Core.SourceGenerators/Catalog/CatalogPropertyGenerator.cs) — emits the bodies for attribute-driven partial properties (`[JsonItem(...)]`, `[CsvItem(...)]`, etc.) on the catalog so Catalog Developers don't have to repeat `CreateItem(() => ItemFactory.…)` boilerplate.
- [StepMetadataGenerator](/src/core/Flowthru.Core.SourceGenerators/Step/StepMetadataGenerator.cs) — emits a companion `{StepName}_Metadata` record alongside every `[FlowthruStep]`-decorated class, so diagnostics, metadata exporters, and architecture tests can walk every step in an assembly without reflecting at runtime.

The generators are *part* of the design, not an optimisation. Without them, the boundary would have to be enforced by convention or by burdening the user with FP-typed APIs. With them, the user's code is straightforward C# and the framework absorbs the structure.

## 4. Where to go next

- [CONTRIBUTING.md](/CONTRIBUTING.md) — the rules and the three error phases.
- [Flowthru.Core README](/src/core/Flowthru.Core/README.md) — the Prelude's provenance and scope.
- [storage-composition.md](./storage-composition.md) — how `IStorageMedium` × `IFormatSerializer` × `IContainerAdapter` factor.
- [anatomy-of-a-flow.md](/docs/explanation/anatomy-of-a-flow.md) — the same picture, from the Flow Developer's side, useful for sanity-checking that the boundary holds.
- The Prelude itself: [FlowIO](/src/core/Flowthru.Core/Prelude/FlowIO.cs), [Validated](/src/core/Flowthru.Core/Prelude/Validated.cs), [EffResult](/src/core/Flowthru.Core/Prelude/EffResult.cs), [FlowResource](/src/core/Flowthru.Core/Prelude/FlowResource.cs). Each file is short. Read them.

If something in this document disagrees with the code, the code is correct and this document is stale — please send a PR.
