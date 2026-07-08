# Flowthru.Extensions.DuckDB

Run a flow step's entire body as SQL inside the embedded [DuckDB](https://duckdb.org/) engine,
wired between ordinary Parquet Catalog Items. The step reads its input files, transforms, and
writes its output file **without a single row entering the .NET runtime** — which is the point:
transforms that must see all their input before emitting output (global sorts, dedup, aggregate,
join) pay a 10–20× tax as in-memory LINQ once data reaches millions of rows, and delegating them
to the engine keeps CLR memory flat besides.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_duckdb)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

Bring the DuckDB mental model: an in-process OLAP engine that queries Parquet files where they
sit — no server, no import step. This package points that engine at your Catalog: each input item
becomes a SQL relation (named after its item label), your query is the step body, and the result
lands directly at the output item's path. On the DAG the step looks like any other step — it
schedules, validates, and renders like the rest of your flow; only its execution happens
engine-side.

Reach for a DuckDB transform when the step is **wide** — it must consume all input before
emitting output — and the data is large. Row-at-a-time logic (map, filter) belongs in ordinary
C# steps, where it composes with the rest of your code.

## Install

```bash
dotnet add package Flowthru.Extensions.DuckDB
```

Register the engine and hand it to your flows:

```csharp
services.AddFlowthru(configuration, b =>
{
  b.RegisterCatalog<Catalog>(sp => new Catalog());
  b.UseDuckDb(opts => opts.MemoryLimit = "4GB");
  b.RegisterFlow<Catalog, IDuckDbEngine>("Analytics", AnalyticsFlow.Create);
});
```

Wire a transform between Parquet items:

```csharp
public static BuiltFlow Create(Catalog catalog, IDuckDbEngine engine) =>
  FlowBuilder.CreateFlow("Analytics", flow =>
  {
    // Single input: the relation is named after the item's label.
    flow.AddDuckDbTransform(
      label: "sort_events",
      input: catalog.Events,
      output: catalog.SortedEvents,
      sql: "SELECT * FROM Events ORDER BY Country, OccurredAt",
      engine: engine);

    // Multiple inputs: bind each item to a relation name for the join.
    flow.AddDuckDbTransform(
      label: "enrich_orders",
      inputs:
      [
        DuckDbInputRelation.From(catalog.Orders, "orders"),
        DuckDbInputRelation.From(catalog.Customers, "customers"),
      ],
      output: catalog.EnrichedOrders,
      sql: """
        SELECT o.Id, o.Total, c.Region
        FROM orders o JOIN customers c ON o.CustomerId = c.Id
        """,
      engine: engine);
  });
```

The SQL's result schema is verified against the output item's declared schema before anything is
written; a mismatch (missing column, extra column, incompatible type) fails the step with a typed
schema-mismatch error enumerating every disagreement — the output file is never written.

## Schema-validated SQL: pre-flight and design-time

