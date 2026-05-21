# Contributing to Flowthru Extension Tests

This document is for **Extension Developers writing tests** — the tests that live in `tests/extensions/<Extension>.Tests/` per the [[Test mirror]] convention, exercising the extensions in `src/extensions/*`.

**Audience scope:** assumes familiarity with [/tests/core/CONTRIBUTING.md](/tests/core/CONTRIBUTING.md) (Core test conventions and the bulk of testing vocabulary). Terms defined here are the *additional* vocabulary specific to extension testing.

See [/src/extensions/CONTRIBUTING.md](/src/extensions/CONTRIBUTING.md) for Extension Developer engineering conventions, and [/CONTRIBUTING.md](/CONTRIBUTING.md) for cross-cutting design rules.

## Subclassing Laws Kits

Every extension that closes a slice of an [[Extension surface]] must ship a [[Laws kit]] subclass — concretely, a class that inherits from the matching `*Laws` base in `Flowthru.Tests.Kits` and supplies factory methods that the laws exercise. The base class drives the contract; the subclass binds it to a real implementation.

The typical shape:

```csharp
[TestFixtureSource(nameof(Fixtures))]
public class CsvTraditionalSchemaLaws : IFormatSerializerLaws<TraditionalSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };
  public CsvTraditionalSchemaLaws(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<TraditionalSchema> CreateSerializer()
    => new CsvFormatSerializer<TraditionalSchema>();
}
```

The naming convention is mid-migration — many existing subclasses are named `*Conformance` (the older convention); the kit bases were renamed to `*Laws` per the algebra-laws framing. New subclasses should follow the `*Laws` naming; the existing `*Conformance` subclasses will be renamed as part of the cleanup tracked in issue [#23](https://github.com/chaoticgoodcomputing/flowthru/issues/23).

## tests/helpers — What's There

`tests/helpers/` ships two packages with deliberate separation:

- **`Flowthru.Tests.Helpers`** — testing mechanism for the Roslyn surface: `NUnit4Verifier` and `CodeFixTestHelper`. Used by Core analyzer / source-generator tests; rarely needed in extension tests unless your extension ships its own analyzers.
- **`Flowthru.Tests.Kits`** — the laws-kit infrastructure: abstract `*Laws` base classes per [[Extension surface]], JSON fixture files under `Fixtures/{Flat,Nested,Mixed}/`, and the `FixtureLoader`. This is what extension tests subclass against.

The kit package is self-contained — it does *not* depend on `Helpers`. JSON fixtures (deserialized via `JsonFormatSerializer<TRow>` from Core, so the kit takes no extension dependency) let cross-format round-trip laws use the *same* input across CSV, Excel, Parquet, XML, etc. — behavioral drift between formats surfaces as a test failure rather than a manual review.

## Backend Matrix

Some extensions need testing against multiple real backend implementations to catch provider-specific bugs that in-memory shims can't reproduce. The canonical motivating case is EFCore: a Postgres-only nullability bug in `EFCoreShapeValidator` (commit `0cb460d9`) shipped because tests ran SQLite-only.

The pattern: a Laws kit subclass parameterizes over multiple backends via `[TestFixtureSource(nameof(BackendMatrix))]`, with each backend implementing a small abstraction (`IResourceBackend` and friends in `Flowthru.Tests.Kits.Prelude`). One scenario, multiple providers, uniform contract enforcement.

The *infrastructure* for this pattern lives in `Flowthru.Tests.Kits.Prelude` (`IResourceBackend`, `FlowResourceConformance<TBackend, TScope>`); no extension has implemented against it yet — see issue [#22](https://github.com/chaoticgoodcomputing/flowthru/issues/22). When the first concrete backend matrix lands, this section's pattern will become canonical for any extension targeting multiple real backends.

## Glossary

### Roles

This context's audience is the Extension Developer — see [/src/extensions/CONTRIBUTING.md](/src/extensions/CONTRIBUTING.md) for the role definition and responsibilities.

### Tests/extensions Vocabulary

Most testing vocabulary inherits from [/tests/core/CONTRIBUTING.md](/tests/core/CONTRIBUTING.md). The entry below is the only term unique to extension testing.

**Backend matrix**: A pattern where a [[Laws kit]] parameterizes over multiple real backend implementations — same scenarios, different providers — to catch provider-specific bugs that in-memory shims can't reproduce. Infrastructure lives in `Flowthru.Tests.Kits.Prelude.IResourceBackend` and `FlowResourceConformance<TBackend, TScope>`; concrete backend implementations are extension-defined. Motivating incident: commit `0cb460d9` ("fix: resolved nullability bug for PGSQL on EFCore shape validator") — a Postgres-only bug that escaped SQLite-only test coverage.
_Avoid_: parameterized test, provider matrix
