---
name: flowthru-duckdb
description: Deep skill for the Flowthru DuckDB extension — running a wide transform (sort, dedup, aggregate, join) as SQL inside the embedded DuckDB engine, between Parquet Catalog Items, with no rows entering .NET. Use when a step is wide and the data is large, when a flow has an AddDuckDbTransform call, or when in-memory LINQ over millions of rows is the bottleneck. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.DuckDB
    surface: engine
    capability: Run a wide transform as SQL in the embedded DuckDB engine between Parquet Items; rows never enter .NET, CLR memory stays flat.
    register: b.UseDuckDb(…)
---

# flowthru-duckdb

Runs a step's **entire body as SQL** inside the embedded [DuckDB](https://duckdb.org/) OLAP engine, wired between ordinary Parquet Catalog Items. The step reads its input Parquet files, transforms, and writes its output file **without a single row entering the .NET runtime**. On the DAG it schedules, validates, and renders like any other step — only its execution happens engine-side.

Bring the DuckDB mental model: an in-process engine that queries Parquet files where they sit — no server, no import step. Each input Item becomes a SQL relation (named after its Item label), your query is the step body, and the result lands directly at the output Item's path.

**Reach for it** when the step is **wide** — it must consume all input before emitting output (global sort, dedup, aggregate, join) — *and* the data is large. Wide work as in-memory LINQ pays a 10–20× tax once rows reach the millions; delegating it to the engine also keeps CLR memory flat. Row-at-a-time logic (map, filter) stays in ordinary C# steps, where it composes with the rest of your code.

## Register

```bash
dotnet add package Flowthru.Extensions.DuckDB
```

Enable the engine in `AddFlowthru`, then register flows with `IDuckDbEngine` as a generic so the runner injects it into the flow factory:

```csharp
services.AddFlowthru(configuration, b =>
{
  b.RegisterCatalog<Catalog>(sp => new Catalog());
  b.UseDuckDb(opts => opts.MemoryLimit = "4GB");
  b.RegisterFlow<Catalog, IDuckDbEngine>("Reporting", ReportingFlow.Create);
});
```

## Wire a transform

Single input — the relation is named after the Item's label, so `FROM ModelInputTable` binds to `catalog.ModelInputTable`:

<!-- flowthru:snippet:docs:transform-duckdb:start -->
```csharp
pipeline.AddDuckDbTransform(
  label: "SummarizeCompanies",
  input: catalog.ModelInputTable,
  output: catalog.CompanySummaries,
  sql: """
    SELECT
      company_id,
      COUNT(*)                                   AS shuttle_count,
      CAST(AVG(price) AS DOUBLE)                 AS avg_price,
      CAST(AVG(review_scores_rating) AS DOUBLE)  AS avg_review_score,
      CAST(SUM(passenger_capacity) AS BIGINT)    AS total_passenger_capacity
    FROM ModelInputTable
    GROUP BY company_id
    ORDER BY avg_review_score DESC, company_id
    """,
  engine: engine
);
```
<!-- flowthru:snippet:docs:transform-duckdb:end -->
_(real source: [ReportingFlow.cs](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/SpaceflightsDuckDB/Flows/Reporting/ReportingFlow.cs))_

For a **join**, pass `inputs:` a list of `DuckDbInputRelation.From(item, "name")` bindings and reference each `name` in the SQL. Column names in the SQL are the Schemas' serialized labels (the names in the Parquet files), not the C# property names. Full worked example: [SpaceflightsDuckDB](https://github.com/chaoticgoodcomputing/flowthru/tree/main/examples/starter/SpaceflightsDuckDB).

## Schema validation — happens before any data is read

`UseDuckDb()` registers a **hermetic pre-flight check** for every transform: empty tables are built from the declared input Schemas, the SQL is `DESCRIBE`d (bound, not executed) against them, and the described result is verified against the declared output Schema. No data is read and nothing outside the process is reached, so it runs at every `ValidationDepth` from `Hermetic` up — a schema-breaking SQL edit fails even an offline smoke test. It is also a **design-time surface**: call `flow.ValidateDuckDbTransforms()` from a unit test (or `FUnitContext.Validate(step)` for one step) and a bad edit fails your test run in milliseconds.

## Gotchas

- **Aggregates widen — `CAST` to match.** `SUM(INTEGER)` comes back `HUGEINT`, `AVG` comes back `DOUBLE`. A result column must round-trip losslessly into the declared property type (widenings accepted, narrowings not), so `CAST` each aggregate onto the type the output Schema declares — as the snippet above does. Skipping this is the most common `FTDDB3002`.
- **`FTDDB30xx` — pre-flight schema failures.** `FTDDB3001`: SQL won't prepare against the declared inputs (unknown column/relation, syntax error). `FTDDB3002`: result schema doesn't satisfy the declared output (missing/extra column, incompatible type) — the message names the step, relation binding, and offending column(s). `FTDDB3003`: an input Schema has a property the checks can't model (nested/`IScalar`).
- **`FTDDB40xx` — endpoint/runtime failures.** `FTDDB4001`: a non-`file://`/`s3://` scheme (`https://`, `ftp://`). `FTDDB4003`: `httpfs` missing with `AllowExtensionDownload = false` — the S3 extension isn't statically linked, so the first S3 transform on a host downloads it once unless pre-provisioned.
- **Parquet endpoints only** (inputs via `read_parquet`, output via `COPY … (FORMAT PARQUET)`). Local files and `s3://` objects only; non-byte-addressable Items (memory, database) are rejected at wire-up. Schema checks cover flat primitive/enum schemas.
- **Serialized by capacity, not `Parallelism`.** Each transform may use the engine's full `MemoryLimit`, so the engine caps concurrent transforms at `MaxConcurrentTransforms` (default `1`) regardless of the flow's `Parallelism`; peak engine memory is `MaxConcurrentTransforms × MemoryLimit`.
- **Cache identity is the SQL text.** Any edit to the SQL (no normalization), the engine version, relation bindings, or output write options busts the cache. Tuning that can't change output values (`MemoryLimit`, `Threads`, `TempDirectory`) is excluded, so re-tuning a host never re-runs steps.
