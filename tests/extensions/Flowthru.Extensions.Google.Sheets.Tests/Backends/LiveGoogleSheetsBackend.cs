using Flowthru.Data.Storage.Sheets;
using Flowthru.Extensions.Google.Sheets.Tests.Support;
using Flowthru.Tests.Kits.Prelude;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

namespace Flowthru.Extensions.Google.Sheets.Tests.Backends;

/// <summary>
/// Live backend for <see cref="Contract.SheetsGatewayLaws{TBackend}"/>: builds a
/// real authenticated <see cref="SheetsService"/> and runs the exact same law
/// suite against Google as the offline tier runs against the JSON store. This is
/// the live-path coverage <see cref="SheetsServiceGateway"/> cannot reach
/// offline.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Opt-in, never a CI failure.</strong> Declares
/// <see cref="TestCapabilities.GoogleSheetsCredentials"/> as a required
/// capability, so the laws kit's <c>OneTimeSetUp</c> yields <em>Inconclusive</em>
/// (not failure) when no test spreadsheet + credential is configured. CI runs
/// the offline tier; the live tier is the developer's local OAuth run or the
/// enterprise user's service-account run.
/// </para>
/// <para>
/// <strong>Pluggable credentials (OAuth now / SA later, same suite).</strong>
/// <see cref="InitializeAsync"/> reads the env (gated behind the capability) and
/// auto-detects the credential type, exactly like the spike:
/// <list type="bullet">
///   <item><c>FLOWTHRU_SHEETS_SA_KEY</c> → a service-account JSON →
///     <c>GoogleCredential.FromFile(path).CreateScoped(spreadsheets)</c>
///     (preferred when both are set).</item>
///   <item><c>FLOWTHRU_SHEETS_OAUTH_CLIENT_SECRET</c> → an OAuth desktop client
///     secret → <c>GoogleWebAuthorizationBroker.AuthorizeAsync(...)</c> with a
///     <see cref="FileDataStore"/> (first local run consents in a browser;
///     cached token reused after).</item>
/// </list>
/// Scope is <c>spreadsheets</c> only — no Drive scope — and the spreadsheet is
/// <strong>pre-created</strong> (the suite creates tables, never spreadsheets).
/// </para>
/// <para>
/// <strong>Disjoint state inside one shared spreadsheet.</strong> Every test's
/// tables share the configured spreadsheet, so isolation comes from a
/// unique-per-resource table-name prefix
/// (<see cref="SheetsGatewayContext.TableNamePrefix"/>). All prefixes a fixture
/// hands out share a common run prefix, and <see cref="Cleanup"/> deletes
/// <em>only</em> the tables whose names start with that run prefix — via
/// <c>DeleteTableRequest</c> keyed on the table id — so sibling tables and tabs
/// in the shared spreadsheet are never touched. (The seam has no
/// <c>DeleteTable</c>; cleanup talks to the SDK directly, as a test harness may,
/// without changing gateway logic.)
/// </para>
/// </remarks>
[Category("RequiresGoogleSheets")]
public sealed class LiveGoogleSheetsBackend : ISheetsGatewayBackend
{
  // One OAuth scope: spreadsheets. No Drive — the test sheet is pre-created.
  private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

  private const string ApplicationName = "flowthru-sheets-laws";
  private const string TokenStore = "flowthru-sheets-laws-token";

  // A run prefix shared by every table this fixture creates, so cleanup can
  // target exactly this run's tables and nothing else in the shared sheet.
  private readonly string _runPrefix = $"flowthru_laws_{Guid.NewGuid():N}_";

  private SheetsService? _service;
  private string? _spreadsheetId;
  private int _counter;

  public IReadOnlyList<TestCapability> RequiredCapabilities { get; } =
    [TestCapabilities.GoogleSheetsCredentials];