The SQL is validated against the three error phases, not runtime-only. `UseDuckDb()` registers a
**hermetic pre-flight check** that runs for every `AddDuckDbTransform` step before any step
executes: empty in-memory tables are built from the *declared* input record schemas (named per
the step's relation bindings), the SQL is `DESCRIBE`d against them — binding the query without
executing it — and the described result schema is verified against the declared output schema.
No real data is read and nothing outside the process is reached, so the check runs at every
`ValidationDepth` from `Hermetic` up — a schema-breaking SQL edit fails even an offline smoke
test (`DryRun.On + ValidationDepth.Hermetic`). Failures aggregate applicatively with all other
pre-flight errors and name the step, the relation binding, and the offending column(s):

```text
FTDDB3002: DuckDB transform 'totals_by_country' SQL result does not satisfy output item
'country_totals' declared schema CountryTotalRow: column 'TotalValue' is HUGEINT in the result
but the declared schema expects Double (accepts DOUBLE/FLOAT/REAL) — add an explicit CAST in
the transform SQL
```

The same check is a **design-time surface**: run it from a unit test and a schema-breaking SQL
edit fails your test run. The embedded engine binds a query in milliseconds — this belongs in
ordinary unit tests:

```csharp
[Test]
public async Task TransformSqlAgreesWithDeclaredSchemas()
{
  var flow = AnalyticsFlow.Create(new Catalog(), new InProcessDuckDbEngine());
  var result = await flow.ValidateDuckDbTransforms(); // every DuckDB step, aggregated
  Assert.That(result.IsValid, Is.True,
    string.Join("\n", result.Errors.Select(e => e.Message)));
}
```

A single step is checkable through the standard FUnit sugar instead: `FUnitContext.Validate(step)`
runs the same check via the step's `Validate()`.

Pre-flight diagnostic codes (`FTDDB30xx`): `FTDDB3001` — the SQL doesn't prepare against the
declared input schemas (unknown column/relation, syntax error); `FTDDB3002` — the result schema
doesn't satisfy the declared output schema; `FTDDB3003` — an input's declared schema has a
property the checks can't model.

## S3 endpoints

Endpoints don't have to be local files: a Parquet item on an `s3://` path (via
`Flowthru.Extensions.AWS.S3`'s `UseS3()`) works as a transform input or output unmodified — the
engine reads inputs with `read_parquet('s3://…')` and writes the output with
`COPY … TO 's3://…'`, all inside DuckDB's `httpfs` extension. The object bytes move directly
between the engine and the object store; nothing is staged to a local file, buffered in the CLR,
or materialized as rows.

```csharp
// catalog.Events on "s3://lake/raw/events.parquet", catalog.SortedEvents on
// "s3://lake/sorted/events.parquet" — the transform wiring is identical:
flow.AddDuckDbTransform(
  label: "sort_events",
  input: catalog.Events,
  output: catalog.SortedEvents,
  sql: "SELECT * FROM Events ORDER BY Country, OccurredAt",
  engine: engine);
```

**How credentials flow.** When the step executes, each S3-backed endpoint resolves its
`ByteLocation` through the S3 gateway — the same seam that reads and writes the object — which
mints a per-call access handoff (endpoint, region, url style, credentials from the standard AWS
chain, session token). The engine turns each handoff into a *temporary* DuckDB secret `SCOPE`d
to exactly that object's URI, created inside the transform's private in-memory database: inputs
carrying different credentials never see each other's (DuckDB picks the secret whose scope is
the longest prefix of the path being read, and an exact-object scope is the most specific
possible). Secrets die with the connection when the transform finishes — nothing is persisted,
logged, or carried on the catalog or the DAG, and engine error messages are scrubbed of
credential material before they surface as error values.

**How `httpfs` loads.** The bundled DuckDB binary statically links `parquet` but *not*
`httpfs`. On the first S3 transform, the engine runs `LOAD httpfs`; if the extension isn't
installed locally it runs `INSTALL httpfs` — a one-time download from DuckDB's extension
repository into the extension directory (`~/.duckdb` by default) — and loads it. Every later
transform loads it locally with no network. Two options control this:

- `ExtensionDirectory` — where DuckDB looks for (and installs) extensions. For air-gapped
  hosts, pre-provision `httpfs.duckdb_extension` here (run `INSTALL httpfs` once on a networked
  machine with the same DuckDB version and platform, or bake it into the container image).
- `AllowExtensionDownload` (default `true`) — set `false` to forbid the `INSTALL` download.
  A missing `httpfs` then fails the step with the typed `FTDDB4003` error naming the remedy,
  and DuckDB's own extension autoinstall is disabled for the connection so nothing downloads
  implicitly either. Purely local transforms never touch `httpfs` and are unaffected.

**Concurrency inheritance.** The `s3:read` concurrency cap (`S3Options.MaxConcurrentReads`) is
a property of the S3 medium, inherited through ordinary item wiring — a DuckDB step with
S3-backed endpoints picks it up exactly as a plain load step does, with no DuckDB-specific
configuration.

## Caching

Transforms are first-class cacheable. A step's SQL is wire-up data rather than compiled code, so
the step declares it into its cache identity: a hash of the exact SQL text (no normalization —
any edit invalidates), the DuckDB engine version, the relation-name bindings, and the
output-affecting write options (compression codec, row-group size). Unchanged SQL over unchanged
inputs with the output file present skips like any other cached step; editing the query, bumping
the engine, rebinding relations, or changing how the output file is written each forces a re-run.
Engine tuning that can't change output values (`MemoryLimit`, `Threads`, `TempDirectory`) is
deliberately excluded, so re-tuning a host never busts caches.

## Concurrency and memory

Each transform may use the engine's full memory budget (`MemoryLimit`, spilling to
`TempDirectory` beyond it), so the engine registers with the scheduler as a capacity-constrained
resource: at most `MaxConcurrentTransforms` DuckDB steps run at once (default `1`), regardless of
the flow's `Parallelism`. Peak engine memory is therefore
`MaxConcurrentTransforms × MemoryLimit`. All knobs bind from the `Flowthru:DuckDb` configuration
section or code-first via `UseDuckDb(opts => ...)`.

## Limitations (honest ones)

- **Local files and `s3://` objects only.** An item whose bytes live behind any other remote
  scheme (`https://`, `ftp://`, …) fails the step with a typed error (`FTDDB4001`).
  Non-byte-addressable items (memory, database) are rejected at wire-up.
