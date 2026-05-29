using Flowthru.Data.Storage.Sheets.Internal;
using Flowthru.Prelude;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// Production <see cref="ISheetsGateway"/> backed by Google's official
/// <see cref="SheetsService"/> client. This is the only class in the extension
/// that references <c>Google.Apis.Sheets.v4</c>: it translates the neutral
/// tabular schema and rows to and from Google's typed tables and values via
/// <see cref="SheetsTranslator"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>No credentials here.</strong> A <see cref="SheetsService"/> arrives
/// fully authenticated from the composition root — by injection or via a
/// factory. The gateway never loads, stores, or sees a secret.
/// </para>
/// <para>
/// <strong>Two construction modes</strong> (mirroring the EF Core adapter):
/// </para>
/// <list type="bullet">
/// <item><strong>Injected</strong> — the container owns the
/// <see cref="SheetsService"/> lifetime; the gateway never disposes it.</item>
/// <item><strong>Factory</strong> — the gateway acquires one
/// <see cref="SheetsService"/> per flow run via <see cref="FlowResource"/> and
/// disposes it on release.</item>
/// </list>
/// </remarks>
public sealed class SheetsServiceGateway : ISheetsGateway, IFlowResourceProvider
{
  // Field mask for ResolveTable: only sheet ids and tables, never cell grid.
  private const string TablesFieldMask = "sheets(properties(sheetId),tables)";

  private readonly SheetsService? _injected;
  private readonly Func<SheetsService>? _factory;

  // Factory mode only: the client acquired for the duration of the current
  // flow run. Set on resource acquire, cleared on release.
  private SheetsService? _acquired;

  /// <summary>
  /// Build a gateway over a container-owned <see cref="SheetsService"/>. The
  /// gateway does not dispose it.
  /// </summary>
  public SheetsServiceGateway(SheetsService service)
  {
    _injected = service ?? throw new ArgumentNullException(nameof(service));
  }

  /// <summary>
  /// Build a gateway over a <see cref="SheetsService"/> factory. One client is
  /// created per flow run (via <see cref="FlowResource"/>) and disposed when
  /// the run completes.
  /// </summary>
  public SheetsServiceGateway(Func<SheetsService> factory)
  {
    _factory = factory ?? throw new ArgumentNullException(nameof(factory));
  }

  // ── IFlowResourceProvider (factory-mode lifecycle) ──────────────────────

  /// <summary>
  /// The flow-scoped resource that owns the factory-built client. Injected
  /// mode returns <see langword="null"/> — the container owns the lifetime, so
  /// there is nothing to bracket.
  /// </summary>
  public IFlowResource? FlowResource
  {
    get
    {
      if (_factory is null) return null;

      return Prelude.FlowResource.Make<SheetsService>(
        acquire: FlowIO.LiftAsync<SheetsService>(_ =>
        {
          var service = _factory();
          _acquired = service;
          return Task.FromResult(service);
        }, source: "SheetsServiceGateway.acquire"),
        release: (service, _) =>
          FlowIO.Lift<FlowUnit>(() =>
          {
            // Release runs on every exit path; clear the field first so a
            // disposed client is never reused, then dispose.
            _acquired = null;
            service.Dispose();
            return FlowUnit.Default;
          }, source: "SheetsServiceGateway.release"));
    }
  }

  // ── ISheetsGateway ──────────────────────────────────────────────────────

  /// <inheritdoc/>
  public async Task<ResolvedTable?> ResolveTable(string spreadsheetId, string tableName, CancellationToken ct)
  {
    var service = ActiveService();
    var request = service.Spreadsheets.Get(spreadsheetId);
    request.Fields = TablesFieldMask;
    var spreadsheet = await request.ExecuteAsync(ct).ConfigureAwait(false);

    var table = FindTable(spreadsheet, tableName);
    return SheetsTranslator.ToResolvedTable(table);
  }

