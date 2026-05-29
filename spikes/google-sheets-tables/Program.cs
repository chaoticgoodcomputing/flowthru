// Throwaway spike — see SPIKE.md. Verifies a service account can create and
// read a native Sheets Table with typed columns. Verbose by design.
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

const string TabTitle = "FlowthruTablesSpike";
const string TableName = "FlowthruSpikeTable";

if (args.Length < 2)
{
    Console.Error.WriteLine(
        "usage: dotnet run --project spikes/google-sheets-tables -- <credentials.json> <spreadsheetId>");
    Console.Error.WriteLine(
        "  <credentials.json>: a service-account key OR an OAuth desktop-app client secret (auto-detected)");
    return 2;
}

var (credPath, spreadsheetId) = (args[0], args[1]);

try
{
    // ── [1/4] Auth (auto-detect: service-account key vs OAuth client secret) ──
    var credJson = await File.ReadAllTextAsync(credPath);
    using var credDoc = JsonDocument.Parse(credJson);
    var isServiceAccount =
        credDoc.RootElement.TryGetProperty("type", out var typeProp)
        && typeProp.GetString() == "service_account";

    BaseClientService.Initializer initializer;
    string principal;
    if (isServiceAccount)
    {
        var cred = GoogleCredential.FromFile(credPath).CreateScoped(SheetsService.Scope.Spreadsheets);
        initializer = new() { HttpClientInitializer = cred, ApplicationName = "flowthru-tables-spike" };
        principal = $"service account {credDoc.RootElement.GetProperty("client_email").GetString()}";
    }
    else
    {
        // OAuth desktop/installed app: opens a browser for consent on first run,
        // then caches the token so subsequent runs are non-interactive.
        using var secretStream = File.OpenRead(credPath);
        var secrets = GoogleClientSecrets.FromStream(secretStream).Secrets;
        var user = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            new[] { SheetsService.Scope.Spreadsheets },
            "user",
            CancellationToken.None,
            new FileDataStore("flowthru-tables-spike-token"));
        initializer = new() { HttpClientInitializer = user, ApplicationName = "flowthru-tables-spike" };
        principal = "OAuth user";
    }

    using var service = new SheetsService(initializer);
    Console.WriteLine($"[1/4] Auth OK ({principal})");

    // Clean slate: drop our tab if a prior run left it (also drops its table),
    // then add a fresh one. Other tabs are untouched.
    var existing = service.Spreadsheets.Get(spreadsheetId);
    existing.Fields = "sheets(properties(sheetId,title))";
    var book = existing.Execute();
    var stale = book.Sheets?.FirstOrDefault(s => s.Properties.Title == TabTitle);
    if (stale is not null)
    {
        Batch(service, spreadsheetId, new Request
        {
            DeleteSheet = new DeleteSheetRequest { SheetId = stale.Properties.SheetId },
        });
    }

    var added = Batch(service, spreadsheetId, new Request
    {
        AddSheet = new AddSheetRequest
        {
            Properties = new SheetProperties { Title = TabTitle },
        },
    });
    var sheetId = added.Replies[0].AddSheet.Properties.SheetId!.Value;

    // ── [2/4] Seed header + two data rows ────────────────────────────────
    var seed = new ValueRange
    {
        Values = new IList<object>[]
        {
            new object[] { "Name", "Amount", "When" },
            new object[] { "alpha", 12.5, "2026-01-15 09:30:00" },
            new object[] { "beta", 7, "2026-02-01 14:00:00" },
        },
    };
    var update = service.Spreadsheets.Values.Update(seed, spreadsheetId, $"{TabTitle}!A1:C3");
    update.ValueInputOption =
        SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
    update.Execute();
    Console.WriteLine($"[2/4] Wrote header + 2 rows to {TabTitle}!A1:C3");

    // ── [3/4] AddTable with typed columns ────────────────────────────────
    Table createdTable;
    try
    {
        var reply = Batch(service, spreadsheetId, new Request
        {
            AddTable = new AddTableRequest
            {
                Table = new Table
                {
                    Name = TableName,
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = 0,
                        EndRowIndex = 3,
                        StartColumnIndex = 0,
                        EndColumnIndex = 3,
                    },
                    ColumnProperties = new List<TableColumnProperties>
                    {
                        new() { ColumnIndex = 0, ColumnName = "Name", ColumnType = "TEXT" },
                        new() { ColumnIndex = 1, ColumnName = "Amount", ColumnType = "DOUBLE" },
                        new() { ColumnIndex = 2, ColumnName = "When", ColumnType = "DATE_TIME" },
                    },
                },
            },
        });
        createdTable = reply.Replies[0].AddTable.Table;
        Console.WriteLine($"[3/4] AddTable OK  -> table '{createdTable.Name}' id={createdTable.TableId}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[3/4] AddTable FAILED: {ex.Message}");
        Console.Error.WriteLine("FAIL: could not create a Table via the API. See SPIKE.md > failure.");
        return 1;
    }

    // ── [4/4] Read the table metadata + values back ──────────────────────
    var readBack = service.Spreadsheets.Get(spreadsheetId);
    readBack.Fields = "sheets(properties(sheetId,title),tables)";
    var refreshed = readBack.Execute();
    var ourTab = refreshed.Sheets!.First(s => s.Properties.SheetId == sheetId);
    var table = ourTab.Tables?.FirstOrDefault(t => t.Name == TableName);

    if (table is null)
    {
        Console.Error.WriteLine("[4/4] Read-back FAILED: table not present in Sheet.Tables after creation.");
        return 1;
    }

    Console.WriteLine($"[4/4] Read-back: table range startRow={table.Range.StartRowIndex} endRow={table.Range.EndRowIndex}");
    foreach (var col in table.ColumnProperties!.OrderBy(c => c.ColumnIndex))
    {
        Console.WriteLine($"       col {col.ColumnIndex}  {col.ColumnName,-8}{col.ColumnType}");
    }

    var values = service.Spreadsheets.Values.Get(spreadsheetId, $"{TabTitle}!A1:C3").Execute();
    Console.WriteLine($"       values rows read: {values.Values?.Count ?? 0}");

    Console.WriteLine("PASS: the authenticated principal can create + read a typed Table; column types round-trip.");
    Console.WriteLine("(Report the exact ColumnType strings above — they pin down the CLR->columnType mapping.)");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FATAL: {ex.GetType().Name}: {ex.Message}");
    return 1;
}

// Single-request batchUpdate helper.
static BatchUpdateSpreadsheetResponse Batch(SheetsService svc, string id, Request request) =>
    svc.Spreadsheets.BatchUpdate(
        new BatchUpdateSpreadsheetRequest { Requests = new[] { request } }, id).Execute();
