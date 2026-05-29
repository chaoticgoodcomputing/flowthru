namespace Flowthru.Data.Storage.Sheets.InMemory;

/// <summary>
/// An offline, JSON-backed <see cref="ISheetsGateway"/> that faithfully emulates
/// the four seam operations against an in-memory <see cref="InMemorySheetsStore"/>
/// (<c>spreadsheetId → tableName → { schema, rows }</c>), with <strong>zero</strong>
/// Google dependency. Backs the starter example and the test suite, mirroring the
/// <c>Microsoft.EntityFrameworkCore.InMemory</c> precedent.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Deterministic by default.</strong> With default
/// <see cref="InMemorySheetsOptions"/> there is no write quota and no fault
/// injection — every operation succeeds and the store is fully reproducible.
/// </para>
/// <para>
/// <strong>Opt-in fault injection.</strong> Setting
/// <see cref="InMemorySheetsOptions.WritesPerMinute"/> turns on quota
/// enforcement: write operations (<see cref="ReplaceRows"/>,
/// <see cref="AddTable"/>) record a timestamp against the injected
/// <see cref="TimeProvider"/>, and a write that would exceed the per-minute
/// window throws a transient <see cref="SheetsRateLimitException"/> — the same
/// neutral type the production gateway maps Google's <c>429</c> onto.
/// </para>
/// <para>
/// <strong>Not thread-safe across instances of the underlying store.</strong>
/// The gateway guards its own state with a lock so concurrent calls on one
/// instance are serialized, but the seam contract is one gateway per catalog
/// per flow run.
/// </para>
/// </remarks>
public sealed class InMemorySheetsGateway : ISheetsGateway
{
  private static readonly TimeSpan QuotaWindow = TimeSpan.FromSeconds(60);

  private readonly object _gate = new();
  private readonly InMemorySheetsStore _store;
  private readonly InMemorySheetsOptions _options;

  // Write timestamps within the current rolling window. Only populated when a
  // quota is configured; empty (and never touched) otherwise.
  private readonly Queue<DateTimeOffset> _writeTimes = new();

  // Synthetic sheet ids handed out to spreadsheets created implicitly by a
  // write to an unseen id, so each spreadsheet's tab id is stable per process.
  private int _nextSheetId;

  /// <summary>Build a gateway over a fresh, empty store.</summary>
  public InMemorySheetsGateway(InMemorySheetsOptions? options = null)
    : this(new InMemorySheetsStore(), options)
  {
  }

  /// <summary>
  /// Build a gateway over an existing <paramref name="store"/> — e.g. one loaded
  /// from JSON via <see cref="InMemorySheetsStore.FromJson"/> to fixture data.
  /// The gateway takes ownership; mutate it only through the gateway thereafter.
  /// </summary>
  public InMemorySheetsGateway(InMemorySheetsStore store, InMemorySheetsOptions? options = null)
  {
    _store = store ?? throw new ArgumentNullException(nameof(store));
    _options = options ?? new InMemorySheetsOptions();
    _nextSheetId = _store.Spreadsheets.Count == 0
      ? 0
      : _store.Spreadsheets.Values.Max(s => s.SheetId) + 1;
  }

  // ── Seeding / inspection ────────────────────────────────────────────────

  /// <summary>
  /// Register an empty spreadsheet under <paramref name="spreadsheetId"/> so it
  /// becomes reachable — the offline analogue of a spreadsheet existing in
  /// Drive. A gateway no longer creates a spreadsheet implicitly on first write;
  /// an unregistered spreadsheet is "missing" and surfaces as a
  /// <see cref="SheetsSpreadsheetAccessException"/>, faithfully simulating a
  /// 404. Idempotent: registering an already-registered id is a no-op (the
  /// spreadsheet and its tables are left intact).
  /// </summary>
  public void RegisterSpreadsheet(string spreadsheetId)
  {
    ArgumentNullException.ThrowIfNull(spreadsheetId);
    lock (_gate)
    {
      _ = GetOrCreateSpreadsheet(spreadsheetId);
    }
  }

  /// <summary>
  /// Register a table with <paramref name="schema"/> and optional
  /// <paramref name="rows"/> directly in the store, bypassing quota — the
  /// programmatic fixture entry point for the example and tests. Throws if a
  /// table by that name already exists in the spreadsheet (same unique-name rule
  /// as <see cref="AddTable"/>).
  /// </summary>
  public void Seed(
    string spreadsheetId,
    string tableName,
    TableSchema schema,
    IEnumerable<IReadOnlyList<FieldValue>>? rows = null)
  {
    ArgumentNullException.ThrowIfNull(schema);
    lock (_gate)
    {
      var spreadsheet = GetOrCreateSpreadsheet(spreadsheetId);
      if (spreadsheet.Tables.ContainsKey(tableName))
      {
        throw new InvalidOperationException(
          $"Table '{tableName}' already exists in spreadsheet '{spreadsheetId}'.");
      }

      spreadsheet.Tables[tableName] = new InMemoryTable
      {
        Columns = schema.Columns.Select(c => new InMemoryColumn(c.Name, c.Type)).ToList(),
        Rows = (rows ?? Enumerable.Empty<IReadOnlyList<FieldValue>>())
          .Select(r => r.ToList())
          .ToList(),
      };
    }
  }

