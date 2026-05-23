# Contributing to Flowthru Core

This document is for **Core Developers** — folks curating Flowthru's core engine and the packages that ship with it (Cli, FUnit, source generators, code fixes). Core is what every other role builds on.

**Audience scope:** assumes familiarity with [examples/CONTRIBUTING.md](/examples/CONTRIBUTING.md) (Flow/Catalog Developer vocabulary) and [src/extensions/CONTRIBUTING.md](/src/extensions/CONTRIBUTING.md) (Extension Developer vocabulary — especially [[Closed sum]] and the [[Extension surface]] concept). Terms defined here are the *additional* vocabulary specific to Core work.

See [/CONTRIBUTING.md](/CONTRIBUTING.md) for cross-cutting design rules.

## What Core Is

`src/core/` is the set of capabilities Flowthru ships as its core offering:

- **Flowthru.Core** — the engine: Flow building, Step execution, Catalog adapter contracts, validation, caching, hosting
- **Flowthru.Cli** — the CLI host (run Flows, query metadata)
- **Flowthru.Core.SourceGenerators** / **Flowthru.Core.CodeFixes** — the Roslyn surface that turns Flowthru's design-time guarantees into IDE diagnostics with fixes
- **Flowthru.FUnit** / **Flowthru.FUnit.SourceGenerators** / **Flowthru.FUnit.CodeFixes** — the functional unit-test library
- **Flowthru** — the umbrella package that bundles Core for downstream consumption

FUnit is part of Core (not a satellite) because rapid test feedback on Step logic *is* part of the design-time experience — not a separate phase. The compiler, Roslyn analyzers, and FUnit are the three mechanisms by which Core delivers design-time guarantees: catching what types can express, what the language can't quite reach, and what only execution against representative inputs can reveal. In a production CI loop, FUnit tests are confirmed green before any Flow ever reaches pre-flight.

## The Core Discipline

Two principles govern every Core change. They are in tension by design — and that tension is what Core's existence resolves.

### 1. Treat Core as a Haskell / Hackage production project

FP rigor — precise types, closed sums, applicative/monadic composition, structured failure-as-values — is the *primary mechanism* by which Flowthru delivers:

- **Design-time guarantees** the type system enforces before any IL runs
- **Pre-flight parsing and validation** that turns environmental concerns into structured `Validated<TError, TValue>` results
- **The commitments extension authors must fulfill** when closing slices of the [[Extension surface]] — broadcast through interfaces and abstract types whose implementations the compiler then checks

Soft FP corrodes these guarantees. A `Maybe<T>` quietly replaced with a nullable `T?`, an exhaustive `Match` quietly replaced with a `switch` plus a `default` clause — each erodes the design-time safety promise. Core code should look more like Haskell or modern Scala than like idiomatic mid-2010s C#.

### 2. Don't let that rigor leak

Flow and Catalog Developers must keep the experience they're promised: a ceremony-free, understandable, rapidly iterable [[API Surface]] and an [[Error Surface]] biased toward fast-acting design-time errors. Closed sums, Kleisli arrows, applicative combinators are *internal* vocabulary. The public surfaces Flow / Catalog Developers touch should look like ordinary modern C# — records, attributes, type-parameterized factories.

The two principles work in concert: Core uses FP precisely *so that less precision is required of the developer surface*. Slack in one shows up as ceremony in the other.

## Source Generators and Code Fixes

Source generators and code fixes are Core concerns, not a specialty:

- **Source generators** cover gaps in C#'s language that FP-in-types can't reach — emitting marker interfaces from `[FlowthruSchema]` that gate serializer compatibility, generating Step metadata for caching, generating Python interop bindings, etc. Reach for FP/Prelude first; reach for source generation when the language won't let you.
- **Code fixes** are how design-time errors become *actionable*. Because Flowthru surfaces more breaking errors earlier than runtime-only frameworks, every Roslyn diagnostic Core emits should ship with a code fix: "this is wrong" *and* "here's how to fix it." Without the fix, the early error is a tax; with the fix, it's a feature.

Every new analyzer or source-generator diagnostic ships with its companion code fix as part of the same change.

## Engine Logging

Engine components (`FlowthruService`, `ParallelFlowScheduler`, future schedulers) take `ILogger` (non-generic) as a constructor dependency. `AddFlowthru` registers a singleton resolved as `loggerFactory.CreateLogger("Flowthru")`, so the engine and every step share **one logger identity** under the single category `Flowthru` (ADR-0005). Lifecycle, pre-flight, and cache-decision logs go out through that shared logger. The `Microsoft.Extensions.Logging.Abstractions` dependency is the only logging coupling Core carries — concrete providers come from the host via `services.AddLogging(...)`. Hosts that don't register a factory get the `NullLogger` fallback wired by `AddFlowthru` and see no output.

