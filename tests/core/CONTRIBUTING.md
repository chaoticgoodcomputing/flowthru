# Contributing to Flowthru Core Tests

This document is for **Core Developers writing tests** for any of the `src/core/` packages — Flowthru.Core, Flowthru.Cli, Flowthru.FUnit, and their Roslyn surface (`*.SourceGenerators`, `*.CodeFixes`).

**Audience scope:** assumes familiarity with [/src/core/CONTRIBUTING.md](/src/core/CONTRIBUTING.md) (Core Developer conventions and vocabulary) and the upstream contexts it points to.

See [/CONTRIBUTING.md](/CONTRIBUTING.md) for cross-cutting design rules (the three error phases, decision rules for where validation belongs), [/src/core/CONTRIBUTING.md](/src/core/CONTRIBUTING.md) for the Core engineering discipline, and [/tests/extensions/CONTRIBUTING.md](/tests/extensions/CONTRIBUTING.md) for the extension-testing context (which inherits much of the same vocabulary).

## Test Categories Map to Error Phases

Test categories in `tests/core/` mirror the three error phases the framework promises to enforce. Each category exists to *prove* the phase's enforcement works:

- **[[Design-time test]]s** verify that the type system, source generators, Roslyn analyzers, and code fixes correctly catch incorrect code patterns before any IL runs. Many use `Flowthru.Tests.Helpers` (`NUnit4Verifier`, `CodeFixTestHelper`) to drive Roslyn against snippets and assert on emitted diagnostics or fix-applied output.
- **[[Pre-flight test]]s** verify that environmental and structural errors are caught before any Step executes — DAG cycles, duplicate producers, missing external inputs, schema drift. Collectively, pre-flight tests uphold the framework's design invariant: *any Flow that passes pre-flight must complete successfully*. When a pre-flight test passes but a Flow then fails at runtime, the diagnostic question is *"is this an environmental concern (incomplete pre-flight check) or a type-level constraint (should have been design-time)?"*
- **[[Runtime test]]s** capture user-reported runtime errors as a *staging ground*. They are not a permanent home — every test here should leave the category quickly: fixed and moved to mirror or example-test coverage, or hoisted to pre-flight / design-time where the check should have lived. Goal: this category stays empty.

## Architecture Tests

[[Architecture test]]s enforce structural invariants across project seams — things like "every closed sum has a private constructor and sealed nested variants," "every extension's namespace mirrors the structure of the surface it extends," "the workspace's namespace layout matches the source-tree layout." They live in `Flowthru.Core.Architecture.Tests` and reflect over assemblies rather than executing pipeline logic.

The pattern is powerful because it codifies conventions the codebase relies on that the compiler can't directly enforce. When you catch a structural invariant during code review ("this should have been a closed sum," "this namespace should mirror its source"), don't rely on review alone — codify it as an architecture test.

Current architecture tests: `ClosedSumStructureTests`, `ExtensionNamespaceMirrorTests`, `ExtensionPointStructureTests`, `NamespaceLayoutTests`. Add more when a structural invariant matters enough to fail CI on drift.

## Laws Kits — Design Ownership

Every Core [[Extension surface]] that admits multiple implementations gets a [[Laws kit]] — an abstract test base class in `Flowthru.Tests.Kits` that codifies the behavioral contract every implementation must satisfy. When you add a new extension surface to Core, **ship the matching laws kit base in the same PR.** The kit base is the test-time equivalent of the surface's interface contract; without it, extensions drift behaviorally even when they all compile against the same interface.

