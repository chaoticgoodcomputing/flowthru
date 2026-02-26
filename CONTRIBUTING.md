# Contributing to Flowthru

Flowthru is a type-safe data engineering framework for .NET. Its design philosophy can be summarized in one sentence: **errors should surface as close to compile-time as possible.**

This document explains the fail-fast architecture, how it is enforced, and how contributors should reason about where new error classes belong.

## The Three Error Phases

Every possible failure in a Flowthru pipeline falls into one of three phases, ordered from most desirable to least:

### 1. Build / Compile-Time

Errors caught by the C# compiler before a binary is ever produced. This is the ideal — a misconfigured pipeline should not compile.

| Mechanism | What it catches |
|---|---|
| **Generic `AddNode<TInput, TOutput>`** | Node I/O types must match catalog entry types. Wiring `Func<IEnumerable<A>, Task<IEnumerable<B>>>` to an `ICatalogEntry<IEnumerable<C>>` is a compiler error. |
| **Source-generated `AddNode` overloads (1×1 through 8×8)** | Multi-input/output tuple arity and element types are enforced structurally. Wrong count or wrong type → compiler error. |
| **`[FlowthruSchema]` source generator** | Emits `IFlatSchema`/`INestedSchema` and serialization marker interfaces. Using a nested schema with a CSV serializer (which requires `ITextSerializable`) fails at compile-time. Diagnostics FT1001 (missing `partial`) and FT1002 (conflicting manual interfaces) are build errors/warnings. |
| **`ComposedStorageAdapter<TContainer, TRow>` generic constraints** | The `TRow` parameter threads through medium, format, and container layers. Mismatched row types across layers are compiler errors. |
| **Catalog entries as typed properties** | `catalog.Companies` is an `ICatalogEntry<IEnumerable<CompanySchema>>` property — not a string key lookup. Typos and missing entries are `CS1061` errors. |
| **`required` modifier on schema properties** | Missing fields in record initialization are compiler errors. |

**Kedro contrast:** In Python's Kedro, nodes and catalog entries are connected by string names (`"companies"`). Type mismatches, misspelled dataset keys, and wrong output counts are all runtime errors. Every mechanism above replaces a Kedro runtime failure with a C# compile-time failure.

### 2. Pre-Flight Checks

Errors caught at runtime, but **before any node executes**. These validate the environment — things the compiler cannot check (file existence, schema drift in external data, DAG structure from dynamic registration).

| Mechanism | What it catches |
|---|---|
| **`Pipeline.Build()` → `DependencyAnalyzer`** | Single producer rule (two nodes writing the same entry → `InvalidOperationException`). Circular dependency detection via topological sort. |
| **`Pipeline.ValidateExternalInputsAsync()`** | Inspects Layer 0 (external) inputs before execution. `IShallowInspectable`: sample N rows, validate file existence, headers, and schema. `IDeepInspectable`: full dataset scan. Configurable per-entry or per-pipeline via `ValidationOptions`. |
| **Dry run mode** (`ExecutionOptions.DryRun`) | Runs all pre-flight checks (build + validation) without executing any node. Returns success/failure with zero side effects. |

**Design invariant:** A pipeline that passes pre-flight checks should always complete successfully. If it doesn't, that's a bug in Flowthru — either a missing pre-flight check or a missing compile-time constraint.

### 3. Runtime

Errors that occur during actual node execution. These should be limited to truly unpredictable failures — network drops, OOM, bugs in user-authored transform logic.

| Mechanism | What it handles |
|---|---|
| **`FlowIO<T>` effect type** | All I/O is lazy and explicit. `Load()` returns `FlowIO<T>`, not `T` — side effects cannot be accidentally dropped. Cancellation is baked in. Typed error recovery via `Catch<TException>()`. |
| **`CatalogEntry<T>.SaveUntyped()` guard** | Runtime type check at the type-erasure boundary (`ICatalogEntry` → `ICatalogEntry<T>`). Produces a descriptive `FlowIO.Fail` on mismatch. |
| **Node-level error isolation** | Each node executes in a try/catch. On failure, execution halts and returns `PipelineResult.CreateFailure()` with the failing node's result and exception. |

## Decision Rules for Contributors

When adding a new feature or fixing a bug, use these rules to determine where validation belongs:

1. **Can the C# type system express this constraint?** → Add it as a generic constraint, source generator diagnostic, or interface requirement. The compiler is the first line of defense.

2. **Is it an environmental concern (files, connections, external schemas)?** → Add it to the pre-flight validation layer. Implement `IShallowInspectable` or `IDeepInspectable` on the relevant storage adapter. It must run before any node executes.

3. **Is it truly unpredictable (network failure, user logic bug)?** → Handle it in the runtime layer via `FlowIO` effects. Ensure the error is captured in `NodeResult`/`PipelineResult`, not swallowed.

4. **If you're unsure**, err toward earlier. A compile-time constraint that's slightly restrictive is better than a runtime error that's slightly permissive.

### Anti-Patterns

- **String-based lookups for catalog entries.** Catalog entries are typed properties. If you find yourself resolving entries by name at runtime, redesign the API.
- **Unchecked I/O outside `FlowIO`.** All load/save/exists operations must return `FlowIO<T>`. Raw `File.ReadAllText()` in a node or adapter is a bug.
- **Validation during execution.** If a check can run before the first node, it belongs in pre-flight — not inside a node's transform function.
- **Swallowing exceptions.** Node failures must propagate to `PipelineResult`. Silent `catch {}` blocks hide errors that should halt the pipeline.

## Testing the Guarantees

The test suite validates each error phase:

- **Compilation tests** (`Category=Compilation`): Verify that source generators emit correct interfaces and diagnostics. Test that malformed schemas produce build errors.
- **Pre-flight tests**: Verify that `Pipeline.Build()` rejects duplicate producers and cycles. Verify that `ValidateExternalInputsAsync()` catches missing files and schema mismatches.
- **Integration tests**: Run full pipelines end-to-end against known-good data. A passing pre-flight must always lead to a successful run.

When adding a new error-phase mechanism, add tests that verify:
1. The error is caught in the correct phase (not later).
2. The error message identifies the problem clearly (entry name, expected vs actual type, file path).
3. The pipeline does not partially execute before surfacing the error (for pre-flight checks).
