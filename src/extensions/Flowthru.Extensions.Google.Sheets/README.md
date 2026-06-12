# Flowthru.Extensions.Google.Sheets

Read and write Google Sheets as Flowthru Catalog Items. A spreadsheet tab becomes a typed
Item addressed by `(spreadsheetId, tableName)` — a stable native-table name, not a fragile cell
range — so a Flow reads rows from one tab and writes derived rows to another the same way it
reads and writes a CSV. The factory matches a table's columns to your schema's properties by
name and coerces each cell to the declared type on read; an output table is created on first
write if it is absent, then atomically replaced each run.

[![coverage](https://codecov.io/gh/chaoticgoodcomputing/flowthru/branch/main/graph/badge.svg?component=flowthru_extensions_google_sheets)](https://codecov.io/gh/chaoticgoodcomputing/flowthru)

## Mental model

A Flowthru Catalog declares typed Items; this package supplies Items backed by a Google
spreadsheet instead of a file. Everything reaches the spreadsheet through a swappable
`ISheetsGateway`, so the Catalog and the Flow never name a credential or a network call. Bring
your spreadsheet mental model — a tab is a table, the header row names the columns, one record
per row — and Flowthru handles the typed mapping to your schema. The extension owns no
credentials: you supply an authenticated `SheetsService`, or hand it the shipped file-backed
gateway to develop fully offline.

## Install

```bash
dotnet add package Flowthru.Extensions.Google.Sheets
```

Register a gateway and a Catalog whose Items route through it. The offline file-backed gateway
is the swap point — replace it with `AddGoogleSheets(sheetsService)` over an authenticated
client for production, with no change to the Catalog or Flow:

```csharp
var gateway = new JsonFileSheetsGateway(workingPath);   // offline; swap for a live SheetsService

services.AddFlowthru(flowthru =>
{
    flowthru.AddGoogleSheets(gateway);                  // retry-on-429 wrapped by default
    flowthru.RegisterCatalog(_ => new Catalog(gateway, spreadsheetId));

    flowthru
        .RegisterFlow<Catalog>("Sales", SalesFlow.Create)
        .WithDescription("Totals raw sales by day and writes the result back to the spreadsheet");
});
```

Declare a Sheets-backed Item in your Catalog:

```csharp
public IItem<IEnumerable<RawSaleSchema>> RawSales =>
    CreateItem(() => ItemFactory.Enumerable.GoogleSheets<RawSaleSchema>(
        label: "RawSales",
        spreadsheetId: _spreadsheetId,
        tableName: "RawSales",
        gateway: _sheets));
```
