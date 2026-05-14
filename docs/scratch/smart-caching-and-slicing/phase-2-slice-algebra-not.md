# Phase 2 — `FlowSliceStrategy.Not` + `--exclude` Flag

> **Created:** 2026-05-13
> **Status:** Pending
> **Depends on:** —
> **Unblocks:** Phase 7 (slice-bounded cache invalidation).

## Motivation

Users today can say "run to step X" or "run only from item Y", but **not** "run to X excluding everything in flow Z." The slice algebra in [FlowSliceStrategy](../../../src/core/Flowthru.Core/Flow/FlowSliceStrategy.cs) is a closed sum supporting `From`, `To`, `Only`, `Flows`, `All`, `None`, `And`, `Or` — every set operation except complement and difference.

The CLI consequence: users wanting to exclude a subset must either invoke multiple times or hand-edit their flow registration. magic-atlas has hit this twice already — wanting to run a re-clustering pass while excluding the Ingest flow (which re-fetches HTTP resources every time).

## Scope

**In scope:**
- Add `Not` to the `FlowSliceStrategy` closed sum.
- Extend the CLI parser to accept `--exclude <patterns>` (repeatable, comma-separated, glob).
- Add label prefixes `flows:` and `tag:` to the matcher so users can write `--exclude flows:Ingest`.
- Update `ClosedSumExhaustivenessAnalyzer` expectations.

**Out of scope:**
- A full expression-grammar selector DSL (Dagster-style). Deferred per the plan README.
- Source-generated CLI parity. Deferred.
- Tag declarations on steps/items — tags as a first-class concept may already exist via attributes; this phase only adds the matcher prefix, not the storage. If tags don't yet exist as a step-author surface, restrict the prefix support to `flows:` for v1 and treat `tag:` as a Phase-2.5 follow-up.

## Current State

[FlowSliceStrategy.cs](../../../src/core/Flowthru.Core/Flow/FlowSliceStrategy.cs):
- Lines 45-91 define the closed sum: `From`, `To`, `Only`, `Flows`, `All`, `None`, `And`, `Or`.
- Line 47: `private FlowSliceStrategy()` — the sum is sealed; new cases must live in this file.
- Lines 139-219: the resolver `switch` enumerates every case; `ClosedSumExhaustivenessAnalyzer` enforces handling.

