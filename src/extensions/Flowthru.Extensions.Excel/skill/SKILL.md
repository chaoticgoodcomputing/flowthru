---
name: flowthru-excel
description: Deep skill for the Flowthru Excel format extension — declaring read-only Excel-backed Catalog Items in a Flowthru (.NET) pipeline. Use when a project ingests a `.xlsx` worksheet someone handed you at the raw edge. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.Excel
    surface: format
    capability: Read-only .xlsx worksheet format for row sequences — ingest a named sheet at the raw edge; cannot be written.
    register: "— (declare a .Excel() item)"
---

# flowthru-excel

Adds the **Excel format** to the Catalog. This is one axis of a catalog item (format × medium × container — see the `flowthru` umbrella skill's `catalog-developers.md`); it decides how bytes serialize, not where they live.

**Reach for Excel** at the raw layer, to ingest a spreadsheet someone handed you. Bring the workbook mental model: a `.xlsx` holds named sheets, each sheet has a header row, and each row below it is a record. You name the sheet to read; Flowthru maps each row onto your `[FlowthruSchema]` record. For intermediate and primary data you own, prefer Parquet.

## Use it

Reference the package — there is **no `UseXxx()` call**. Once referenced, `.Excel()` is available on the item builder:

```bash
dotnet add package Flowthru.Extensions.Excel
```

Declare an Excel-backed Item. Unlike the other formats, Excel **requires a sheet name** — a workbook can hold many sheets, so `.WithSheet(...)` says which one:

<!-- flowthru:snippet:docs:item-excel:start -->
```csharp
/// <summary>Raw shuttle data imported from external sources (Excel).</summary>
public IItem<IEnumerable<ShuttleSchema>> Shuttles =>
  CreateItem(() => Item.Of<IEnumerable<ShuttleSchema>>("Shuttles")
    .Excel()
    .AtPath($"{_basePath}/_01_Raw/Datasets/shuttles.xlsx")
    .WithSheet("Sheet1")
    .Build());
```
_(source: [`Spaceflights/Catalog.Raw.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/Spaceflights/Data/_01_Raw/Catalog.Raw.cs))_
<!-- flowthru:snippet:docs:item-excel:end -->

## Notes

- **Read-only:** Excel is for ingesting, not emitting. An Excel-backed Item reports that it can't be written, and a Flow that tries to save to one fails fast (pre-flight) before it touches the workbook. To write a spreadsheet-shaped output, save to CSV instead.
- **Sheet is mandatory:** omit `.WithSheet(...)` and the declaration won't resolve a target — always name the sheet you want.
- **Container:** Excel backs row-sequence items (`IItem<IEnumerable<TSchema>>`), mapping each row under the header onto your schema record.
- **Nulls:** if nullable columns use placeholder strings for blanks, declare them with `.WithNullValues(...)` so they parse as `null` rather than literal text.
- **Medium is orthogonal:** `.Excel()` doesn't care where the `.xlsx` lives (that's the medium's job). Combine with the Http or S3 medium (their own skills) to read a remote workbook with the same declaration.