  /// <inheritdoc/>
  public async Task<TableData> ReadRows(string spreadsheetId, ResolvedTable table, CancellationToken ct)
  {
    var service = ActiveService();
    var range = table.Range;
    var dataStartRow = range.StartRowIndex + 1;

    // No data rows below the header — nothing to read.
    if (range.EndRowIndex <= dataStartRow)
    {
      return TableData.Empty(table.Schema);
    }

    // Read the data region by GridRange (sheet id + bounds) so no tab title is
    // needed; UNFORMATTED_VALUE + SERIAL_NUMBER keeps types raw for the adapter.
    var body = new BatchGetValuesByDataFilterRequest
    {
      DataFilters = new List<DataFilter>
      {
        new()
        {
          GridRange = new GridRange
          {
            SheetId = range.SheetId,
            StartRowIndex = dataStartRow,
            EndRowIndex = range.EndRowIndex,
            StartColumnIndex = range.StartColumnIndex,
            EndColumnIndex = range.EndColumnIndex,
          },
        },
      },
      ValueRenderOption = "UNFORMATTED_VALUE",
      DateTimeRenderOption = "SERIAL_NUMBER",
    };

    var response = await service.Spreadsheets.Values
      .BatchGetByDataFilter(body, spreadsheetId)
      .ExecuteAsync(ct)
      .ConfigureAwait(false);

    var valueRange = response.ValueRanges?.FirstOrDefault()?.ValueRange;
    return SheetsTranslator.FromValueRange(table.Schema, valueRange);
  }

  /// <inheritdoc/>
  public async Task ReplaceRows(string spreadsheetId, ResolvedTable table, TableData rows, CancellationToken ct)
  {
    var service = ActiveService();

    var batch = SheetsTranslator.BuildReplaceBatch(table, rows);

    // Nothing to clear and nothing to write — skip the round-trip.
    if (batch.Requests is null || batch.Requests.Count == 0) return;

    await service.Spreadsheets.BatchUpdate(batch, spreadsheetId).ExecuteAsync(ct).ConfigureAwait(false);
  }

  /// <inheritdoc/>
  public async Task<ResolvedTable> AddTable(
    string spreadsheetId, string tableName, TableSchema schema, CancellationToken ct)
  {
    var service = ActiveService();

    // AddTable needs a sheet to anchor on; create on the first tab. Resolving
    // the spreadsheet's sheets also lets a follow-up resolve return the range.
    var spreadsheet = await SpreadsheetTables(service, spreadsheetId, ct).ConfigureAwait(false);
    var sheetId = spreadsheet.Sheets?.FirstOrDefault()?.Properties?.SheetId
      ?? throw new InvalidOperationException(
        $"Spreadsheet '{spreadsheetId}' has no sheet to create table '{tableName}' on.");

    var batch = SheetsTranslator.BuildAddTableBatch(tableName, schema, sheetId);
    await service.Spreadsheets.BatchUpdate(batch, spreadsheetId).ExecuteAsync(ct).ConfigureAwait(false);

    // Re-resolve so the caller gets the table's authoritative range and schema
    // (incl. the column-index-0 coalesce applied on read-back).
    var created = await ResolveTable(spreadsheetId, tableName, ct).ConfigureAwait(false);
    return created
      ?? throw new InvalidOperationException(
        $"Created table '{tableName}' could not be resolved in spreadsheet '{spreadsheetId}'.");
  }

  // ── Internals ───────────────────────────────────────────────────────────

  private SheetsService ActiveService()
  {
    if (_injected is not null) return _injected;
    if (_acquired is not null) return _acquired;
    throw new InvalidOperationException(
      "SheetsServiceGateway has no active SheetsService. In factory mode the "
      + "client is acquired per flow run via IFlowResource; calling the gateway "
      + "outside a flow run is unsupported.");
  }

  private static async Task<Spreadsheet> SpreadsheetTables(
    SheetsService service, string spreadsheetId, CancellationToken ct)
  {
    var request = service.Spreadsheets.Get(spreadsheetId);
    request.Fields = TablesFieldMask;
    return await request.ExecuteAsync(ct).ConfigureAwait(false);
  }

  private static Table? FindTable(Spreadsheet spreadsheet, string tableName)
  {
    if (spreadsheet.Sheets is null) return null;
    foreach (var sheet in spreadsheet.Sheets)
    {
      if (sheet.Tables is null) continue;
      foreach (var table in sheet.Tables)
      {
        if (string.Equals(table.Name, tableName, StringComparison.Ordinal))
        {
          return table;
        }
      }
    }
    return null;
  }
}
