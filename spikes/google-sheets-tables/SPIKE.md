# Spike: Google Sheets native Tables via a service account

## What we're testing

Whether a **service account** can create and read a native Google Sheets [Table](https://developers.google.com/workspace/sheets/api/guides/tables) through the Sheets API, and whether **typed columns round-trip** (`TableColumnProperties.ColumnType` survives a write → read). This gates the [ADR-0018](/.claude/docs/adr/0018-google-sheets-catalog-extension.md) decision to adopt a table-addressed, schema-bearing model for `Flowthru.Extensions.Google.Sheets` instead of plain tab + A1 range.

Desk research already retired the *tier* risk — native tables are available to all Workspace editions, Workspace Individual, and personal accounts, with no edition gate. The open question is: does `AddTableRequest` succeed via the API on a writable sheet, and what column-type strings come back.

**Auth caveat.** v1 uses service-account auth, so the strongest verification runs the spike with a service-account key. If you don't have one (e.g. your org blocks SA-key download), run it via **OAuth** as yourself — this proves the `AddTable` API path works and that column types round-trip, but not the service-account-specific principal. That residual is low-risk (tables are an ordinary `batchUpdate` write with no documented human-only gate) and is closed definitively when real SA auth is wired in the README work. The spike auto-detects which credential type you pass — the extension itself is auth-agnostic (it consumes a DI'd `SheetsService`), so this only affects how *you* build the client.

## Prerequisites (you provide)

All paths need a (free) Google Cloud project with the **Sheets API enabled**, plus .NET 10, and a **Google Sheet you can edit** (its ID is the long token in `docs.google.com/spreadsheets/d/<spreadsheetId>/edit`).

- **OAuth (no service account needed):** OAuth consent screen set to **External** with your own account added as a **Test user**; an **OAuth client ID of type "Desktop app"**, downloaded as `client_secret_*.json`. Use any sheet you own.
- **Service account:** a service-account **JSON key**; the target sheet **shared with the service-account email as Editor**.

## How to run

```bash
# OAuth (opens a browser for consent on first run; caches the token after)
dotnet run --project spikes/google-sheets-tables -- /path/to/client_secret.json <spreadsheetId>

# Service account
dotnet run --project spikes/google-sheets-tables -- /path/to/service-account.json <spreadsheetId>
```

The spike creates a tab `FlowthruTablesSpike`, writes a header + two rows, issues an `AddTable` with three typed columns (`TEXT`, `DOUBLE`, `DATE_TIME`), then reads the table metadata and values back.

### Zero-GCP alternative: Google Apps Script

The REST API (and thus `Program.cs`) cannot run without a GCP project. To answer the same question with **no GCP project at all**, use [`AppsScript.gs`](./AppsScript.gs) — Apps Script's advanced Sheets service is the same API v4, but Apps Script supplies the project transparently:

1. Open your Google Sheet → **Extensions → Apps Script**.
2. Paste in `AppsScript.gs`.
3. In the editor, **Services (+) → add "Sheets API"**.
4. Run `spikeTables()`, approve the auth prompt, then **View → Logs**.

This verifies the API capability (the gating question) but, like OAuth, not the service-account principal — that residual is closed when real SA auth is wired in the README work.

## What success looks like

```
[1/4] Auth OK (OAuth user)
[2/4] Wrote header + 2 rows to FlowthruTablesSpike!A1:C3
[3/4] AddTable OK  -> table 'FlowthruSpikeTable' id=...
[4/4] Read-back: table range startRow=0 endRow=3
       col 0  Name    TEXT
       col 1  Amount  DOUBLE
       col 2  When    DATE_TIME
PASS: the authenticated principal can create + read a typed Table; column types round-trip.
```

→ Adopt the two-layer table model (ADR-0018). Report the exact returned `ColumnType` strings — they pin down the CLR→columnType mapping.

## What failure looks like

- **403 on `AddTable`** — the authenticated principal lacks edit access. For OAuth, confirm you consented and can edit the sheet; for a service account, confirm the sheet is shared with the SA email as Editor. (If plain writes also 403, it's a generic access issue, not a table restriction.)
- **400 `INVALID_ARGUMENT` naming a column type** — our guessed `ColumnType` strings are wrong; the error lists the valid set. Report it; it directly informs the mapping.
- **`AddTable` rejected for any table-specific reason** — tables are not creatable under service-account auth. This would push us back to tab + A1 range for v1; report the full error.

## Conclusion

_(Capture the run result as a comment on the relevant milestone issue, then delete this directory per the spikes lifecycle.)_