  /// <summary>Snapshot the store as JSON, for assertions and fixtures.</summary>
  public string ToJson()
  {
    lock (_gate)
    {
      return _store.ToJson();
    }
  }

  // ── ISheetsGateway ──────────────────────────────────────────────────────

  /// <inheritdoc/>
  public Task<ResolvedTable?> ResolveTable(string spreadsheetId, string tableName, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    lock (_gate)
    {
      // An unregistered spreadsheet is "missing" — a hard failure, distinct from
      // a table being absent. This is what lets pre-flight tell 404 apart from
      // table-not-found.
      var spreadsheet = RequireSpreadsheet(spreadsheetId);

      if (!spreadsheet.Tables.TryGetValue(tableName, out var table))
      {
        // Null-when-absent is load-bearing for a present spreadsheet: pre-flight
        // and create-if-absent branch on it.
        return Task.FromResult<ResolvedTable?>(null);
      }

      return Task.FromResult<ResolvedTable?>(Resolve(spreadsheet, tableName, table));
    }
  }

  /// <inheritdoc/>
  public Task<TableData> ReadRows(string spreadsheetId, ResolvedTable table, CancellationToken ct)
  {
    ArgumentNullException.ThrowIfNull(table);
    ct.ThrowIfCancellationRequested();
    lock (_gate)
    {
      var stored = Find(spreadsheetId, table.Name)
        ?? throw new InvalidOperationException(
          $"Table '{table.Name}' does not exist in spreadsheet '{spreadsheetId}'.");

      // Read back under the resolved schema, copying rows so the caller cannot
      // mutate the store through the returned data. Date/DateTime/Time columns
      // are normalized to serial Numbers to mirror the live gateway, which reads
      // with UNFORMATTED_VALUE + SERIAL_NUMBER and so never returns a Temporal —
      // a column seeded either way round-trips identically through Load.
      var columns = table.Schema.Columns;
      var rows = stored.Rows
        .Select(r => (IReadOnlyList<FieldValue>)NormalizeRow(r, columns))
        .ToList();
      return Task.FromResult(new TableData(table.Schema, rows));
    }
  }

  /// <inheritdoc/>
  public Task ReplaceRows(string spreadsheetId, ResolvedTable table, TableData rows, CancellationToken ct)
  {
    ArgumentNullException.ThrowIfNull(table);
    ArgumentNullException.ThrowIfNull(rows);
    ct.ThrowIfCancellationRequested();
    lock (_gate)
    {
      var stored = Find(spreadsheetId, table.Name)
        ?? throw new InvalidOperationException(
          $"Table '{table.Name}' does not exist in spreadsheet '{spreadsheetId}'.");

      // Quota check happens BEFORE any mutation, so a rejected write leaves the
      // table untouched — the transient failure is honestly all-or-nothing.
      ThrowIfQuotaExceeded();

      // Build the full replacement first, then swap in one assignment — the
      // header/schema (stored Columns) is preserved, only the data rows change,
      // and no partially-written state is ever observable.
      var replacement = rows.Rows.Select(r => r.ToList()).ToList();
      stored.Rows.Clear();
      stored.Rows.AddRange(replacement);

      RecordWrite();
      return Task.CompletedTask;
    }
  }

  /// <inheritdoc/>
  public Task<ResolvedTable> AddTable(
    string spreadsheetId, string tableName, TableSchema schema, CancellationToken ct)
  {
    ArgumentNullException.ThrowIfNull(schema);
    ct.ThrowIfCancellationRequested();
    lock (_gate)
    {
      // Flowthru creates tables, not spreadsheets: the spreadsheet must already
      // exist (the live AddTable needs a sheet to anchor on). An unregistered id
      // is a missing spreadsheet, not an implicit create.
      var spreadsheet = RequireSpreadsheet(spreadsheetId);
      if (spreadsheet.Tables.ContainsKey(tableName))
      {
        // Mirror the real API's unique-name rule: creating a duplicate is an
        // error, not a silent overwrite.
        throw new InvalidOperationException(
          $"Table '{tableName}' already exists in spreadsheet '{spreadsheetId}'.");
      }

      ThrowIfQuotaExceeded();

      var table = new InMemoryTable
      {
        Columns = schema.Columns.Select(c => new InMemoryColumn(c.Name, c.Type)).ToList(),
        Rows = new List<List<FieldValue>>(),
      };
      spreadsheet.Tables[tableName] = table;

      RecordWrite();
      return Task.FromResult(Resolve(spreadsheet, tableName, table));
    }
  }

