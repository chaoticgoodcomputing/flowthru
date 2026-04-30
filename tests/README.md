# Testing Philosophy

Recall from the [Contributor's Guide](/CONTRIBUTING.md) that Flowthru has two core promises to end-users:

> 1. End-users can easily write data pipelines, and have a development experience focused on what *their* pipelines will do, not how Flowthru is handling the pipeline.
> 2. If an error can occur in the pipeline they've created, it will occur as soon in the development process as possible.

The tests projects are meant to enforce how we fulfill these promises, and cover both the API surface *and* the error surface of Flowthru and its extensions.

## API Surface Tests (How and when should Flowthru work?)

These tests verify that Flowthru works correctly when used as intended. They validate the first promise: that developers can write pipelines easily, without unnecessary ceremony or boilerplate, and expect it to Just Work™.

### Unit Tests

Unit tests validate individual components in isolation:

- **Execution tests:** Verify that nodes execute correctly, that catalog entries load and save data, and that pipelines orchestrate node execution properly.
- **Service tests:** Verify dependency injection, configuration loading, and the service layer that hosts pipelines.

These tests use in-memory storage and minimal fixtures to exercise specific code paths without external dependencies.

### Integration Tests (`Flowthru.Tests.Examples`)

**Every example project in `/examples` is executed as a test.** This serves two purposes:

1. **Examples are documentation.** They demonstrate real-world usage patterns for end-users.
2. **Examples are executable contracts.** If an example breaks, either the API surface changed (a breaking change that needs documentation) or Flowthru has a regression.

The integration test suite:
- Discovers all example projects via reflection
- Invokes their `Program` entry point through the service layer
- Verifies they complete successfully
- Provides code coverage through actual pipeline execution

This approach ensures that the code we show to users actually works, and that refactoring Flowthru's internals doesn't silently break usage patterns we've documented.

## Error Surface Tests (How and when should Flowthru fail?)

These tests validate the second promise: that errors surface as early in the development process as possible. They're organized by the Flowthru philosophy's documented three stages of errors:

> 1. Build-time (beautiful, gold standard, chef's kiss)
> 2. Pre-flight (tolerable, but aggravating)
> 3. Runtime (evil! should be destroyed wherever possible)

When developing features and fixes, we should **always** be considering not just *how* Flowthru can fail, but *when*.

### Compilation Tests

Verify that the type system and source generators catch configuration errors at build time:

- Schemas with mismatched types between nodes and catalog entries don't compile
- Incompatible schema/serializer combinations (e.g., nested schemas with CSV) produce build errors
- Source generator diagnostics (missing `partial`, conflicting interfaces) are emitted correctly

These tests often use Roslyn analyzers or verify that certain code patterns produce expected compiler errors. When writing these tests, consider: could this constraint be expressed as a generic constraint? Could a source generator emit a diagnostic?

### Pre-Flight Tests

Verify that environmental and structural errors are caught before any node executes:

- Duplicate producers (two nodes writing to the same entry) are rejected during DAG construction
- Circular dependencies are detected
- Missing external inputs are caught during validation
- Schema drift in external files is detected before execution

**If a pre-flight check passes, the pipeline must complete successfully.** If it doesn't, either the check is incomplete or a compile-time constraint is missing. When a pre-flight test fails, ask: is this truly an environmental concern, or could it have been a type-level constraint?

### Runtime Tests

Runtime errors are like suspicious moles: they're horrible, dangerous, and should be documented & tracked. Runtime error-surface tests are how we accomplish this documentation and tracking. This category of tests serves two purposes:

1. Replicate user reports of runtime errors
2. Act as a staging ground for confirmed runtime errors to either be:
  1. Fixed, and moved to a unit/integration test; or
  2. Be moved up to the build or pre-flight error surface

**Tests should not remain here for very long!** Once a user report of a runtime error has been cataloged here, it should be moved to the appropriate location — fixed if possible, or moved up to a pre-flight or build error.

### Extension Conformance Kits (`Flowthru.Tests.Kits`)

Every first-party Flowthru extension that implements a Core extension surface — `IStorageAdapter<T>`, `IFormatSerializer<TRow>`, `IMetadataProvider`, and friends — must also ship a *conformance subclass* that inherits from the matching abstract base in [`tests/helpers/Flowthru.Tests.Kits`](helpers/Flowthru.Tests.Kits/). The conformance bases codify each surface's contract; the subclass supplies factory methods, NUnit instantiates one fixture per declared scenario, and the contract is enforced uniformly.

The kit lives separately from `Flowthru.Tests.Helpers`:

- `Flowthru.Tests.Helpers` — Core test mechanism (compilation/generator helpers, NUnit verifier, capturing providers).
- `Flowthru.Tests.Kits` — extension contract: conformance bases, schema fixtures, JSON sample data, the `FixtureLoader`. Self-contained; does not depend on `Helpers`.

#### Adding a conformance subclass

The pattern is the same across surfaces — declare a static `Fixtures` property, decorate with `[TestFixtureSource(nameof(Fixtures))]`, take the fixture path through the constructor, and override the abstract factory methods. NUnit instantiates one fixture per entry in `Fixtures` and runs the inherited `[Test]` methods against each.

```csharp
[TestFixtureSource(nameof(Fixtures))]
public class ParquetTraditionalSchemaConformance : FormatSerializerConformance<TraditionalSchema>
{
  public static IEnumerable<string> Fixtures => new[] { "Flat/Simple/rows.json" };
  public ParquetTraditionalSchemaConformance(string fixturePath) : base(fixturePath) { }

  protected override IFormatSerializer<TraditionalSchema> CreateSerializer()
    => new ParquetFormatSerializer<TraditionalSchema>();
}
```

#### Why JSON for fixtures

`JsonFormatSerializer<TRow>` ships in Core, so the kit can deserialize fixture data without taking on an extension dependency. Cross-format round-trip tests use the *same* JSON fixture as input across CSV, Excel, Parquet, XML, etc. — behavioral drift between formats surfaces as a test failure rather than a manual review. Fixtures live as `.json` files under `Flowthru.Tests.Kits/Fixtures/{Flat,Nested,Mixed}/<scenario>/<variant>.json`; the shape directories pair with the schema's capability marker interfaces emitted by `[FlowthruSchema]`.

#### Read-only formats and adapters

When `Traits.CanWrite = false` (Excel, OnnxModelStorageAdapter), the round-trip test passes vacuously with an explanatory message. The trait-honesty cross-check in `StorageAdapterTraitsConformance<T>` exercises the read-only path; per-extension tests cover format-specific deserialization scenarios that require a writer from a different library (e.g., ClosedXML for `.xlsx`).

#### Backend matrix testing (EFCore + TestContainers)

Some extensions need to be exercised against multiple real backends to catch provider-specific bugs that in-memory shims can't reproduce. The canonical example is EFCore: a Postgres-only false positive in `EFCoreShapeValidator` shipped to production because tests only ran against in-memory SQLite (commit `0cb460d9`).

EFCore's conformance subclasses parameterize over a backend abstraction:

```csharp
[TestFixtureSource(nameof(BackendMatrix))]
public class EFCoreStorageAdapterConformance : StorageAdapterConformance<IEnumerable<TestEntity>>
{
  public static IEnumerable<TestFixtureData> BackendMatrix()
  {
    yield return new TestFixtureData("Synthetic/efcore-entities", typeof(SqliteInMemoryBackend));
    var pg = new TestFixtureData("Synthetic/efcore-entities", typeof(PostgresContainerBackend));
    pg.Properties.Add("Category", "Integration");
    yield return pg;
  }

  public EFCoreStorageAdapterConformance(string fixturePath, Type backendType)
    : base(fixturePath) { _backendType = backendType; }
  // ... [OneTimeSetUp] activates the backend; tests run identically against both.
}
```

`IDbBackend` is a minimal abstraction (`StartAsync()` → `DbContextOptions<TestDbContext>`, plus `IAsyncDisposable`). Two implementations live in `tests/extensions/Flowthru.Extensions.EFCore.Tests/Backends/`:

- **`SqliteInMemoryBackend`** — fast (no Docker), runs on every PR via the default `nx run affected -t test` flow.
- **`PostgresContainerBackend`** — `Testcontainers.PostgreSql`-driven, tagged `Integration`, runs only on demand.

Two-tier execution:

```bash
# Fast tier — SQLite only, sub-second EFCore
nx run-many -t test                     # default
dotnet test                             # excludes nothing; runs Integration if Docker is available

# Integration tier — TestContainers (requires Docker)
nx run tests:test:integration           # filters to Category=Integration across the solution
dotnet test --filter Category=Integration
```

The pattern lifts to other extensions where multiple backends matter: HTTP (TestContainers nginx serving fixture files), EFCore.Bulk (per-provider bulk-load paths), GQL (TestContainers hot-chocolate). The `IDbBackend`-style abstraction stays per-extension; the `Category = "Integration"` convention and the `nx test:integration` target stay shared.

When adding a backend matrix to a new extension:

1. Define a minimal backend abstraction in `tests/extensions/<Ext>.Tests/Backends/` mirroring `IDbBackend`.
2. Implement an in-process / in-memory backend for the fast tier.
3. Implement a TestContainers-backed integration backend tagged `Category = "Integration"`.
4. Refactor existing conformance subclasses to take the backend type as a constructor argument (per the `EFCoreStorageAdapterConformance` example above) and activate via `Activator.CreateInstance` in `[OneTimeSetUp]`.
5. Add an explicit regression test for any production bug the backend matrix would have caught — pin it to the relevant integration backend and cite the commit.

#### Cross-extension negative scenarios

Some bugs have a *categorical shape* — pre-flight pass-through that should have surfaced a structural mismatch, the wrong `ValidationErrorType` for a known failure mode, etc. — that could plausibly exist in extensions other than the one where the bug was first found. The kit lifts these into *negative scenarios*: a virtual factory on the kit base, defaulting to `null`, that each adapter's conformance subclass opts into when the scenario applies.

When the kit catches an adapter using the wrong error category for a known failure, **fix the adapter, don't loosen the assertion.** The kit's assertions reflect what the Core types *should* mean, not what current adapters happen to do — that's the whole reason for having a kit.

```csharp
// In StorageAdapterConformance<T> — the kit base:
protected virtual IStorageAdapter<T>? CreateAdapterMissingExpectedColumn() => null;

[Test]
public async Task InspectShallow_SchemaDeclaresColumnNotInSource_DetectsMismatch()
{
  var adapter = CreateAdapterMissingExpectedColumn();
  if (adapter is null) Assert.Pass("Negative scenario not opted into.");

  var result = await adapter!.InspectShallow(sampleSize: 10).Run();
  Assert.That(result.Errors,
    Has.Some.Matches<ValidationError>(e => e.ErrorType == ValidationErrorType.SchemaMismatch));
}

// In each conformance subclass that can construct the scenario:
protected override IStorageAdapter<...>? CreateAdapterMissingExpectedColumn()
{
  // build an adapter pointing at a source whose shape diverges from the schema
  // (e.g., a CSV file with a header row missing a schema-declared column)
  return BuildAdapter(seedFileWithMismatchedShape);
}
```

The error category is part of the contract. Provider extensions that detect a structural mismatch should throw `SchemaMismatchException` (in `Flowthru.Core.Data.Validation`); `ValidationResult.FromException` translates it to `ValidationErrorType.SchemaMismatch`. Provider-specific exception text (CsvHelper's `HeaderValidationException` message, Parquet's schema-diff details, etc.) lives in `ValidationError.Message`, never in the `ErrorType`.

When a bug surfaces in one extension and you suspect the same shape elsewhere:

1. **Lift the bug shape to a kit virtual.** Add a `CreateAdapter<X>()` factory on the relevant conformance base that returns `null` by default. Add the corresponding `[Test]` method asserting the expected outcome.
2. **Opt the original extension in.** Override the factory in its conformance subclass; verify the test catches the bug.
3. **Audit the others.** Override the factory in every adapter conformance subclass that *could* exercise the scenario; run the suite. Each adapter that fails the assertion either has the same bug (fix it) or is using the wrong category (fix it). Adapters where the scenario is structurally inapplicable (XML's whole-document semantics, JSON's missing-optional indistinguishability) legitimately leave the default `null`.

The Phase F audit ran exactly this pattern with `CreateAdapterMissingExpectedColumn`. Found bugs in three of four audited paths: CSV's two adapter paths used wrong categories; Parquet silently accepted missing columns; SingletonJSON silently accepted missing required properties. All three fixed in the same phase.

### Evaluating Error Tests

When you encounter a runtime or pre-flight error during development:

1. **Can the C# type system express this constraint?** → Research generic constraints, source generators, or Roslyn analyzers that could move it to compile-time.
2. **Is it truly environmental?** → If external state (files, network, databases) is the only variable, pre-flight is appropriate.
3. **Is it truly unpredictable?** → Network drops, OOM, and hardware failures belong at runtime. Most logic errors do not.

Error surface tests are an ongoing audit. As C# and Roslyn evolve, revisit runtime and pre-flight tests to see if they can migrate earlier.

## Running Tests

### Basic Test Execution

Run all tests across the solution:
```bash
nx run flowthru:test
```

Run specific test categories (unit tests only):
```bash
nx run test/unit:compilation   # Build-time error tests
nx run test/unit:validation     # Pre-flight error tests
nx run test/unit:execution      # Runtime execution tests
```

Run integration tests (all examples):
```bash
nx run test/examples:test
```

### Coverage Collection

Tests are run with coverage collection enabled in CI via `coverlet.runsettings`. Coverage reports are aggregated and tracked by [Codecov](https://codecov.io/gh/chaoticgoodcomputing/flowthru), with per-flag carryforward so partial `nx affected` runs don't erase unaffected projects.

To force a clean test run by removing previous `TestResults` artifacts:
```bash
nx run tests:coverage:purge
```

### What Gets Measured

Code coverage is collected for all `Flowthru*` assemblies:
- `Flowthru` (core framework)
- `Flowthru.Integrations.*` (integration libraries)
- `Flowthru.Extensions.*` (extension libraries)

The following are excluded from coverage:
- Test assemblies (`*.Tests`, `*.Tests.*`)
- Generated code (source generators, designers)
- Third-party libraries (xunit, NUnit, Microsoft, System)

Coverage configuration is defined in `coverlet.runsettings` at the repository root.
