using Flowthru.Data.Storage.Sheets.Local;
using Flowthru.Prelude;

namespace Flowthru.Data.Storage.Sheets.InMemory;

/// <summary>
/// A test-only, in-memory <see cref="ISheetsGateway"/> over a
/// <see cref="LocalSheetsStore"/> — the general adapter/factory/pre-flight test
/// double and the rate-limit-test workhorse. It reuses the store for all four
/// seam ops, register/seed, the #94 date→serial read normalization, and JSON
/// de/serialize, and layers on the test-only concerns the shipped gateways do
/// not have: an opt-in write quota driven by an injected <see cref="TimeProvider"/>
/// that throws the transient <see cref="SheetsRateLimitException"/> on breach.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Deterministic by default.</strong> With default
/// <see cref="InMemorySheetsOptions"/> there is no write quota and no fault
/// injection — every operation succeeds and the store is fully reproducible.
/// </para>
/// <para>
/// <strong>Opt-in fault injection.</strong> Setting
/// <see cref="InMemorySheetsOptions.WritesPerMinute"/> turns on quota enforcement:
/// write operations (<see cref="ReplaceRows"/>, <see cref="AddTable"/>) record a
/// timestamp against the injected <see cref="TimeProvider"/>, and a write that
/// would exceed the per-minute window throws a transient
/// <see cref="SheetsRateLimitException"/> — the same neutral type the production
/// gateway maps Google's <c>429</c> onto. The quota check runs before any
/// mutation, so a rejected write leaves the store untouched.
/// </para>
/// <para>
/// Lives in the test suite, not the shipped extension: it exists to exercise
/// retry/backoff and the adapter against an offline gateway. The store it wraps
/// is the same one the shipped <see cref="JsonFileSheetsGateway"/> uses, so its
/// op behavior is identical to production-local; only the quota is synthetic.
/// </para>
/// </remarks>
public sealed class InMemorySheetsGateway : ISheetsGateway, IFlowResourceProvider
{
  private static readonly TimeSpan QuotaWindow = TimeSpan.FromSeconds(60);

  private readonly object _gate = new();
  private readonly LocalSheetsStore _store;
  private readonly InMemorySheetsOptions _options;

  // Write timestamps within the current rolling window. Only populated when a
  // quota is configured; empty (and never touched) otherwise.
  private readonly Queue<DateTimeOffset> _writeTimes = new();

  /// <summary>Build a gateway over a fresh, empty store.</summary>
  public InMemorySheetsGateway(InMemorySheetsOptions? options = null)
    : this(new LocalSheetsStore(), options)
  {
  }

  /// <summary>
  /// Build a gateway over an existing <paramref name="store"/> — e.g. one loaded
  /// from JSON via <see cref="LocalSheetsStore.FromJson"/> to fixture data. The
  /// gateway takes ownership; mutate it only through the gateway thereafter.
  /// </summary>
  public InMemorySheetsGateway(LocalSheetsStore store, InMemorySheetsOptions? options = null)
  {
    _store = store ?? throw new ArgumentNullException(nameof(store));
    _options = options ?? new InMemorySheetsOptions();
  }

  // ── No flow-scoped resource ─────────────────────────────────────────────────

  /// <inheritdoc/>
  public IFlowResource? FlowResource => null;

  // ── Seeding / inspection ────────────────────────────────────────────────────

  /// <summary>Register an empty spreadsheet so it becomes reachable. Idempotent.</summary>
  public void RegisterSpreadsheet(string spreadsheetId)
  {
    lock (_gate)
    {
      _store.RegisterSpreadsheet(spreadsheetId);
    }
  }

  /// <summary>
  /// Seed a table with a schema and optional rows directly in the store,
  /// bypassing quota — the programmatic fixture entry point for tests.
  /// </summary>
  public void Seed(
    string spreadsheetId,
    string tableName,
    TableSchema schema,
    IEnumerable<IReadOnlyList<FieldValue>>? rows = null)
  {
    lock (_gate)
    {
      _store.Seed(spreadsheetId, tableName, schema, rows);
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

  // ── ISheetsGateway ──────────────────────────────────────────────────────────

  /// <inheritdoc/>
  public Task<ResolvedTable?> ResolveTable(string spreadsheetId, string tableName, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    lock (_gate)
    {
      return Task.FromResult(_store.ResolveTable(spreadsheetId, tableName));
    }
  }

  /// <inheritdoc/>
  public Task<TableData> ReadRows(string spreadsheetId, ResolvedTable table, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    lock (_gate)
    {
      return Task.FromResult(_store.ReadRows(spreadsheetId, table));
    }
  }

  /// <inheritdoc/>
  public Task ReplaceRows(string spreadsheetId, ResolvedTable table, TableData rows, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    lock (_gate)
    {
      // Quota check happens BEFORE any mutation, so a rejected write leaves the
      // table untouched — the transient failure is honestly all-or-nothing.
      ThrowIfQuotaExceeded();
      _store.ReplaceRows(spreadsheetId, table, rows);
      RecordWrite();
      return Task.CompletedTask;
    }
  }

  /// <inheritdoc/>
  public Task<ResolvedTable> AddTable(
    string spreadsheetId, string tableName, TableSchema schema, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    lock (_gate)
    {
      // A duplicate name throws from the store before the quota is recorded; the
      // quota check itself runs before the store mutates so a rejected create
      // leaves nothing behind.
      ThrowIfQuotaExceeded();
      var created = _store.AddTable(spreadsheetId, tableName, schema);
      RecordWrite();
      return Task.FromResult(created);
    }
  }

  // ── Quota (test-only fault injection) ────────────────────────────────────────

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