The laws kit *base* is a Core concern (designed here). The conformance *subclasses* are extension concerns (live under `tests/extensions/<Extension>/`). See [/tests/extensions/CONTRIBUTING.md](/tests/extensions/CONTRIBUTING.md) for the subclassing convention and the in-flight rename from `*Conformance` → `*Laws` (issue [#23](https://github.com/chaoticgoodcomputing/flowthru/issues/23)).

## FUnit in tests/

FUnit does not appear in `tests/`. NUnit is the standard for every test in this repository, including `Flowthru.FUnit.Tests` — which tests the FUnit library *code* using NUnit.

FUnit is a *downstream-user* library: it gives Flow Developers writing Steps a rapid TDD loop on their Step logic, contributing to the [[Design-time error]] phase from outside the framework. Conflating "FUnit is part of Core" (true — see [/src/core/CONTRIBUTING.md](/src/core/CONTRIBUTING.md)) with "FUnit is what we use in tests/" (false) leads to the wrong mental model. Core ships and tests FUnit; tests/ doesn't consume it.

## Coverage Workflow

`nx run FlowthruCoverage` compiles coverage across all tests, for each `src/` library, into the [[Example test]] pipeline at `examples/advanced/FlowthruCoverage/`. The pipeline outputs analyses to `examples/advanced/FlowthruCoverage/Data/_04_Reporting/Datasets/`; the [Reporting Catalog](/examples/advanced/FlowthruCoverage/Data/_04_Reporting/Catalog.Reporting.cs) is the canonical entry point for "what analyses are available for answering coverage questions." Review it when looking for gaps — both gaps in your library's coverage *and* gaps in what coverage questions Flowthru itself can answer.

### Kit-first thinking

When you find that current coverage is inadequate, ask two questions:

1. **What individual tests would cover me?** The obvious question.
2. **Can the [[Laws kit]] system be expanded so other extensions also benefit from this coverage?** The leverage question.

The kit system is the leverage point. Coverage gaps caught and addressed at the kit level benefit every implementation of the surface, not just yours. Kit-first thinking applies to all test authors — Core, Extension, anywhere — because the kit is shared infrastructure any test author can extend.

## Glossary

### Roles

This context's audience is the Core Developer — see [/src/core/CONTRIBUTING.md](/src/core/CONTRIBUTING.md) for the role definition and responsibilities.

### Tests/core Vocabulary

**Architecture test**: A test that enforces a structural invariant across the codebase — type relationships, namespace layout, extension-surface shape, or the seams between projects — by reflecting over the assembly rather than executing pipeline logic. Used to codify conventions that matter enough to fail CI on drift, rather than relying on review (`ClosedSumStructureTests`, `ExtensionNamespaceMirrorTests`, `ExtensionPointStructureTests`, `NamespaceLayoutTests`).
_Avoid_: invariant test, structure test, fitness function (the latter is the broader software-architecture-community term; Flowthru consistently uses "architecture test")

**Laws kit**: An abstract test base class in `Flowthru.Tests.Kits` (named `*Laws`) that codifies the behavioral contract for a Core [[Extension surface]] — `IStorageAdapterLaws<T>`, `IFormatSerializerLaws`, `IFormatRowReaderLaws`, `IStorageMediumLaws`, `ISupportsFingerprintLaws`. Every extension that closes a slice of the surface ships a subclass with factory methods; NUnit instantiates one fixture per declared scenario, enforcing the laws uniformly. The kit is the test-time analog of the extension-surface interface — the surface defines the API contract, the kit defines the behavioral contract via individual [[Law]]s.
_Avoid_: contract test, base test class, conformance kit (older name; persists on subclasses pending naming cleanup — see issue [#23](https://github.com/chaoticgoodcomputing/flowthru/issues/23))

**Law**: A single test method on a [[Laws kit]] base that every implementation must satisfy — named `<Behavior>Law` (`RoundTripLaw`, `InspectShallowOnWellFormedLaw`, etc.). Framed algebraically: laws describe invariants of the *interface*, not any one implementation. A failing law is either an implementation bug or a kit-contract bug — never "this adapter is different."
_Avoid_: contract test, assertion, invariant

**Design-time test**: A test that enforces a [[Design-time error]] — verifies that the C# type system, source generators, Roslyn analyzers, or code fixes correctly catch incorrect code patterns before any IL runs. Sub-categories include *compilation tests* (does the type system reject mismatched Schemas?), *analyzer tests* (does the diagnostic fire on the right input?), *code-fix tests* (does the fix transform incorrect code into correct code?), and *source-generator tests* (does the generator emit the expected code?).
_Avoid_: compile-time test (too narrow — doesn't cover analyzer/code-fix work), unit test (general programming term), build test

**Pre-flight test**: A test that enforces a [[Pre-flight error]] — verifies the framework catches environmental or structural errors before any Step executes (DAG validation, external input inspection, schema drift in external data, [[Dry-run mode]]). Collectively, pre-flight tests uphold the framework's design invariant: *any Flow that passes pre-flight must complete successfully*.
_Avoid_: validation test, setup test, configuration test

**Runtime test**: A test that captures a user-reported [[Runtime error]] as a *staging ground*, not a permanent home — every runtime test should leave the category quickly: fixed and moved to [[Test mirror|mirror]] or [[Example test|example-test]] coverage, or hoisted to [[Pre-flight error|pre-flight]] / [[Design-time error|design-time]] where the check should have lived. Goal: this category stays empty.
_Avoid_: regression test, integration test, smoke test

**Test mirror**: The enforced 1:1 structural relationship between `src/` and `tests/` projects — `src/core/Flowthru.Core` is mirrored by `tests/core/Flowthru.Core.Tests`, `src/extensions/Flowthru.Extensions.Csv` by `tests/extensions/Flowthru.Extensions.Csv.Tests`, etc. When you add a new source project, you also add its mirror test project; the structure makes it obvious where the tests for any production code live.
_Avoid_: shadow project, parallel test project

**Example test**: A test that physically runs an example from `examples/` as a test, exercising the framework end-to-end through real pipeline usage. Lives in `tests/integration/Flowthru.Tests.Examples`. The examples-as-tests pattern means every starter and advanced example is also a contract — breaking the example breaks CI.
_Avoid_: end-to-end test, system test, scenario test
