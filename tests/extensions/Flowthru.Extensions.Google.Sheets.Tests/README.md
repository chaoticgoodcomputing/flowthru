# Google Sheets extension tests

Most tests here are offline and need no setup. One suite —
`SheetsGatewayLaws<TBackend>` — is a **backend matrix**: the same
`ISheetsGateway` contract laws run over two backends.

| Tier | Backend | Runs when | CI |
| --- | --- | --- | --- |
| Offline | `OfflineSheetsBackend` (`JsonFileSheetsGateway` over a temp file) | always | yes |
| Live | `LiveGoogleSheetsBackend` (real `SheetsService`) | only with credentials | no — opt-in |

The live tier follows the `tests/extensions/CONTRIBUTING.md` **Test capability
gate**: without credentials it reports **Inconclusive** (skipped), never a
failure. CI runs the offline tier; the live tier is the developer's local OAuth
run, or the enterprise user's service-account run. Both tiers run the identical
law suite, so the offline double's behaviour is checked against real Google.

## Running the live tier

The live backend talks to a **pre-created** Google Spreadsheet (the suite
creates tables, never spreadsheets — so only the `spreadsheets` OAuth scope is
needed, no Drive scope). It creates tables under a unique per-run name prefix
and deletes only those tables in teardown; sibling tables and tabs are never
touched, so it is safe to point at a shared scratch spreadsheet.

### Environment variables

| Variable | Required | Meaning |
| --- | --- | --- |
| `FLOWTHRU_SHEETS_TEST_SPREADSHEET_ID` | always | id of a pre-created spreadsheet (the `…/d/<ID>/edit` segment of its URL) |
| `FLOWTHRU_SHEETS_SA_KEY` | one of these two | path to a service-account JSON key |
| `FLOWTHRU_SHEETS_OAUTH_CLIENT_SECRET` | one of these two | path to an OAuth desktop client-secret JSON |

The credential type is auto-detected (service-account key preferred when both
are set). The capability gate is satisfied when the spreadsheet id is set **and**
one credential path points at an existing file — it never opens a browser or a
network connection; the `SheetsService` is built only after the gate clears.

### Option A — OAuth (a user runs the live tier, now)

1. In a Google Cloud project, enable the **Google Sheets API**.
2. Create an **OAuth client ID** of type **Desktop app**; download the client
   secret JSON.
3. Create (or reuse) a spreadsheet in your own Drive; grab its id from the URL.
4. Export and run:

   ```sh
   export FLOWTHRU_SHEETS_TEST_SPREADSHEET_ID=<spreadsheet-id>
   export FLOWTHRU_SHEETS_OAUTH_CLIENT_SECRET=/abs/path/to/client_secret.json
   dotnet test tests/extensions/Flowthru.Extensions.Google.Sheets.Tests \
     --filter "FullyQualifiedName~SheetsGatewayLaws"
   ```

   The **first** run opens a browser for consent (scope: `spreadsheets`) and
   caches the token in a `FileDataStore` (`flowthru-sheets-laws-token`); later
   runs are non-interactive. Headless/CI environments cannot complete this
   consent — that is by design: the live tier is opt-in and local.

### Option B — service account (the enterprise tier, later)

1. Enable the **Google Sheets API** and create a **service account**; download
   its JSON key.
2. **Share** the test spreadsheet with the service account's
   `client_email` (Editor).
3. Export and run:

   ```sh
   export FLOWTHRU_SHEETS_TEST_SPREADSHEET_ID=<spreadsheet-id>
   export FLOWTHRU_SHEETS_SA_KEY=/abs/path/to/service-account.json
   dotnet test tests/extensions/Flowthru.Extensions.Google.Sheets.Tests \
     --filter "FullyQualifiedName~SheetsGatewayLaws"
   ```

   No browser, no token cache — the service account authenticates
   non-interactively.

## Where the pieces live

- Laws: `Contract/SheetsGatewayLaws.cs`
- Backends: `Backends/OfflineSheetsBackend.cs`, `Backends/LiveGoogleSheetsBackend.cs`
- Backend interface + rationale (why this kit does **not** inherit
  `FlowResourceLaws`): `Support/ISheetsGatewayBackend.cs`
- Capability: `TestCapabilities.GoogleSheetsCredentials` in
  `tests/helpers/Flowthru.Tests.Kits/Prelude/TestCapabilities.cs`
