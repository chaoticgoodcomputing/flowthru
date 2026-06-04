# Flowthru.Extensions.Csv

Read and write Flowthru Catalog Items as CSV files. Adds the CSV **format** to the Catalog
builder, so any Item backed by a row sequence serializes to and from `.csv` with a one-line
declaration.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_csv)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

Storage in Flowthru is three independent axes: **format** (how bytes serialize) × **medium**
(where bytes live) × **container** (the in-memory shape). This package supplies one format —
CSV. It doesn't care whether the bytes live on local disk, S3, or elsewhere (that's the
medium's job), only how a row maps to a line. Bring your CSV mental model — a header row, a
delimiter, one record per line — and Flowthru handles the typed mapping to your schema.

## Install

```bash
dotnet add package Flowthru.Extensions.Csv
```

Declare a CSV-backed Item in your Catalog:

```csharp
public IItem<IEnumerable<OrderSchema>> Orders =>
    CreateItem(() => Item.Of<IEnumerable<OrderSchema>>("Orders")
        .Csv()
        .AtPath($"{_basePath}/Data/_01_Raw/orders.csv")
        .Build());
```
