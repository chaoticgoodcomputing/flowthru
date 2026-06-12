# Flowthru.Extensions.Excel

Read Flowthru Catalog Items from Excel workbooks. Adds the Excel **format** to the Catalog
builder, so any Item backed by a row sequence loads from a `.xlsx` worksheet with a one-line
declaration. Excel here is **read-only** — it's for ingesting spreadsheets someone handed you,
not for emitting them; an Excel-backed Item reports that it can't be written, and a Flow that
tries to save to one fails fast before it touches the workbook.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_excel)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

Storage in Flowthru is three independent axes: **format** (how bytes serialize) × **medium**
(where bytes live) × **container** (the in-memory shape). This package supplies one format —
Excel. Bring the spreadsheet mental model: a workbook holds named sheets, a sheet has a header
row, and each row below it is a record. You name the sheet to read; Flowthru maps each row onto
your schema. It doesn't care where the `.xlsx` lives (that's the medium's job), only how a row
maps to your typed schema.

## Install

```bash
dotnet add package Flowthru.Extensions.Excel
```

Declare an Excel-backed Item in your Catalog. Unlike the other formats, Excel requires a sheet
name — a workbook can hold many sheets, so you say which one:

```csharp
public IItem<IEnumerable<ShuttleSchema>> Shuttles =>
    CreateItem(() => Item.Of<IEnumerable<ShuttleSchema>>("Shuttles")
        .Excel()
        .AtPath($"{_basePath}/Data/_01_Raw/shuttles.xlsx")
        .WithSheet("Sheet1")
        .Build());
```

If your nullable columns use placeholder strings for blanks, declare them with `.WithNullValues(...)`
so they parse as `null` rather than literal text.
