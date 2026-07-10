# Wide-Transform Benchmark — Eager Step vs Engine Transform

> {{verdict}}

Both paths ran the **same optimize pass** — sort by the composite key
(`DeviceId`, `Channel`, `ObservedAt`), keep the first-ingested row per key,
prune the lineage columns — over the **same fabricated Parquet input** at each
size ({{sizes}} rows). The eager path is an ordinary C# LINQ Step
(`OrderBy`/`ThenBy` + `DistinctBy` + projection) that materialises every row
in the CLR; the engine path is one SQL statement executed inside the embedded
DuckDB engine, so no row ever enters the .NET runtime. After each pair of
runs the harness verified both outputs agree row-for-row before recording the
measurement.

## Per-size comparison

| Input rows | Output rows | Eager ms | Engine ms | Speedup | Eager alloc (MiB) | Engine alloc (MiB) | Alloc ratio |
|-----------:|------------:|---------:|----------:|--------:|------------------:|-------------------:|------------:|
{{comparison_table_rows}}

- **Speedup** is eager wall-clock over engine wall-clock — above 1.00x the
  engine is faster.
- **Alloc ratio** is eager managed allocations over engine managed
  allocations. The engine path's allocations stay roughly flat as the input
  grows (the rows live in DuckDB's native memory, governed by its own
  `MemoryLimit`); the eager path allocates per row, so the ratio widens with
  size.

## Reading the numbers

The engine transform carries a fixed cost — opening its in-memory database,
binding the input Parquet, verifying the result schema — that only small
inputs fail to amortise; below the crossover (if the table shows one) the
eager Step wins. As the input grows, the eager path's per-row cost (decode
every row into a CLR object, sort object references, re-encode) grows
linearly while the engine's columnar execution scales much more gently — the
gap widens from there. Wall-clock on a shared machine naturally jitters
between runs; the shape of the table is the result, not any single cell.

<sub>Generated {{generated_utc}} by the WideTransformBenchmark Analyze Flow
from `Data/_01_Raw/Datasets/benchmark_measurements.csv`.</sub>