[ArgumentParser.cs:43-215](../../../src/core/Flowthru.Cli/ArgumentParser.cs#L43-L215):
- Flags `--from`, `--to`, `--only`, `--flow` are mapped to `FlowSliceStrategy` primitives.
- Multiple `--from A --from B` compose via `Or`; `--from A --to B` composes via `And`.
- Labels match step labels or item labels via glob (`*`, `?`).

## Design

### Algebra

```csharp
/// <summary>
/// Set complement — a step is included iff the inner strategy does
/// NOT include it. Composes with And/Or to express difference:
/// "to X but not in flows Y" → And(To(X), Not(Flows(Y))).
/// </summary>
public sealed record Not(FlowSliceStrategy Inner) : FlowSliceStrategy;
```

Resolver case (inside `ApplyToSet`):

```csharp
case Not n:
{
    var innerKeep = n.Inner.ApplyToSet(ctx);
    var allSteps = new HashSet<IStepNode>(ctx.OrderedSteps, ReferenceEqualityComparer.Instance);
    allSteps.ExceptWith(innerKeep);
    return allSteps;
}
```

A convenience constructor:

```csharp
public static FlowSliceStrategy Excluding(FlowSliceStrategy inner) => new Not(inner);
public static FlowSliceStrategy Difference(FlowSliceStrategy left, FlowSliceStrategy right) =>
    new And(left, new Not(right));
```

### CLI surface

| Flag | Semantics |
|---|---|
| `--exclude <patterns>` | Comma-separated label patterns. Multiple `--exclude` flags compose via `Or` inside a single `Not`. The whole selector composes as `And(rest, Not(union-of-excludes))`. |

Prefix syntax for the matcher (applies to `--exclude`, `--only`, `--from`, `--to`):
- Plain `name` → match step label or item label (existing behavior).
- `flows:Name` → match by flow label (resolves via `FlowSliceStrategy.Flows`).
- `tag:Name` → match by tag — deferred to Phase 2.5 if step tags aren't yet first-class.

Examples:
```
flowthru run --to CardEmbeddings --exclude flows:Ingest
flowthru run --from BuildTrainingPairs --exclude clean-customers,validate-*
flowthru run --exclude flows:Ingest,flows:Reporting
```

## Tasks

1. **`src/core/Flowthru.Core/Flow/FlowSliceStrategy.cs`** — Add `Not(FlowSliceStrategy Inner)` case. Add resolver `switch` case. Add `Excluding` and `Difference` static convenience constructors.

2. **`src/core/Flowthru.Core/Flow/FlowSliceStrategy.cs`** — Audit all `ApplyToSet` consumers (recursive calls in the `And`/`Or` cases) to confirm `Not` propagates correctly.

3. **`src/core/Flowthru.Core.SourceGenerators/Algebra/ClosedSumExhaustivenessAnalyzer.cs`** — Verify the analyzer picks up the new case automatically (it should — it walks the sealed hierarchy). Add a regression test that omitting `Not` from a switch over `FlowSliceStrategy` fires the diagnostic.

4. **`src/core/Flowthru.Cli/ArgumentParser.cs`** — Add `--exclude` flag parsing in [ArgumentParser.cs:65-157](../../../src/core/Flowthru.Cli/ArgumentParser.cs#L65-L157). Map to `Not(Or(...))` composition with the rest of the selector.

5. **`src/core/Flowthru.Cli/ArgumentParser.cs`** — Extend the matcher to recognize the `flows:` prefix: strip the prefix and dispatch to `FlowSliceStrategy.Flows(...)` instead of label glob.

6. **`src/core/Flowthru.Cli/ArgumentParser.cs`** — Update [ArgumentParser.cs:198-214](../../../src/core/Flowthru.Cli/ArgumentParser.cs#L198-L214) help text. Document `--exclude` and the `flows:` prefix.

7. **Tests:**
   - Unit: `FlowSliceStrategyTests` — `Not(Only(X))` on a 3-step flow returns the other two steps.
   - Unit: `Not(Not(X)) ≡ X` (double-complement law).
   - Unit: `Difference(All, X) ≡ Not(X)`.
   - Unit: `And(To(X), Not(Flows(Y)))` against a fixture flow.
   - Parser: `--to X --exclude flows:Y` parses to the expected strategy tree.
   - Parser: multiple `--exclude` flags compose via `Or` inside the `Not`.
   - CLI integration: a small flow with the `--exclude` flag end-to-end.

## Public Surface Changes

Additive:
- `FlowSliceStrategy.Not` case.
- `FlowSliceStrategy.Excluding(...)` and `.Difference(...)` static constructors.
- `--exclude` CLI flag.
- `flows:` matcher prefix.

No breaking changes. Existing flag combinations continue to work identically.

## Phase Placement (per CONTRIBUTING.md)

- **Compile-time:** `ClosedSumExhaustivenessAnalyzer` ensures every consumer of `FlowSliceStrategy` handles `Not`. The strategy tree is type-checked at the call site.
- **Pre-flight:** The selector resolves against the merged DAG before any step runs. An `--exclude` pattern matching nothing is a no-op (not an error) — same as today's behavior for `--from`/`--to` patterns that don't match.
- **Runtime:** No change.

## Testing Strategy

- Unit tests for the algebra in `tests/Flowthru.Core.Tests/Flow/FlowSliceStrategyTests.cs`.
- Parser tests in `tests/Flowthru.Cli.Tests/ArgumentParserTests.cs`.
- A `Verify`-style snapshot test for the help text update.
- Exhaustiveness analyzer regression test.

## Confirmation Criteria

- `nx run-many -t build` passes.
- `nx run affected -t test` passes; coverage on `FlowSliceStrategy.Not` reaches the project's standard threshold.
- `flowthru --help` output documents `--exclude` and `flows:` prefix.
- The KedroSpaceflights example accepts `flowthru run --to ReportingAggregations --exclude flows:DataIngest` and runs the expected subset.

## Risks

- **Pattern-prefix ambiguity:** a user's actual step label `flows:Ingest` (a colon in the name) would be misread. Mitigation: document the convention and reject step/item labels containing `:` at build time via a small analyzer addition. Worth a one-line FT-code diagnostic.
- **`Not(All)` resolves to `None`** and `Not(None)` to `All` — both correct and harmless, but worth a note in the resolver docstring.

## Follow-ups

- Phase 7 reuses the `--exclude` flag and `flows:` prefix for `flowthru cache invalidate`. No separate parser work needed.
- A future "selector grammar" RFC could fold `--exclude` into a single `--select '…'` expression. Deferred.