- **`httpfs` may need the network once.** The bundled engine doesn't statically link `httpfs`,
  so the first S3 transform on a host downloads it unless it was pre-provisioned (see the S3
  section above). With `AllowExtensionDownload = false`, a missing `httpfs` is the typed
  `FTDDB4003` failure — explicit, never papered over.
- **Parquet endpoints only.** Inputs are bound via `read_parquet`; the output is written with
  `COPY ... (FORMAT PARQUET)`. Other formats (CSV, Postgres via `ATTACH`) are planned.
- **Schema checks cover flat primitive/enum schemas.** Nested and `IScalar` schema properties
  aren't checkable — the output schema rejects them at wire-up; an input schema carrying them
  fails pre-flight with a typed error (`FTDDB3003`) rather than silently skipping the check.
  Nullability isn't verified at transform time (DuckDB reports every query column as nullable) —
  a null in a non-nullable column surfaces as a typed schema mismatch when the output is next
  loaded. The runtime `DESCRIBE` verification (against the real files) still runs before the
  `COPY`, so drift between a real file and its declared schema is caught before anything is
  written.
- **Numeric strictness.** A result column type must round-trip losslessly into the declared
  property type (safe widenings are accepted; narrowings are not). Aggregates often widen —
  DuckDB's `SUM(INTEGER)` is `HUGEINT` — so `CAST` in your SQL to match the declared schema.

## Deployment

Everything below was observed, not assumed — verified 2026-07 against `DuckDB.NET.Data.Full`
1.5.3 on .NET 10 with the spike consumer at `tools/spikes/DuckDbAot` (a Parquet → `ORDER BY` →
Parquet transform through `IDuckDbEngine`). Cells we could not exercise are explicitly marked
**unverified** with the exact repro to run.

### What you ship

`DuckDB.NET.Data.Full` pulls `DuckDB.NET.Bindings.Full`, which bundles one prebuilt native
engine per RID (103.5 MB nupkg; 414 MB expanded in the NuGet cache). Publishing for a specific
RID ships only that RID's library:

| RID         | Native library     | Size     |
| ----------- | ------------------ | -------- |
| linux-x64   | `libduckdb.so`     | 67.1 MB  |
| linux-arm64 | `libduckdb.so`     | 59.9 MB  |
| osx (universal x64+arm64) | `libduckdb.dylib` | 107.1 MB |
| win-x64     | `duckdb.dll`       | 35.2 MB  |
| win-arm64   | `duckdb.dll`       | 40.4 MB  |

There is **no `linux-musl` RID** — see Alpine below. The linux-x64 library links against glibc
(`readelf`: max `GLIBC_2.25`, `GLIBCXX_3.4.22`) and needs `libstdc++` at runtime — satisfied by
Amazon Linux 2023, Debian, Ubuntu, and the non-Alpine .NET base images.

Measured deployment sizes for the spike app (linux-x64, docs/pdb/dbg excluded from zips):

| Shape                              | On disk | Zipped   |
| ---------------------------------- | ------- | -------- |
| Framework-dependent (`--self-contained false`) | 70 MB | 22.6 MB |
| NativeAOT (4.7 MB binary + native lib)         | 84 MB | 24.1 MB |

### Containers on glibc Linux — works, prefer this

**Verified:** the framework-dependent publish ran the full transform on `amazonlinux:2023`
(glibc 2.34, `dnf install dotnet-runtime-10.0` — AL2023 ships .NET 10 in its own repo) and on
the Debian-based .NET images. This is the recommended shape: no trim/AOT caveats, ~22.6 MB of
app on top of a runtime base image (`mcr.microsoft.com/dotnet/runtime:10.0`, 234 MB for the
aspnet variant).

### NativeAOT — works, with three observed caveats

**Verified:** `dotnet publish -c Release -r linux-x64` with `PublishAot=true` compiles and the
published binary runs the engine transform end-to-end (`RuntimeFeature.IsDynamicCodeSupported ==
false`; P/Invoke into `libduckdb.so` is unaffected by AOT). Warning counts observed
(`TrimmerSingleWarn=false`): **2 from this extension** (IL2026/IL3050, one call site), **10 from
DuckDB.NET.Data** (List/Map/Struct vector readers and the prepared-statement converter — none on
the flat-schema Parquet path this extension uses), **0 from Flowthru.Core**.

1. **`Flowthru:DuckDb` config binding silently no-ops under AOT.** `UseDuckDb()` binds options
   with the reflection-based `ConfigurationBinder.Bind` — under AOT the section is ignored and
   options keep their defaults, with no error (observed: `Threads=null` where JIT binds
   `Threads=3`). Code-first `UseDuckDb(opts => ...)` works under AOT and is the recommended
   configuration path for AOT apps. Verified fix (planned): building the extension with
   `<EnableConfigurationBindingGenerator>true</EnableConfigurationBindingGenerator>` makes the
   section bind correctly under AOT and removes both extension warnings.
