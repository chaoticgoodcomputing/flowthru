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

## Concurrency and memory

Each transform may use the engine's full memory budget (`MemoryLimit`, spilling to
`TempDirectory` beyond it), so the engine registers with the scheduler as a capacity-constrained
resource: at most `MaxConcurrentTransforms` DuckDB steps run at once (default `1`), regardless of
the flow's `Parallelism`. Peak engine memory is therefore
`MaxConcurrentTransforms × MemoryLimit`. All knobs bind from the `Flowthru:DuckDb` configuration
section or code-first via `UseDuckDb(opts => ...)`.

## Limitations (honest ones)

- **Local files only.** Endpoints must be backed by local file storage. An item whose bytes live
  behind a remote URI (e.g. `s3://`) fails the step with a typed error (`FTDDB4001`); S3-backed
  transforms are planned. Non-file-backed items (memory, database) are rejected at wire-up.
- **Parquet endpoints only.** Inputs are bound via `read_parquet`; the output is written with
  `COPY ... (FORMAT PARQUET)`. Other formats (CSV, Postgres via `ATTACH`) are planned.
- **Transforms are never cached — loudly.** The SQL text is wire-up data that isn't part of the
  step's cache identity yet. Rather than risk serving stale output after a query edit, the step
  declares itself uncacheable, and the reason surfaces wherever cache decisions are reported.
  Query-aware cache identity is planned.
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
