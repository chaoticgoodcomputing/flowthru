# Flowthru Diagnostics

This directory documents every diagnostic ID emitted by Flowthru's analyzers
and source generators. Each ID has its own `FT____.md` page describing what
triggers it, why the rule exists, the minimal fix, and any auto-fix available.

## Diagnostic taxonomy

Flowthru uses a single `FT` prefix with reserved ranges. Each range maps to
a layer of the architecture, which in turn maps to the audience most likely
to encounter it.

| Range      | Layer                       | Audience                  | Default severity |
|------------|-----------------------------|---------------------------|------------------|
| `FT0xxx`   | Algebra (`Flowthru.Prelude`) | All contributors          | Error            |
| `FT1xxx`   | Capability shape            | Extension Developers      | Error            |
| `FT2xxx`   | Host satisfaction           | Host integrators          | Error            |
| `FT3xxx`   | Pre-flight algebra          | Core / Extension Developers | Error          |
| `FT4xxx`   | Runtime ADT                 | Core / consumers          | Error            |
| `FT5xxx`   | Test trenchcoat (FUnit)     | Test authors              | Warning          |

The range a diagnostic falls into tells the developer their role at the
moment the diagnostic fires, before they even read the message:

- `FT2xxx` — you are configuring the host. A required capability isn't
  registered.
- `FT3xxx` — you are writing pre-flight logic. Validation is being misused.
- `FT5xxx` — you are in test code. Coverage or stub configuration is missing.

## Conventions

- **One ID, one rule.** Diagnostic IDs are stable identifiers; once
  published, an ID never changes meaning. New rules get new IDs in the
  appropriate range.
- **Severity defaults are by range.** Per-diagnostic severity overrides are
  the exception, not the norm. `FT0xxx`–`FT4xxx` are errors because the
  algebra and runtime contracts are non-negotiable; `FT5xxx` is a warning
  because FUnit is opt-in via `#if FUNIT_ENABLED` and missing test coverage
  is a quality concern, not a correctness one.
- **Every ID maps to a docs page.** When you add a new diagnostic, add the
  corresponding `docs/diagnostics/FT____.md` page in the same change. A
  build-time analyzer will (in Phase 3) fail the build if a new ID lacks
  its docs page.
- **Help links point here.** Each `DiagnosticDescriptor` sets
  `helpLinkUri` to the corresponding docs page so IDE quick-fixes can take
  the user directly to the explanation.

## Per-page format

Each `FT____.md` page follows this template:

```markdown
# FTxxxx — Short title

**Severity:** Error | Warning | Info
**Category:** Algebra | Capability | Host | Pre-flight | Runtime | Test
**Introduced:** Phase X

## What triggers this

(One-paragraph description of the code shape that produces the diagnostic.)

## Why this rule exists

(One or two paragraphs explaining the FP concept or invariant being enforced,
and the failure mode the rule prevents.)

## How to fix

(Concrete code-level guidance. Show before/after if useful.)

## Auto-fix

(If a code-fix provider exists for this diagnostic, document it here.
Otherwise: "No auto-fix; manual change required.")

## Related diagnostics

(Cross-links to siblings in the same range or related rules.)
```

## Range-by-range conceptual map

### `FT0xxx` — Algebra

Rules that enforce the integrity of the FP foundations in
`Flowthru.Prelude`: that `Eff` values aren't discarded, that `RuntimeError`
remains a closed sum, that pattern-matching against `RuntimeError` /
`PreFlightError` / `EffResult` / `Validated` covers every case.

### `FT1xxx` — Capability shape

Rules that enforce the structural correctness of capability declarations
and interpreter implementations: that types marked `[FlowthruInterpreter]`
implement the capability they claim, that capability traits are sealed
correctly, that the Conformance Kit pattern is followed.

### `FT2xxx` — Host satisfaction

Rules that fire at host configuration sites: a flow declares a
`[RequiresCapability]` (or, equivalently, a `Has<TRuntime, TCapability>`
constraint) that the host's registered runtime doesn't satisfy. The C#
generic constraint solver does most of this work natively; analyzers in
this range produce the friendly error message and migration hints.

### `FT3xxx` — Pre-flight algebra

Rules that enforce correct use of `Validated<E, T>` in pre-flight code:
no thrown exceptions in `[PreFlightCheck]` methods, no discarded
`Validated` results, no monadic short-circuit where applicative
accumulation is expected.

### `FT4xxx` — Runtime ADT

Rules that enforce correct handling of `RuntimeError` in runtime code:
exhaustive pattern matching, no silent `catch {}` blocks in `Eff` lift
sites, the `RuntimeError` hierarchy stays closed.

### `FT5xxx` — Test trenchcoat (FUnit)

Rules that enforce FUnit's coverage and configuration contracts: every
`[FlowthruStep]` has at least one `[StepTest]`, every
`[FlowthruInterpreter]` has at least one `[InterpreterTest]`,
`[FUnitStubContainer]` registrations match the capabilities flows under
test require. Replaces the legacy `FU0xx` / `FU1xx` ranges from the
pre-rewrite FUnit.

## Currently active IDs

No diagnostics are active yet. Phase 1 of the FP rewrite will introduce
the first IDs; this list will be kept in sync from that point forward.

| ID | Title | Introduced |
|----|-------|------------|
| _(none yet)_ | | |
