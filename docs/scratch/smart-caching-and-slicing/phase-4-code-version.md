# Phase 4 — `CodeVersion` via `StepMetadataGenerator` + Python Extension

> **Created:** 2026-05-13
> **Status:** Pending
> **Depends on:** —
> **Unblocks:** Phase 6 (composite cache key requires step `CodeVersion`).

## Motivation

For the cache plan to detect that a step's code has changed — and thus its previously-cached output is stale — every step needs a stable, framework-derived identity. Per the locked design:

- C# step authors write zero cache-related code. The existing source generator emits the identity as a constant.
- Python step extensions stamp the identity at startup from the `.py` source content + interpreter version + dependency manifest.
- Any future step extension follows the same contract: provide a `CodeVersion`, or steps from your extension are always-stale (fail-safe).

Without this, a code edit to a step's transform produces the **silent staleness bug**: input file unchanged → fingerprints match → cached output present → cache hit returns data the new code would have produced differently. This is exactly the kind of "looks correct, fails late" failure CONTRIBUTING.md commits to eliminating.

## Scope

**In scope:**
- Extend [StepMetadataGenerator](../../../src/core/Flowthru.Core.SourceGenerators/Step/StepMetadataGenerator.cs) to compute a per-step source-text hash at compile time and emit it on the generated `_Metadata` record.
- Expose `CodeVersion` on `IStepNode` (read-only) so the cache plan can access it.
- Allow author override via `[FlowthruStep(CodeVersion = "v2")]` for cases where the author wants to invalidate cache for a semantic-only rewrite, or preserve cache across a cosmetic rewrite.
- Python extension: compute and stamp `CodeVersion` for `PythonStep<TIn, TOut>` at construction from the `.py` file + interpreter version + `requirements.txt` hash.

**Out of scope:**
- The cache plan itself (Phase 6).
- Composite hash composition (Phase 6).
- Future non-C#-non-Python step extensions — design contract is documented; implementation is per-extension.

## Current State

[StepMetadataGenerator](../../../src/core/Flowthru.Core.SourceGenerators/Step/StepMetadataGenerator.cs):
- Walks types annotated with `[FlowthruStep]`.
- Emits `{StepClassName}_Metadata` records with traits (`IsIdempotent`, `HasSideEffects`).
- Already has access to the syntax tree and semantic model for the step class.

[IStepNode](../../../src/core/Flowthru.Core/Step/IStepNode.cs):
- Already carries `NodeTraits Traits` and `IReadOnlyList<ServiceRef> ServiceDependencies`.

[FlowthruStepAttribute](../../../src/core/Flowthru.Core/Step/FlowthruStepAttribute.cs) — needs verification of the current attribute surface for adding a `CodeVersion` property.

## Design

### Identity computation (C#)

The source generator computes a hash from:

1. **Step type's source text** — the syntax-tree text of the class declaration, normalized (whitespace-stripped, comments-stripped).
2. **Transitive type symbol references** within the step's transform method — for v1, limited to types declared in the same compilation. Defer cross-assembly transitive hashing.
3. **`[FlowthruStep(CodeVersion = …)]` override** — if present, takes precedence over the computed hash entirely.

Emitted as:

```csharp
// Source-generated, do not edit
partial class MyStep
{
    public const string CodeVersion = "sha256:abc123…";  // 16-char prefix, sufficient for cache-key uniqueness
}
```

The step's `_Metadata` record exposes this as a property:

```csharp
public sealed record MyStep_Metadata(
    bool IsIdempotent,
    bool HasSideEffects,
    string CodeVersion);
```

`IStepNode` gains a read-only property:

```csharp
public interface IStepNode
{
    // ... existing surface
    string? CodeVersion { get; }
}
```

`Step<TIn, TOut>` ([Step.cs](../../../src/core/Flowthru.Core/Step/Step.cs)) returns `null` by default; source-generated steps return their constant. Null means "always stale" — fail-safe for any step the framework can't identify.

### Identity computation (Python)

The Python step extension owns `CodeVersion` derivation. Per [Flowthru.Extensions.Python](../../../src/extensions/Flowthru.Extensions.Python):

```csharp
internal static string ComputePythonCodeVersion(
    string scriptPath,
    string interpreterPath,
    string? requirementsPath)
{
    using var sha = SHA256.Create();
    sha.AppendBytes(File.ReadAllBytes(scriptPath));
    sha.AppendBytes(File.ReadAllBytes(interpreterPath));  // captures interpreter version
    if (requirementsPath is not null && File.Exists(requirementsPath))
        sha.AppendBytes(File.ReadAllBytes(requirementsPath));
    return $"py-sha256:{Convert.ToHexString(sha.GetCurrentHash())[..16]}";
}
```

`PythonStep<TIn, TOut>` captures this at construction time. Pure delegation: the Python extension follows the same surface (`IStepNode.CodeVersion`) the C# generator does.

### Override semantics

```csharp
[FlowthruStep(Label = "compute-totals", CodeVersion = "v2")]
public sealed partial class ComputeTotalsStep : FlowthruStep<Customer, OrderTotal>
{
    // ...
}
```

