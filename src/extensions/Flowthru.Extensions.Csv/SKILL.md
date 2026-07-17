---
name: flowthru-csv
description: Deep skill for the Flowthru CSV format extension — declaring CSV-backed Catalog Items in a Flowthru (.NET) pipeline. Use when a project reads or writes .csv, typically at the raw edge where an external producer hands you delimited files. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.Csv
    surface: format
    capability: Text, delimited on-disk format for row sequences — the raw-edge default when an external producer dictates CSV.
    register: "— (declare a .Csv() item)"
---

# flowthru-csv

Adds the **CSV format** to the Catalog. This is one axis of a catalog item (format × medium × container — see the `flowthru` umbrella skill's `catalog-developers.md`); it decides how bytes serialize, not where they live.

**Reach for CSV** at the raw layer, where an external producer hands you delimited files and you don't get to pick the format. Bring the ordinary CSV mental model — a header row, a delimiter, one record per line — and Flowthru maps each line onto your `[FlowthruSchema]` record. For intermediate and primary data you own, prefer Parquet (binary, columnar, compressed); CSV round-trips large typed row sets far more slowly.

## Use it

Reference the package — there is **no `UseXxx()` call**. Once referenced, `.Csv()` is available on the item builder:

```bash
dotnet add package Flowthru.Extensions.Csv
```

Declare a CSV-backed Item and point it at a path:

<!-- flowthru:snippet:docs:catalog-raw-companies:start -->
```csharp
/// <summary>Raw company data imported from external sources.</summary>
public IItem<IEnumerable<CompanySchema>> Companies =>
  CreateItem(() => Item.Of<IEnumerable<CompanySchema>>("Companies")
    .Csv()
    .AtPath($"{_basePath}/_01_Raw/Datasets/companies.csv")
    .Build());
```
_(source: [`Spaceflights/Catalog.Raw.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/Spaceflights/Data/_01_Raw/Catalog.Raw.cs))_
<!-- flowthru:snippet:docs:catalog-raw-companies:end -->

## Notes

- **Container:** CSV backs row-sequence items (`IItem<IEnumerable<TSchema>>`). It maps your schema record to and from lines — one record per line, header row from the property names — so you don't hand-parse columns.
- **Read and write:** unlike Excel (read-only), a CSV Item round-trips both ways. A Step can save its output to a `.csv` Item as well as load from one.
- **Medium is orthogonal:** `.Csv()` doesn't care whether the bytes live on local disk, S3, or elsewhere (that's the medium's job). Combine with the Http or S3 medium (their own skills) to read/write remote CSV with the same declaration.
- **Nulls:** if blank cells arrive as placeholder strings rather than empty, declare them with `.WithNullValues(...)` so they parse as `null` instead of literal text.
