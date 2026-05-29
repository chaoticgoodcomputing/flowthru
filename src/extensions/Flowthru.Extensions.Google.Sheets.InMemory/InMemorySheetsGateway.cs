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
      if (!_store.Spreadsheets.TryGetValue(spreadsheetId, out var spreadsheet)
          || !spreadsheet.Tables.TryGetValue(tableName, out var table))
      {
        // Null-when-absent is load-bearing: pre-flight and create-if-absent
        // branch on it.
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
      // mutate the store through the returned data.
      var rows = stored.Rows
        .Select(r => (IReadOnlyList<FieldValue>)r.ToList())
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
      var spreadsheet = GetOrCreateSpreadsheet(spreadsheetId);
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

  private InMemoryTable? Find(string spreadsheetId, string tableName) =>
    _store.Spreadsheets.TryGetValue(spreadsheetId, out var spreadsheet)
      && spreadsheet.Tables.TryGetValue(tableName, out var table)
        ? table
        : null;

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
