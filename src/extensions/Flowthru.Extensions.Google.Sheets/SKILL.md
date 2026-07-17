---
name: flowthru-google-sheets
description: Deep skill for the Flowthru Google Sheets extension — reading and writing a spreadsheet tab as a typed Catalog Item in a Flowthru (.NET) pipeline. Use when a Flow's input or output lives in a Google Sheet, or when a non-technical consumer needs to edit source rows or read results in a spreadsheet. Pairs with the umbrella `flowthru` skill.
metadata:
  flowthru:
    extension: Flowthru.Extensions.Google.Sheets
    surface: medium
    capability: A Google Sheets tab as a typed Item, addressed by (spreadsheetId, tableName) — read source rows, write derived rows back, offline or live.
    register: b.AddGoogleSheets(…)
---

# flowthru-google-sheets

Adds a **Google Sheets medium** to the Catalog: an Item whose bytes live in a spreadsheet tab instead of a file. This is the *medium* axis of a catalog item (format × medium × container — see the `flowthru` umbrella skill's `catalog-developers.md`); it decides where rows live, not their on-disk encoding.

Bring your spreadsheet mental model: a **tab is a table**, the header row names the columns, one record per row. An Item is addressed by `(spreadsheetId, tableName)` — a stable native-table name, *not* a fragile `A1:D50` cell range. On read, the factory matches the tab's columns to your `[FlowthruSchema]` record's properties **by name** and coerces each cell to the declared type. On write, the output table is created from the schema on first write if absent, then **atomically replaced** each run (replace is the upsert — Flowthru owns the whole table).

**Reach for it** at the raw edge where a human maintains source rows in a sheet, or at the reporting edge where a consumer reads results in one. Everything reaches the spreadsheet through a swappable `ISheetsGateway`, so the Catalog and Flow never name a credential or a network call.

## Register (auth is the host's job)

Reference the package, then register a gateway on the Flowthru builder. The extension owns **no credentials** — you hand it either an authenticated client or the offline file-backed gateway:

```bash
dotnet add package Flowthru.Extensions.Google.Sheets
```

```csharp
// Production: an authenticated SheetsService (service account / OAuth / ADC).
flowthru.AddGoogleSheets(sheetsService);         // retry-on-429 wrapped by default

// Offline / tests: a file-backed gateway — no account, no network. This is the
// swap point; the Catalog and Flow do not change between the two.
var gateway = new JsonFileSheetsGateway(workingPath);
flowthru.AddGoogleSheets(gateway);
flowthru.RegisterCatalog(_ => new Catalog(gateway, spreadsheetId));
```

The Catalog takes the `ISheetsGateway` by injection (like the EF Core example's injected `DbContext`), so it never sees auth. `AddGoogleSheets` also accepts a `Func<SheetsService>` (fresh client per run) and a `SheetsRetryOptions` to tune backoff; `AddGoogleSheetsWithoutRetry` opts out.

## Declare a Sheets-backed Item

<!-- flowthru:snippet:docs:item-google-sheets:start -->
```csharp
public IItem<IEnumerable<RawSaleSchema>> RawSales =>
  CreateItem(() => ItemFactory.Enumerable.GoogleSheets<RawSaleSchema>(
    label: "RawSales",
    spreadsheetId: _spreadsheetId,
    tableName: "RawSales",
    gateway: _sheets));
```
<!-- flowthru:snippet:docs:item-google-sheets:end -->

_(real source: [GoogleSheets `Catalog.Raw.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/GoogleSheets/Data/_01_Raw/Catalog.Raw.cs))_

## Gotchas (FTGS diagnostics)

These fire at **pre-flight** (before Step logic runs) — the codes are grep-able in build/run output:

- **`FTGS1601` spreadsheet not found / `FTGS1602` access denied** — bad `spreadsheetId` or the auth principal lacks access. Share the sheet with your service account's email.
- **`FTGS1603` table not found** — the spreadsheet is reachable but has no tab named `tableName`. A *read* fails here; a *write* creates the table instead. Names are matched case-insensitively.
- **`FTGS1604` missing column** — the tab lacks a header your schema requires. Extra live columns are tolerated; missing required ones fail fast.
- **`FTGS1605` column type mismatch** — a header exists but a cell won't coerce to the property's declared type. Only fires for externally-created or hand-edited tables; a Flowthru-created table always round-trips.
- **`FTGS1606` deserialization** — a row failed to decode into the schema.
- **`FTGS1607` retry exhausted** — transient `429`s outlasted the backoff window; tune `SheetsRetryOptions` (defaults target the ~60-writes/min quota).
- **`FTGS1608` write ceiling** — a single write exceeded the batch ceiling. **Not retried** and not chunked — it fails loudly rather than truncating. Split the dataset or write to a file medium.