2. **Build on a glibc no newer than your target.** An AOT binary built on a rolling-release host
   (glibc 2.42) failed on AL2023 with `GLIBC_2.38 not found`. Building inside an
   `amazonlinux:2023` container (`dnf install dotnet-sdk-10.0 clang zlib-devel`) produced a
   binary that runs on AL2023 — do AOT publishes in a container matching the deploy target.
3. **Minimal images need invariant globalization.** On `public.ecr.aws/lambda/provided:al2023`
   (no ICU) the AOT binary fail-fasts at startup (`Couldn't find a valid ICU package`); with
   `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` (or `<InvariantGlobalization>true</>`) it passes.

### AWS Lambda — two shapes, both fit the limits

Both artifact shapes fit Lambda's packaging limits (50 MB zipped direct upload / 250 MB
unzipped: measured 22.6–24.1 MB zipped, 70–84 MB on disk), and the engine workload itself is
**verified on Lambda's OS base**: the AL2023-built AOT spike ran the full transform on
`public.ecr.aws/lambda/provided:al2023` with `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`.

- **Container image (prefer):** build the AOT publish in an AL2023 builder stage, copy onto
  `public.ecr.aws/lambda/provided:al2023` (121 MB base) with your handler bootstrap, set the
  invariant-globalization env var. Sidesteps the zip limit entirely and pins glibc at build time.
- **Zip + managed runtime:** the framework-dependent zip (22.6 MB) fits direct upload; make sure
  `libduckdb.so` lands at the package root (publish with `-r linux-x64`), and prefer 1769 MB+
  memory (a full vCPU) since DuckDB is threaded.

**Unverified (repro included):** an actual Lambda invocation through the Runtime Interface
Emulator or a deployed function — the spike ran the workload directly on the base image, not
behind a `Amazon.Lambda.RuntimeSupport` handler. To close: wrap the transform in a minimal
`lambda_handler`, `podman build` the image above, run it locally with
`podman run -p 9000:8080 <image>` (RIE is bundled in the AWS base images) and
`curl -d '{}' http://localhost:9000/2015-03-31/functions/function/invocations`. Also note
Lambda's writable disk is `/tmp` (512 MB default, configurable to 10 GB) — point
`TempDirectory` there and treat spills beyond it as a hard bound. arm64 was not exercised
(x64-only host); the package does bundle `linux-arm64`.

### Alpine / musl — does not work, use a glibc image

Unsupported at the packaging level and confirmed at runtime, in three observed failure modes:

1. `DuckDB.NET.Bindings.Full` 1.5.3 bundles **no `linux-musl-*` native library** (package
   inspection above).
2. On `mcr.microsoft.com/dotnet/runtime:10.0-alpine`, loading fails with
   `DllNotFoundException: Unable to load shared library 'duckdb'` — the bundled `.so` is
   glibc-linked.
3. With the `gcompat` + `libstdc++` shim installed, the process **segfaults** (SIGSEGV, exit
   139) while loading the engine — the shim does not carry DuckDB.

A glibc-based image (`runtime:10.0`, AL2023, Debian slim) is the supported path; the size
difference against Alpine is far smaller than the 67 MB engine you're shipping anyway. Repro:

```bash
podman run --rm -v ./publish:/app:ro mcr.microsoft.com/dotnet/runtime:10.0-alpine \
  sh -c 'apk add --no-cache gcompat libstdc++; dotnet /app/YourApp.dll'
```

### Memory-constrained hosts (1 GB Fargate task)

DuckDB's default `memory_limit` is 80% of available RAM, and it **is cgroup-aware** (observed:
`819.1 MiB` inside a 1 GiB-capped container, `100.4 GiB` on a 125 GiB host) — but 80% of a 1 GB
task leaves ~200 MB for the CLR, GC, and everything else, and DuckDB's limit governs its buffer
manager, not every allocation. Set it explicitly:

```csharp
b.UseDuckDb(opts =>
{
  opts.MaxConcurrentTransforms = 1;   // default — peak engine memory = 1 × MemoryLimit
  opts.MemoryLimit = "512MB";         // leaves ~500 MB for the CLR and headroom
  opts.Threads = 1;                   // match the task's vCPU (0.25–0.5 at 1 GB)
  opts.TempDirectory = "/tmp/duckdb-spill";  // Fargate ephemeral storage (20 GB default)
});
```

Work beyond `MemoryLimit` spills to `TempDirectory` instead of failing, so undersizing the
limit costs speed, not correctness. Raise `MaxConcurrentTransforms` only when
`MaxConcurrentTransforms × MemoryLimit` plus CLR headroom fits the task.
