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