`FlowthruActivitySource` still emits trace spans (`flowthru.run`, `flowthru.preflight`, `flowthru.step`) for OpenTelemetry and other distributed-tracing consumers — that was always activities' real job. The CLI-side `FlowthruActivityLogger` bridge that translated those activities into log lines has been retired; see [.claude/docs/adr/0006-engine-logs-directly-retire-activity-bridge.md](/.claude/docs/adr/0006-engine-logs-directly-retire-activity-bridge.md). The step-side convention (`Create(ILogger)`) lives in [/examples/CONTRIBUTING.md](/examples/CONTRIBUTING.md) and [.claude/docs/adr/0005-step-logging-via-shared-ilogger.md](/.claude/docs/adr/0005-step-logging-via-shared-ilogger.md).

## Prelude

`Flowthru.Prelude` houses the FP primitives the rest of Core builds on — `FlowIO<T>`, `EffResult<A>`, `Validated<TError, TValue>`, `FlowUnit`, `FlowResource`, plus the `IFlowResource` interface.

The contents are vendored and adapted from [LanguageExt v5](https://github.com/louthy/language-ext). License acknowledgement: `Flowthru.Core/Prelude/LICENSE-LanguageExt.md`. Think of Prelude as Flowthru's equivalent of a Haskell `Prelude` module or LanguageExt's own core — the FP helpers that everything else builds on.

There is no formal graduation criterion for what earns a place in Prelude; the LanguageExt provenance is the practical guide. When in doubt about whether to add something, default to keeping it *out* of Prelude — small Prelude is the right Prelude.

## Glossary

### Roles

**Core Developer**: The role that curates Flowthru's core library — the [[API Surface]] for Flow/Catalog developers and the [[Extension surface]] for Extension Developers. Designs the core abstractions (FlowBuilder, IStepNode, IItem, Catalog, Prelude types), the Roslyn surface (source generators + code fixes), the functional unit-test library (FUnit), and the error-phase machinery.

**Responsibilities:**
- Keep the [[API Surface]] small, expressive, and ceremony-free for Flow and Catalog Developers
- Keep the [[Error Surface]] biased toward [[Design-time error|design-time]] and [[Pre-flight error|pre-flight]] — minimize [[Runtime error|runtime]] failures
- Provide clear extension points for Extension Developers; ship Core changes that fail dependent extensions at compile time, not runtime
- Reason in correct FP terms ([[Closed sum]], [[Kleisli arrow]], [[Combinator]], [[Applicative vs monadic composition]]) — the architecture's correctness depends on it
- Ship every new Roslyn diagnostic with a companion code fix

_Avoid_: framework maintainer (Core Developer is the specific Flowthru role)

### Core Developer Vocabulary

**DAG**: The bipartite arrow/place graph the engine schedules. Items are *places* — named typed objects (`IItem` : `INode`). Steps are *arrows* — Kleisli arrows of `FlowIO` (`IStepNode` : `INode`), of shape `Func<TIn, FlowIO<TOut>>`. Bipartite means arrows compose only through places, which is what makes the graph a category in the formal sense.
_Avoid_: pipeline, workflow graph
*Note*: "category" here is the formal mathematical sense (objects + morphisms); the Catalog Developer entry for [[Data category]] uses "category" in the classification sense. Context disambiguates.

**Kleisli arrow**: A function of shape `A → M<B>` for some monad `M`. In Flowthru, `IStepNode<TIn, TOut>.Transform` is `Func<TIn, FlowIO<TOut>>` — a Kleisli arrow of the `FlowIO` monad. The user-supplied pure `A → B` from a `[FlowthruStep]`-attributed `Create()` is lifted into this shape at flow-construction time, which is why composition through the engine has clean monadic semantics rather than ad-hoc plumbing.
_Avoid_: transform (correct at the user surface; in Core the precise term is Kleisli arrow), callback, handler

**Combinator**: A method that composes values of a type without exposing the variants. On a closed sum, combinators (`Map`, `Bind`, `Zip`, `Select`, `SelectMany`) chain operations on the contained value while staying inside the same closed sum. To exit the closed sum and produce a non-closed-sum result, use `Match` instead.
_Avoid_: helper method, fluent API

**Applicative vs monadic composition**: Two ways to chain operations on a closed sum. *Applicative* (`Zip`, `ZipAll`) accumulates errors from all branches — for pre-flight validation where every problem should surface at once. *Monadic* (`Bind`, LINQ `from`/`in`/`select`) short-circuits on the first error — for operations that genuinely depend on earlier success. Choose by asking: are these checks independent (applicative) or dependent (monadic)?
_Avoid_: parallel vs sequential
