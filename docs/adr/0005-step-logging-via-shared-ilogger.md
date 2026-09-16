# `ILogger` declared on `Create()` is the canonical step-logging surface

Steps that need logging declare `ILogger` (non-generic) as a parameter on their `Create()` factory, resolved via the existing `[FlowthruStep]` source generator's interface-typed-parameter → `ServiceRef` mechanism ([StepMetadataGenerator.cs:149–167](/src/core/Flowthru.Core.SourceGenerators/Step/StepMetadataGenerator.cs#L149-L167)). `AddFlowthru` registers a singleton `ILogger` resolved as `loggerFactory.CreateLogger("Flowthru")`, so the engine internals (`FlowthruService`, `ParallelFlowScheduler`) and every step share **one logger identity** under the single category `Flowthru`. Hosts that haven't called `AddLogging()` see a `NullLogger` via `AddFlowthru`'s `TryAdd<NullLoggerFactory>` fallback; calls are silently dropped, the run still succeeds.

We considered four alternatives and rejected each:

- An ambient `StepContext` threaded into `step.Execute()` — breaks the canonical `Func<TIn, TOut>` transform shape and opens an unbounded context grab-bag.
- An `AsyncLocal`-backed `Logger` static emitted onto each step via a new sourcegen companion — invisible mutable state, hard to test, surprising under parallel scheduling.
- `ILogger<TStep>` for per-step categorization — the .NET-idiomatic shape, but C# disallows static classes as generic type arguments (CS0718). Adopting it would have forced every step that declares a logger from `static class` to `sealed class` with a private ctor, a large public-API migration for marginal benefit (per-step `appsettings.json` filtering).
- `ILoggerFactory` instead of `ILogger` as the default — preserves the static-class step shape but adds two lines of `.CreateLogger(...)` boilerplate per step. Available as the opt-in escape hatch for hosts that *do* want per-step categorization: take `ILoggerFactory` in `Create()` and call `factory.CreateLogger<NonStaticMarker>()` or `factory.CreateLogger(typeof(MyStep).FullName!)`.

The declared-`ILogger` approach reuses machinery that already exists, keeps step dependencies in the type signature per Flowthru's discipline, preserves `static class` as the canonical step shape, and satisfies the original ask literally: "a single `ILogger` available to both internals and Steps." The convention is documented in `examples/CONTRIBUTING.md` and demonstrated in the example projects rather than enforced by a Roslyn analyzer — code-as-documentation is the chosen surface.

## Governed code

- `src/core/Flowthru.Core/Hosting/ServiceCollectionExtensions.cs` — singleton `ILogger` registration with `NullLoggerFactory` fallback
- `src/core/Flowthru.Core/Flow/ParallelFlowScheduler.cs` — scheduler accepts and logs through the shared `ILogger`
- `src/extensions/Flowthru.Extensions.Python/Hosting/PythonFlowthruBuilderExtensions.cs` — mirrors the shared `ILogger` registration for standalone `UsePython()` usage
- `src/extensions/Flowthru.Extensions.Python/Step/Python/Internal/SubprocessPythonExecutor.cs` — bridges worker stderr through the shared `ILogger`
- `src/extensions/Flowthru.Extensions.Python/Step/Python/Internal/StderrLineClassifier.cs` — per-line decision point for the stderr-to-`ILogger` bridge
- `src/extensions/Flowthru.Extensions.Python/build/flowthru_worker.py` — Python-side logging handler that emits JSON frames for the stderr bridge
- `tests/core/Flowthru.Core.Tests/Step/StepLoggerInjectionTests.cs` — end-to-end coverage for `Create(ILogger)` convention and shared-category contract
- `tests/extensions/Flowthru.Extensions.Python.Tests/SubprocessPythonExecutorBridgeTests.cs` — end-to-end coverage for the Python stderr bridge