  // ── Internals ───────────────────────────────────────────────────────────

  // Normalize a stored row to the live read representation: any cell in a
  // Date/DateTime/Time column becomes a serial Number, regardless of whether it
  // was seeded as Temporal (convert via ToSerial) or already as a Number/serial
  // (passed through). Non-temporal columns are copied verbatim.
  private static List<FieldValue> NormalizeRow(
    IReadOnlyList<FieldValue> row, IReadOnlyList<TableColumn> columns)
  {
    var normalized = new List<FieldValue>(row.Count);
    for (var c = 0; c < row.Count; c++)
    {
      var field = row[c];
      var isTemporalColumn = c < columns.Count && IsTemporalColumn(columns[c].Type);
      normalized.Add(isTemporalColumn ? ToSerialNumber(field) : field);
    }
    return normalized;
  }

  private static bool IsTemporalColumn(ColumnType type) =>
    type is ColumnType.Date or ColumnType.DateTime or ColumnType.Time;

  // Coerce a temporal-column cell into the serial Number the live gateway emits.
  // A Temporal is converted to its serial; a Number is already a serial and
  // passes through; anything else (Empty, or a mis-typed cell) is left as-is so
  // the schema-driven decoder reports it the same way it would for live data.
  private static FieldValue ToSerialNumber(FieldValue field) => field.Kind switch
  {
    FieldKind.Temporal => FieldValue.Number(Internal.SheetsTranslator.ToSerial(field.TemporalValue)),
    _ => field,
  };

  private InMemoryTable? Find(string spreadsheetId, string tableName)
  {
    // Unregistered spreadsheet → hard failure, the same 404-shape ResolveTable
    // surfaces; a registered spreadsheet missing the table → null.
    var spreadsheet = RequireSpreadsheet(spreadsheetId);
    return spreadsheet.Tables.TryGetValue(tableName, out var table) ? table : null;
  }

  // Look up a spreadsheet that must already be registered; an unknown id is a
  // missing spreadsheet (the offline analogue of Google's 404), distinct from a
  // table being absent within it.
  private InMemorySpreadsheet RequireSpreadsheet(string spreadsheetId)
  {
    if (_store.Spreadsheets.TryGetValue(spreadsheetId, out var spreadsheet))
    {
      return spreadsheet;
    }

    throw new SheetsSpreadsheetAccessException(
      spreadsheetId,
      SheetsSpreadsheetAccessFailure.NotFound,
      $"Spreadsheet '{spreadsheetId}' does not exist. Register it with "
      + $"{nameof(RegisterSpreadsheet)} or {nameof(Seed)} before using it.");
  }

  private InMemorySpreadsheet GetOrCreateSpreadsheet(string spreadsheetId)
  {
    if (_store.Spreadsheets.TryGetValue(spreadsheetId, out var existing))
    {
      return existing;
    }

    var spreadsheet = new InMemorySpreadsheet { SheetId = _nextSheetId++ };
    _store.Spreadsheets[spreadsheetId] = spreadsheet;
    return spreadsheet;
  }

  private static ResolvedTable Resolve(
    InMemorySpreadsheet spreadsheet, string tableName, InMemoryTable table)
  {
    var schema = table.ToSchema();
    // Synthetic range: header row at 0, data rows beneath, columns 0..width.
    // Half-open end-row = 1 (header) + data row count.
    var range = new TableRange(
      SheetId: spreadsheet.SheetId,
      StartRowIndex: 0,
      EndRowIndex: 1 + table.Rows.Count,
      StartColumnIndex: 0,
      EndColumnIndex: schema.ColumnCount);
    return new ResolvedTable(tableName, schema, range);
  }

  // Quota is off unless WritesPerMinute is set. When on, evict timestamps older
  // than the rolling window, then reject if the window is already full.
  private void ThrowIfQuotaExceeded()
  {
    if (_options.WritesPerMinute is not { } limit) return;

    var now = _options.Clock.GetUtcNow();
    EvictExpired(now);

    if (_writeTimes.Count >= limit)
    {
      var oldest = _writeTimes.Peek();
      var retryAfter = oldest + QuotaWindow - now;
      throw new SheetsRateLimitException(
        $"In-memory Sheets write quota of {limit}/min exceeded.",
        retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero);
    }
  }

  private void RecordWrite()
  {
    if (_options.WritesPerMinute is null) return;
    _writeTimes.Enqueue(_options.Clock.GetUtcNow());
  }

  private void EvictExpired(DateTimeOffset now)
  {
    var cutoff = now - QuotaWindow;
    while (_writeTimes.Count > 0 && _writeTimes.Peek() <= cutoff)
    {
      _writeTimes.Dequeue();
    }
  }
}
