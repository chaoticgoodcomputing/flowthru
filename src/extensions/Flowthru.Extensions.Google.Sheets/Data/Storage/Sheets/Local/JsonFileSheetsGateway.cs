using System.Text.Json;
using Flowthru.Prelude;

namespace Flowthru.Data.Storage.Sheets.Local;

/// <summary>
/// A local-development <see cref="ISheetsGateway"/> backed by a single JSON file
/// on disk — a fully offline stand-in for a Google Sheet with no Google account,
/// no credentials, and no network. Read the four seam operations against a
/// snapshot loaded from the file on construction; every write flushes the whole
/// snapshot back to the file, so the JSON is always an inspectable record of the
/// sheet's contents.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Load on construct, flush on write.</strong> The file is read once when
/// the gateway is built; reads serve from that in-memory snapshot (the file is
/// not re-read per operation). A missing file starts an empty store; the file is
/// created on the first write. Each mutating call —
/// <see cref="ReplaceRows"/>, <see cref="AddTable"/>,
/// <see cref="RegisterSpreadsheet"/>, <see cref="Seed"/> — writes the full store
/// back to the path before returning.
/// </para>
/// <para>
/// <strong>Corrupt JSON is a hard failure.</strong> If the file exists but does
/// not parse as a valid store, the constructor throws — it does not silently
/// discard your data and start empty. Delete or fix the file to recover.
/// </para>
/// <para>
/// <strong>Single-process, no file locking.</strong> This gateway assumes one
/// process owns the file. It does no locking and offers no concurrency guarantee
/// across processes; a second process writing the same file concurrently can lose
/// writes. It is meant for local development and demos, not shared or production
/// storage — for that, use <c>AddGoogleSheets</c> over an authenticated
/// <c>SheetsService</c>.
/// </para>
/// <para>
/// <strong>No flow-scoped resource.</strong> There is no per-run client to
/// acquire or release, so <see cref="FlowResource"/> is <see langword="null"/>.
/// </para>
/// </remarks>
public sealed class JsonFileSheetsGateway : ISheetsGateway, IFlowResourceProvider
{
  private readonly string _path;
  private readonly LocalSheetsStore _store;

  /// <summary>
  /// Build a gateway over the JSON file at <paramref name="path"/>. The file is
  /// loaded immediately: a missing file yields an empty store; a file that fails
  /// to parse throws.
  /// </summary>
  /// <exception cref="JsonException">The file exists but is not valid store JSON.</exception>
  /// <exception cref="ArgumentException">
  /// The file is valid JSON but represents a null store.
  /// </exception>
  public JsonFileSheetsGateway(string path)
  {
    _path = path ?? throw new ArgumentNullException(nameof(path));
    _store = File.Exists(_path)
      ? LocalSheetsStore.FromJson(File.ReadAllText(_path))
      : new LocalSheetsStore();
  }

  // ── No flow-scoped resource ─────────────────────────────────────────────────

  /// <inheritdoc/>
  public IFlowResource? FlowResource => null;

  // ── Seeding / registration (flush on write) ─────────────────────────────────

  /// <summary>
  /// Register an empty spreadsheet so it becomes reachable, then flush. Idempotent.
  /// See <see cref="LocalSheetsStore.RegisterSpreadsheet"/>.
  /// </summary>
  public void RegisterSpreadsheet(string spreadsheetId)
  {
    _store.RegisterSpreadsheet(spreadsheetId);
    Flush();
  }

  /// <summary>
  /// Seed a table with a schema and optional rows directly, then flush — the
  /// programmatic fixture entry point for local setup. See
  /// <see cref="LocalSheetsStore.Seed"/>.
  /// </summary>
  public void Seed(
    string spreadsheetId,
    string tableName,
    TableSchema schema,
    IEnumerable<IReadOnlyList<FieldValue>>? rows = null)
  {
    _store.Seed(spreadsheetId, tableName, schema, rows);
    Flush();
  }

  // ── ISheetsGateway ──────────────────────────────────────────────────────────

  /// <inheritdoc/>
  public Task<ResolvedTable?> ResolveTable(string spreadsheetId, string tableName, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    return Task.FromResult(_store.ResolveTable(spreadsheetId, tableName));
  }

  /// <inheritdoc/>
  public Task<TableData> ReadRows(string spreadsheetId, ResolvedTable table, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    return Task.FromResult(_store.ReadRows(spreadsheetId, table));
  }

  /// <inheritdoc/>
  public Task ReplaceRows(string spreadsheetId, ResolvedTable table, TableData rows, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    _store.ReplaceRows(spreadsheetId, table, rows);
    Flush();
    return Task.CompletedTask;
  }

  /// <inheritdoc/>
  public Task<ResolvedTable> AddTable(
    string spreadsheetId, string tableName, TableSchema schema, CancellationToken ct)
  {
    ct.ThrowIfCancellationRequested();
    var created = _store.AddTable(spreadsheetId, tableName, schema);
    Flush();
    return Task.FromResult(created);
  }

  // ── Internals ────────────────────────────────────────────────────────────────

  private void Flush() => File.WriteAllText(_path, _store.ToJson());
}
