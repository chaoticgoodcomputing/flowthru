# Flowthru.Extensions.Parquet

Read and write Flowthru Catalog Items as Parquet files. Adds the Parquet **format** to the
Catalog builder, so any Item backed by a row sequence serializes to and from `.parquet` with a
one-line declaration. Parquet is the format of choice for intermediate and primary data: it's
binary, columnar, and compressed, so it round-trips large typed row sets far faster and smaller
than CSV.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_parquet)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

Storage in Flowthru is three independent axes: **format** (how bytes serialize) × **medium**
(where bytes live) × **container** (the in-memory shape). This package supplies one format —
Parquet. Bring the columnar mental model: instead of one record per line, values are grouped by
column into compressed row groups, so reads can skip columns and writes pack tightly. You don't
manage any of that — you declare the Item, and Flowthru maps your schema to and from the columnar
layout. It doesn't care where the `.parquet` lives (that's the medium's job).

## Install

```bash
dotnet add package Flowthru.Extensions.Parquet
```

Declare a Parquet-backed Item in your Catalog:

```csharp
public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("PreprocessedShuttles")
        .Parquet()
        .AtPath($"{_basePath}/Data/_02_Intermediate/preprocessed_shuttles.parquet")
        .Build());
```

Defaults are Snappy compression, 1,000,000-row groups, and dictionary encoding. To tune them,
pass `.WithOptions(...)`.