When present, the override **replaces** the source-derived hash entirely. Authors use this for:
- Semantic rev: "I refactored this without changing inputs/outputs; bust the cache anyway."
- Cosmetic rev: pin `CodeVersion = "v1"` across a whitespace-only rewrite to preserve cache.

## Tasks

1. **`src/core/Flowthru.Core.SourceGenerators/Step/StepMetadataGenerator.cs`** — Extend to compute source-text SHA256 per step. Honor `[FlowthruStep(CodeVersion = …)]` override.

2. **`src/core/Flowthru.Core/Step/FlowthruStepAttribute.cs`** — Add `string? CodeVersion { get; init; }` property if not present.

3. **`src/core/Flowthru.Core/Step/IStepNode.cs`** — Add `string? CodeVersion { get; }` to the interface (default body `null` for non-breaking).

4. **`src/core/Flowthru.Core/Step/Step.cs`** — Implement `CodeVersion` accessor: if construction received a non-null `codeVersion` parameter (added via the generated `FlowBuilder.AddStep` overloads), expose it. Otherwise null.

5. **`src/core/Flowthru.Core.SourceGenerators/Flow/FlowBuilderGenerator.cs`** — Update generated `AddStep<TIn, TOut>` overloads (75 of them, per the existing precedent) to thread the source-generated `CodeVersion` through to the `Step<TIn, TOut>` constructor.

6. **`src/extensions/Flowthru.Extensions.Python/`** — In the `PythonStep<TIn, TOut>` constructor (or its factory), compute `CodeVersion` from `.py` script + interpreter + requirements. Stamp on the `IStepNode` surface.

7. **Test fixtures:**
   - Existing example projects (KedroSpaceflights, etc.) — verify the generator emits `CodeVersion` constants without runtime impact.
   - A diagnostic test: changing the body of a sample step's transform produces a different `CodeVersion`; changing only whitespace produces the same one (post-normalization).

8. **Tests:**
   - Source-generator output snapshot: a sample step compiles to a `_Metadata` record with the expected `CodeVersion` shape.
   - `[FlowthruStep(CodeVersion = "v2")]` override is honored verbatim.
   - Python: changing the `.py` file changes `CodeVersion`; changing `requirements.txt` changes it; changing nothing keeps it stable.
   - Integration: a sample flow's `IStepNode.CodeVersion` is non-null for source-generated and Python steps; null for hand-constructed `Step<...>` instances (the fail-safe path).

## Public Surface Changes

Additive:
- `IStepNode.CodeVersion` (nullable, default null — non-breaking).
- `[FlowthruStep(CodeVersion = …)]` attribute parameter (optional).
- `MyStep.CodeVersion` constant on source-generated step partials.

No breaking changes. Existing hand-constructed steps continue to compile and behave identically; they're simply uncacheable.

## Phase Placement (per CONTRIBUTING.md)

- **Compile-time:** Source generator emits the constant. Diagnostic if a step has both `[FlowthruStep(CodeVersion = …)]` and `partial` overrides of the generated constant.
- **Pre-flight:** Cache plan reads `IStepNode.CodeVersion` to compose the per-step composite hash (Phase 6).
- **Runtime:** No participation.

## Testing Strategy

- Source-generator tests follow existing patterns in `tests/Flowthru.Core.SourceGenerators.Tests/`.
- Python extension tests use the existing Python-step harness; add fixtures with toy `.py` files.
- Hand-constructed `Step<...>` instance returns null for `CodeVersion` — covered by unit test on `Step` constructor.

## Confirmation Criteria

- `nx run-many -t build` passes.
- `nx run affected -t test` passes; source-generator snapshot tests verify expected `CodeVersion` shape.
- An end-to-end test demonstrates: edit a step's body → `CodeVersion` changes; revert → `CodeVersion` returns to original; add whitespace only → `CodeVersion` unchanged.
- Python extension: edit the `.py` file → `CodeVersion` changes.

## Risks

- **Source-text hashing is fragile to comment changes** unless normalized. Mitigation: strip comments and trivia before hashing. Edge case: docstring changes won't bust cache, which matches user intent.
- **Cross-assembly type-symbol changes** aren't captured in v1. A step that calls `OtherAssembly.Helper` won't notice when `Helper` changes. Mitigation: documented limitation; users with such dependencies set `CodeVersion` manually when they bump the dependency, or rev the assembly version (which we can hash alongside source — defer until needed).
- **Override misuse** — a developer writes `CodeVersion = "v1"` and forgets to bump it after a semantic change → silent staleness returns. Mitigation: document this as the author's explicit responsibility when overriding. The override exists *because* the framework can't always guess; with great power, etc.
- **Python interpreter binary hashing** is potentially slow for large `python.exe` files. Mitigation: hash interpreter binary's `Version` info string + path instead of the full binary if perf becomes an issue.

## Follow-ups

- Phase 6 consumes `CodeVersion` to compose composite cache keys.
- A future "transitive code identity" RFC could expand the C# generator to follow type references across compilation units. Defer.