  public async Task InitializeAsync()
  {
    _spreadsheetId = Environment.GetEnvironmentVariable("FLOWTHRU_SHEETS_TEST_SPREADSHEET_ID")
      ?? throw new InvalidOperationException(
        "FLOWTHRU_SHEETS_TEST_SPREADSHEET_ID must be set. The capability gate "
        + "should have prevented this path when it is absent.");

    var initializer = await BuildInitializerAsync().ConfigureAwait(false);
    _service = new SheetsService(initializer);
  }

  public SheetsGatewayContext CreateResource()
  {
    if (_service is null || _spreadsheetId is null)
    {
      throw new InvalidOperationException(
        "LiveGoogleSheetsBackend.CreateResource() called before InitializeAsync(). "
        + "The laws kit's OneTimeSetUp wires this after the capability gate clears.");
    }

    var n = Interlocked.Increment(ref _counter);
    return new SheetsGatewayContext(
      Gateway: new SheetsServiceGateway(_service),
      SpreadsheetId: _spreadsheetId,
      TableNamePrefix: $"{_runPrefix}r{n}_");
  }

  public async Task Cleanup()
  {
    if (_service is null || _spreadsheetId is null) return;

    try
    {
      // Find every table whose name starts with this run's prefix and delete
      // it by id. Names outside the run prefix — sibling tables this fixture
      // did not create — are left untouched.
      var get = _service.Spreadsheets.Get(_spreadsheetId);
      get.Fields = "sheets(tables(name,tableId))";
      var book = await get.ExecuteAsync().ConfigureAwait(false);

      var deletions = new List<Request>();
      foreach (var sheet in book.Sheets ?? [])
      {
        foreach (var table in sheet.Tables ?? [])
        {
          if (table.Name is { } name
            && name.StartsWith(_runPrefix, StringComparison.Ordinal)
            && table.TableId is { } id)
          {
            deletions.Add(new Request { DeleteTable = new DeleteTableRequest { TableId = id } });
          }
        }
      }

      if (deletions.Count > 0)
      {
        await _service.Spreadsheets
          .BatchUpdate(new BatchUpdateSpreadsheetRequest { Requests = deletions }, _spreadsheetId)
          .ExecuteAsync()
          .ConfigureAwait(false);
      }
    }
    catch
    {
      // Best-effort teardown: a cleanup failure must not fail the fixture.
    }
    finally
    {
      _service.Dispose();
      _service = null;
    }
  }

  // Auto-detect SA vs OAuth from the env, mirroring the spike. SA is preferred
  // when both are present.
  private static async Task<BaseClientService.Initializer> BuildInitializerAsync()
  {
    var saKey = Environment.GetEnvironmentVariable("FLOWTHRU_SHEETS_SA_KEY");
    if (!string.IsNullOrWhiteSpace(saKey) && File.Exists(saKey))
    {
      var credential = GoogleCredential.FromFile(saKey).CreateScoped(Scopes);
      return new BaseClientService.Initializer
      {
        HttpClientInitializer = credential,
        ApplicationName = ApplicationName,
      };
    }

    var oauthSecret = Environment.GetEnvironmentVariable("FLOWTHRU_SHEETS_OAUTH_CLIENT_SECRET");
    if (!string.IsNullOrWhiteSpace(oauthSecret) && File.Exists(oauthSecret))
    {
      // Desktop/installed app: opens a browser for consent on first run, then
      // caches the token in the FileDataStore so later runs are non-interactive.
      await using var secretStream = File.OpenRead(oauthSecret);
      var secrets = (await GoogleClientSecrets.FromStreamAsync(secretStream).ConfigureAwait(false)).Secrets;
      var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
        secrets,
        Scopes,
        user: "flowthru-laws",
        CancellationToken.None,
        new FileDataStore(TokenStore)).ConfigureAwait(false);
      return new BaseClientService.Initializer
      {
        HttpClientInitializer = credential,
        ApplicationName = ApplicationName,
      };
    }

    throw new InvalidOperationException(
      "No usable Google credential found. The capability gate should have "
      + "prevented this path. Set FLOWTHRU_SHEETS_SA_KEY or "
      + "FLOWTHRU_SHEETS_OAUTH_CLIENT_SECRET to an existing file.");
  }
}
