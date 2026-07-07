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

The S3 gateway laws ([S3GatewayLaws](/tests/extensions/Flowthru.Extensions.AWS.S3.Tests/Contract/S3GatewayLaws.cs)) are a second instance, over three tiers: [LocalFileS3Backend](/tests/extensions/Flowthru.Extensions.AWS.S3.Tests/Backends/LocalFileS3Backend.cs) (offline stub, always runs), [MinioContainerBackend](/tests/extensions/Flowthru.Extensions.AWS.S3.Tests/Backends/MinioContainerBackend.cs) (Testcontainers-driven real MinIO, gated on Docker), and [LiveS3Backend](/tests/extensions/Flowthru.Extensions.AWS.S3.Tests/Backends/LiveS3Backend.cs) (an external S3 / S3-compatible bucket via `FLOWTHRU_S3_TEST_*`, gated on [[TestCapabilities.AwsS3|Test capability gate]]). The MinIO tier makes the shipped `LocalFileS3Gateway` stub a *verified* stand-in against a real S3 server with no AWS account.

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

## Constrained-Resource Tiers

Capability gating answers *is the dependency present?* Some bugs answer a different question: *does it survive under production-like resource limits?* These never reproduce on a fat CI runner — nothing enforces a ceiling. That is the same blind spot that let the seekable-stub bug (#105) and the concurrent-read memory-exhaustion crash (#111) ship: both passed every test until they hit a real, constrained environment.

The convention: a resource-sensitive integration test asserts the invariant plainly (every concurrent read returns the right rows), and a **constrained-container CI job** runs it under an explicit cap so the ceiling is real. The test cannot impose the host's memory limit on itself — that is a job/container concern:

```bash
# Reproduces #111: concurrent s3:// Parquet reads exhaust memory under a 1 GB ceiling.
# Green unconstrained; fails under the cap. MinIO runs as a sidecar; the *test process* is capped.
podman run --rm --network=host --memory=1g --cpus=0.5 -v "$PWD":/work -w /work \
  -e FLOWTHRU_S3_TEST_BUCKET=... -e FLOWTHRU_S3_TEST_SERVICE_URL=http://localhost:19000 \
  -e AWS_ACCESS_KEY_ID=... -e AWS_SECRET_ACCESS_KEY=... \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet vstest \
  dist/tests/extensions/Flowthru.Extensions.AWS.S3.Tests/net10.0/Flowthru.Extensions.AWS.S3.Tests.dll \
  --TestCaseFilter:"FullyQualifiedName~ParquetOverS3ConcurrencyTests"
```

[ParquetOverS3ConcurrencyTests](/tests/extensions/Flowthru.Extensions.AWS.S3.Tests/ParquetOverS3ConcurrencyTests.cs) is the reference case. Test non-local storage under production-like limits, not just functionally.

### Streaming tier — RSS stays flat as row count grows (#124)

The eager tier above proves the *cap holds* for whole-object reads (bounded fan-out). The streaming tier proves the ADR-0023 remedy: a `.AsStream()` read of a **multi-row-group** `s3://` Parquet object is O(one row group), so a *single* streaming read of an arbitrarily large object — and a whole layer of concurrent ones — survives a cap that whole-object buffering would blow. [StreamingParquetOverS3Tests](/tests/extensions/Flowthru.Extensions.AWS.S3.Tests/StreamingParquetOverS3Tests.cs) is the reference case: it asserts only correctness (every streamed read returns all seeded rows, in order, checksum-verified); the **flat-RSS invariant is the job's to enforce** — crank `FLOWTHRU_STREAM_ROWS` and the capped test process's RSS must not track it.

This tier self-provisions MinIO via Testcontainers (gated on `TestCapabilities.Docker`), so — unlike the sidecar pattern above — MinIO is launched as a **sibling container on the host socket**, outside the cap; only the *test process* is memory-limited:

```bash
# Streaming O(row group) regression (#124): a multi-row-group s3:// Parquet object
# streamed back through .AsStream(). RSS of the *capped test process* stays FLAT as
# FLOWTHRU_STREAM_ROWS grows; the eager whole-object path would blow the 1 GB cap.
# Testcontainers launches MinIO as a sibling on the mounted host socket — the MinIO
# container is NOT counted against --memory; only the test process is. A `docker`
# shim is needed for the capability probe (`which docker`) when the runtime is podman.
podman run --rm --network=host --memory=1g --cpus=0.5 -v "$PWD":/work -w /work \
  -v /run/user/1000/podman/podman.sock:/var/run/docker.sock \
  -e DOCKER_HOST=unix:///var/run/docker.sock -e TESTCONTAINERS_RYUK_DISABLED=true \
  -e FLOWTHRU_STREAM_ROWS=2000000 -e FLOWTHRU_STREAM_ROWGROUP=50000 \
  mcr.microsoft.com/dotnet/sdk:10.0 sh -c \
  'ln -sf "$(command -v podman || echo /usr/bin/docker)" /usr/local/bin/docker; dotnet vstest \
   dist/tests/extensions/Flowthru.Extensions.AWS.S3.Tests/net10.0/Flowthru.Extensions.AWS.S3.Tests.dll \
   --TestCaseFilter:"FullyQualifiedName~StreamingParquetOverS3Tests"'
```

Both tiers share the discipline: the test asserts the *right rows*; the container job imposes the *memory ceiling*. Neither test measures its own RSS.

## Glossary

### Roles

This context's audience is the Extension Developer — see [/src/extensions/CONTRIBUTING.md](/src/extensions/CONTRIBUTING.md) for the role definition and responsibilities.

### Tests/extensions Vocabulary

Most testing vocabulary inherits from [/tests/core/CONTRIBUTING.md](/tests/core/CONTRIBUTING.md). The entries below are unique to extension testing.

**Backend matrix**: A pattern where a generic [[Laws kit]] subclass parameterises over multiple real backend *types* via `[TestFixture(typeof(TBackend))]` — same scenarios, different providers — to catch provider-specific bugs that in-memory shims can't reproduce. Infrastructure lives in `Flowthru.Tests.Kits.Prelude.IResourceBackend` and `FlowResourceLaws<TBackend, TScope>`; concrete backend implementations are extension-defined. Canonical example: [EFCoreResourceLaws](/tests/extensions/Flowthru.Extensions.EFCore.Tests/Lifecycle/EFCoreResourceLaws.cs) over SQLite (in-process) and PostgreSQL (Testcontainers). Motivating incident: commit `0cb460d9` ("fix: resolved nullability bug for PGSQL on EFCore shape validator") — a Postgres-only bug that escaped SQLite-only test coverage.
_Avoid_: parameterized test, provider matrix

**Test capability gate**: A declarative mechanism for backend-scoped optional dependencies. Backends list `RequiredCapabilities` (e.g. [[TestCapabilities.Docker|TestCapabilities]]); the Laws kit's `OneTimeSetUp` runs `Assume.That(cap.IsAvailable(), cap.MissingMessage)` before any expensive setup. Missing capability ⇒ Inconclusive fixture, not a failure. Lives in `Flowthru.Tests.Kits.Prelude.TestCapability` + `TestCapabilities`. Replaces ad-hoc patterns like manual `[Category(...)]` filtering or constructor-side exception throwing.
_Avoid_: skip filter, integration gate, requires-tag
