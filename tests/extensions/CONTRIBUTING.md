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

Subclasses follow the same `*Laws` naming as the kit bases they inherit (e.g., `CsvSerializedEnumLaws : ISerializedEnumLaws`, `SingletonXmlAdapterLaws : IStorageAdapterLaws<XmlSchema>`). The `I`-prefix is reserved for kit-level base classes — concrete subclasses drop it.

## tests/helpers — What's There

`tests/helpers/` ships two packages with deliberate separation:

- **`Flowthru.Tests.Helpers`** — testing mechanism for the Roslyn surface: `NUnit4Verifier` and `CodeFixTestHelper`. Used by Core analyzer / source-generator tests; rarely needed in extension tests unless your extension ships its own analyzers.
- **`Flowthru.Tests.Kits`** — the laws-kit infrastructure: abstract `*Laws` base classes per [[Extension surface]], JSON fixture files under `Fixtures/{Flat,Nested,Mixed}/`, and the `FixtureLoader`. This is what extension tests subclass against.

The kit package is self-contained — it does *not* depend on `Helpers`. JSON fixtures (deserialized via `JsonFormatSerializer<TRow>` from Core, so the kit takes no extension dependency) let cross-format round-trip laws use the *same* input across CSV, Excel, Parquet, XML, etc. — behavioral drift between formats surfaces as a test failure rather than a manual review.

## Backend Matrix

Some extensions need testing against multiple real backend implementations to catch provider-specific bugs that in-memory shims can't reproduce. The canonical motivating case is EFCore: a Postgres-only nullability bug in `EFCoreShapeValidator` (commit `0cb460d9`) shipped because tests ran SQLite-only.

The pattern: a generic [[Laws kit]] subclass parameterizes over multiple backend *types* via `[TestFixture(typeof(TBackend))]`, with each backend implementing `IResourceBackend<TScope>` (or `IEphemeralResourceBackend<TScope>` for resources that provision and tear down external state). One scenario, multiple providers, uniform contract enforcement.

The canonical example lives in [/tests/extensions/Flowthru.Extensions.EFCore.Tests/Lifecycle/EFCoreResourceLaws.cs](/tests/extensions/Flowthru.Extensions.EFCore.Tests/Lifecycle/EFCoreResourceLaws.cs): one generic subclass, two `[TestFixture(typeof(...))]` attributes binding [SqliteFileBackend](/tests/extensions/Flowthru.Extensions.EFCore.Tests/Backends/SqliteFileBackend.cs) (in-process, runs always) and [PostgresContainerBackend](/tests/extensions/Flowthru.Extensions.EFCore.Tests/Backends/PostgresContainerBackend.cs) (Testcontainers-driven, gated on Docker availability). Both fixtures run by default; the Postgres tier reports Inconclusive on environments without Docker rather than failing — see [[Test capability gate]] below.

### Re-entrancy contract

A single backend instance lives for a whole fixture. `CreateResource()` is called per test and must return a resource whose external state is *disjoint* from every prior and concurrent call. The kit enforces this via `ConcurrentCreateResourceProducesDisjointStateLaw` — 8 parallel `CreateResource()` calls, each yielding a unique `ExternalStateIdentifier(scope)`. Violations point to shared mutable state on the backend; constructors must stay cheap and configuration-only, with any expensive shared setup amortised inside a `Lazy<T>` field that fires on first use.

## Test Capability Gating

External-dependency tests (Docker, SPARK_HOME, JDK 17+, Chromium) report **Inconclusive** rather than fail when their dependency is absent. The mechanism is three layers, deliberately loose-coupled:

1. **Bash check at install time** — [scripts/post-install/dependencies/](/scripts/post-install/dependencies/) informs the developer once at `pnpm install` time what's missing and how to install. Doesn't gate tests.
2. **`TestCapabilities` at run time** — [tests/helpers/Flowthru.Tests.Kits/Prelude/TestCapabilities.cs](/tests/helpers/Flowthru.Tests.Kits/Prelude/TestCapabilities.cs) ships a named singleton per capability with a lazy `IsAvailable()` probe. Each probe runs at most once per test process.
3. **Backend-declared `RequiredCapabilities`** — backends list the capabilities they depend on. The Laws kit's `OneTimeSetUp` runs `Assume.That(cap.IsAvailable(), cap.MissingMessage)` over the list before any expensive setup, so a missing capability yields Inconclusive *before* the backend ever attempts (e.g.) to start a container.

Adding a new dependency is two lines:

```csharp
public static TestCapability SparkHome { get; } = new(
  Name: "SPARK_HOME",
  IsAvailable: () => Environment.GetEnvironmentVariable("SPARK_HOME") is { } p && Directory.Exists(p),
  MissingMessage: "SPARK_HOME must point to a valid Spark install. Install: https://spark.apache.org/downloads.html"
);
```

A backend that needs it declares `RequiredCapabilities { get; } = [TestCapabilities.SparkHome]` and gets gated automatically — no new branches in the Laws kit, no per-consumer plumbing.

Backends that need a dependency may *also* carry `[Category("RequiresX")]` for explicit CI matrix selection. The category is informational; the capability gate is the load-bearing check.

## Glossary

### Roles

This context's audience is the Extension Developer — see [/src/extensions/CONTRIBUTING.md](/src/extensions/CONTRIBUTING.md) for the role definition and responsibilities.

### Tests/extensions Vocabulary

Most testing vocabulary inherits from [/tests/core/CONTRIBUTING.md](/tests/core/CONTRIBUTING.md). The entries below are unique to extension testing.

**Backend matrix**: A pattern where a generic [[Laws kit]] subclass parameterises over multiple real backend *types* via `[TestFixture(typeof(TBackend))]` — same scenarios, different providers — to catch provider-specific bugs that in-memory shims can't reproduce. Infrastructure lives in `Flowthru.Tests.Kits.Prelude.IResourceBackend` and `FlowResourceLaws<TBackend, TScope>`; concrete backend implementations are extension-defined. Canonical example: [EFCoreResourceLaws](/tests/extensions/Flowthru.Extensions.EFCore.Tests/Lifecycle/EFCoreResourceLaws.cs) over SQLite (in-process) and PostgreSQL (Testcontainers). Motivating incident: commit `0cb460d9` ("fix: resolved nullability bug for PGSQL on EFCore shape validator") — a Postgres-only bug that escaped SQLite-only test coverage.
_Avoid_: parameterized test, provider matrix

**Test capability gate**: A declarative mechanism for backend-scoped optional dependencies. Backends list `RequiredCapabilities` (e.g. [[TestCapabilities.Docker|TestCapabilities]]); the Laws kit's `OneTimeSetUp` runs `Assume.That(cap.IsAvailable(), cap.MissingMessage)` before any expensive setup. Missing capability ⇒ Inconclusive fixture, not a failure. Lives in `Flowthru.Tests.Kits.Prelude.TestCapability` + `TestCapabilities`. Replaces ad-hoc patterns like manual `[Category(...)]` filtering or constructor-side exception throwing.
_Avoid_: skip filter, integration gate, requires-tag
