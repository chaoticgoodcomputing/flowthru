using System.Text.Json;
using System.Text.Json.Serialization;
using Flowthru.Data.Storage.Sheets.Internal;

namespace Flowthru.Data.Storage.Sheets.Local;

/// <summary>
/// An offline, in-process backing store for a Google Sheet — a
/// <c>spreadsheetId → (tableName → { schema, rows })</c> map that faithfully
/// emulates the four <see cref="ISheetsGateway"/> seam operations with
/// <strong>zero</strong> Google dependency. This is the single source of truth
/// shared by every local gateway: the shipped <see cref="JsonFileSheetsGateway"/>
/// (file-backed) and the in-memory test double both delegate their op logic
/// here, so there is one implementation of resolve/read/replace/create, one
/// register/seed entry point, and one JSON shape.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The store is the data and the ops; it is not the policy.</strong>
/// It enforces the structural rules of a sheet — a spreadsheet must exist before
/// a table can be created on it, a table name is unique within a spreadsheet, a
/// replace is atomic and schema-preserving — but it imposes no quota, no fault
/// injection, and no clock. Those are gateway concerns layered on top.
/// </para>
/// <para>
/// <strong>Read normalizes dates to serial Numbers.</strong> The live
/// <c>SheetsServiceGateway</c> reads with <c>UNFORMATTED_VALUE</c> +
/// <c>SERIAL_NUMBER</c>, so a Date/DateTime/Time column always comes back as a
/// serial <see cref="FieldKind.Number"/>, never a <see cref="FieldKind.Temporal"/>.
/// The store mirrors that on read: a temporal cell in a temporal column is
/// converted to its serial, regardless of whether it was seeded as a Temporal or
/// already as a serial Number — so a date seeded either way round-trips
/// identically through the schema-driven adapter.
/// </para>
/// <para>
/// <strong>Not thread-safe.</strong> Callers that need concurrency safety (or a
/// flush-on-write side effect) wrap the store; the store itself does no locking.
/// </para>
/// </remarks>
public sealed class LocalSheetsStore
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
    // Deterministic enum rendering (Kind/Type as names, not ints).
    Converters = { new JsonStringEnumConverter() },
  };

  private readonly LocalSheetsData _data;

  // Synthetic sheet ids handed out to spreadsheets created implicitly by a
  // register/seed, so each spreadsheet's tab id is stable per process.
  private int _nextSheetId;

  /// <summary>Build a store over a fresh, empty data set.</summary>
  public LocalSheetsStore()
    : this(new LocalSheetsData())
  {
  }

  private LocalSheetsStore(LocalSheetsData data)
  {
    _data = data ?? throw new ArgumentNullException(nameof(data));
    _nextSheetId = _data.Spreadsheets.Count == 0
      ? 0
      : _data.Spreadsheets.Values.Max(s => s.SheetId) + 1;
  }

  // ── JSON de/serialize ─────────────────────────────────────────────────────

  /// <summary>Serialize the store to JSON (indented, deterministic key order).</summary>
  public string ToJson() => JsonSerializer.Serialize(_data, JsonOptions);

  /// <summary>
  /// Rehydrate a store from JSON produced by <see cref="ToJson"/>. Throws a
  /// <see cref="JsonException"/> on malformed JSON and an
  /// <see cref="ArgumentException"/> if the JSON is syntactically valid but
  /// represents a null document.
  /// </summary>
  public static LocalSheetsStore FromJson(string json)
  {
    var data = JsonSerializer.Deserialize<LocalSheetsData>(json, JsonOptions)
      ?? throw new ArgumentException("JSON deserialized to a null store.", nameof(json));
    return new LocalSheetsStore(data);
  }

  // ── Seeding / registration ────────────────────────────────────────────────

  /// <summary>
  /// Register an empty spreadsheet under <paramref name="spreadsheetId"/> so it
  /// becomes reachable — the offline analogue of a spreadsheet existing in Drive.
  /// A store never creates a spreadsheet implicitly on first write; an
  /// unregistered spreadsheet is "missing" and surfaces as a
  /// <see cref="SheetsSpreadsheetAccessException"/>, faithfully simulating a 404.
  /// Idempotent: registering an already-registered id is a no-op (the spreadsheet
  /// and its tables are left intact).
  /// </summary>
  public void RegisterSpreadsheet(string spreadsheetId)
  {
    ArgumentNullException.ThrowIfNull(spreadsheetId);
    _ = GetOrCreateSpreadsheet(spreadsheetId);
  }

  /// <summary>
  /// Register a table with <paramref name="schema"/> and optional
  /// <paramref name="rows"/> directly in the store — the programmatic fixture
  /// entry point. The spreadsheet is created if absent. Throws if a table by that
  /// name already exists in the spreadsheet (same unique-name rule as
  /// <see cref="AddTable"/>).
  /// </summary>
  public void Seed(
    string spreadsheetId,
    string tableName,
    TableSchema schema,
    IEnumerable<IReadOnlyList<FieldValue>>? rows = null)
  {
    ArgumentNullException.ThrowIfNull(schema);
    var spreadsheet = GetOrCreateSpreadsheet(spreadsheetId);
    if (spreadsheet.Tables.ContainsKey(tableName))
    {
      throw new InvalidOperationException(
        $"Table '{tableName}' already exists in spreadsheet '{spreadsheetId}'.");
    }

    spreadsheet.Tables[tableName] = new LocalSheetsTable
    {
      Columns = schema.Columns.Select(c => new LocalSheetsColumn(c.Name, c.Type)).ToList(),
      Rows = (rows ?? Enumerable.Empty<IReadOnlyList<FieldValue>>())
        .Select(r => r.ToList())
        .ToList(),
    };
  }

  // ── The four seam operations ──────────────────────────────────────────────

  /// <summary>
  /// Resolve <paramref name="tableName"/> in <paramref name="spreadsheetId"/> to
  /// its schema and grid range. Returns <see langword="null"/> when the
  /// spreadsheet is reachable but no table by that name exists; throws
  /// <see cref="SheetsSpreadsheetAccessException"/> when the spreadsheet itself is
  /// unregistered.
  /// </summary>
  public ResolvedTable? ResolveTable(string spreadsheetId, string tableName)
  {
    // An unregistered spreadsheet is "missing" — a hard failure, distinct from a
    // table being absent. This is what lets pre-flight tell 404 apart from
    // table-not-found.
    var spreadsheet = RequireSpreadsheet(spreadsheetId);

    if (!spreadsheet.Tables.TryGetValue(tableName, out var table))
    {
      // Null-when-absent is load-bearing for a present spreadsheet: pre-flight
      // and create-if-absent branch on it.
      return null;
    }

    return Resolve(spreadsheet, tableName, table);
  }

  /// <summary>
  /// Read the data rows of <paramref name="table"/> under its resolved schema,
  /// normalizing temporal columns to serial Numbers (see the type remarks). Rows
  /// are copied so the caller cannot mutate the store through the returned data.
  /// </summary>
  public TableData ReadRows(string spreadsheetId, ResolvedTable table)
  {
    ArgumentNullException.ThrowIfNull(table);
    var stored = RequireTable(spreadsheetId, table.Name);

    var columns = table.Schema.Columns;
    var rows = stored.Rows
      .Select(r => (IReadOnlyList<FieldValue>)NormalizeRow(r, columns))
      .ToList();
    return new TableData(table.Schema, rows);
  }

  /// <summary>
  /// Atomically replace the data rows of <paramref name="table"/> with
  /// <paramref name="rows"/>, preserving the stored columns. The full replacement
  /// is built first and swapped in one assignment, so no partially-written state
  /// is ever observable.
  /// </summary>
  public void ReplaceRows(string spreadsheetId, ResolvedTable table, TableData rows)
  {
    ArgumentNullException.ThrowIfNull(table);
    ArgumentNullException.ThrowIfNull(rows);
    var stored = RequireTable(spreadsheetId, table.Name);

    var replacement = rows.Rows.Select(r => r.ToList()).ToList();
    stored.Rows.Clear();
    stored.Rows.AddRange(replacement);
  }

  /// <summary>
  /// Create a table named <paramref name="tableName"/> from
  /// <paramref name="schema"/> in <paramref name="spreadsheetId"/> and return it
  /// resolved. The spreadsheet must already exist (Flowthru creates tables, not
  /// spreadsheets); a duplicate name is an error, not a silent overwrite.
  /// </summary>
  public ResolvedTable AddTable(string spreadsheetId, string tableName, TableSchema schema)
  {
    ArgumentNullException.ThrowIfNull(schema);
    // The spreadsheet must already exist (the live AddTable needs a sheet to
    // anchor on). An unregistered id is a missing spreadsheet, not an implicit
    // create.
    var spreadsheet = RequireSpreadsheet(spreadsheetId);
    if (spreadsheet.Tables.ContainsKey(tableName))
    {
      // Mirror the real API's unique-name rule: creating a duplicate is an error.
      throw new InvalidOperationException(
        $"Table '{tableName}' already exists in spreadsheet '{spreadsheetId}'.");
    }

    var table = new LocalSheetsTable
    {
      Columns = schema.Columns.Select(c => new LocalSheetsColumn(c.Name, c.Type)).ToList(),
      Rows = new List<List<FieldValue>>(),
    };
    spreadsheet.Tables[tableName] = table;

    return Resolve(spreadsheet, tableName, table);
  }

  // ── Internals ──────────────────────────────────────────────────────────────

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
    FieldKind.Temporal => FieldValue.Number(SheetsTranslator.ToSerial(field.TemporalValue)),
    _ => field,
  };

  private LocalSheetsTable RequireTable(string spreadsheetId, string tableName)
  {
    // Unregistered spreadsheet → hard failure, the same 404-shape ResolveTable
    // surfaces; a registered spreadsheet missing the table → InvalidOperation.
    var spreadsheet = RequireSpreadsheet(spreadsheetId);
    return spreadsheet.Tables.TryGetValue(tableName, out var table)
      ? table
      : throw new InvalidOperationException(
        $"Table '{tableName}' does not exist in spreadsheet '{spreadsheetId}'.");
  }

  // Look up a spreadsheet that must already be registered; an unknown id is a
  // missing spreadsheet (the offline analogue of Google's 404), distinct from a
  // table being absent within it.
  private LocalSheetsSpreadsheet RequireSpreadsheet(string spreadsheetId)
  {
    if (_data.Spreadsheets.TryGetValue(spreadsheetId, out var spreadsheet))
    {
      return spreadsheet;
    }

    throw new SheetsSpreadsheetAccessException(
      spreadsheetId,
      SheetsSpreadsheetAccessFailure.NotFound,
      $"Spreadsheet '{spreadsheetId}' does not exist. Register it with "
      + $"{nameof(RegisterSpreadsheet)} or {nameof(Seed)} before using it.");
  }

  private LocalSheetsSpreadsheet GetOrCreateSpreadsheet(string spreadsheetId)
  {
    if (_data.Spreadsheets.TryGetValue(spreadsheetId, out var existing))
    {
      return existing;
    }

    var spreadsheet = new LocalSheetsSpreadsheet { SheetId = _nextSheetId++ };
    _data.Spreadsheets[spreadsheetId] = spreadsheet;
    return spreadsheet;
  }

  private static ResolvedTable Resolve(
    LocalSheetsSpreadsheet spreadsheet, string tableName, LocalSheetsTable table)
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
}

// ── The JSON-serializable shape ───────────────────────────────────────────────
//
// Pure data — schema + rows of FieldValue — with no behavior, so a store can be
// loaded/dumped as JSON to fixture and inspect its contents. Mutation goes
// through LocalSheetsStore, which owns atomicity and the unique-name rule; these
// types are the on-the-wire shape only.

/// <summary>The JSON document of a <see cref="LocalSheetsStore"/>: spreadsheets keyed by id.</summary>
internal sealed class LocalSheetsData
{
  /// <summary>The spreadsheets in the store, keyed by spreadsheet id.</summary>
  public Dictionary<string, LocalSheetsSpreadsheet> Spreadsheets { get; init; } = new();
}

/// <summary>One spreadsheet: its tables keyed by table name.</summary>
internal sealed class LocalSheetsSpreadsheet
{
  /// <summary>
  /// The numeric id assigned to the first (and only) synthetic tab in this
  /// spreadsheet. Tables on it report this in their <see cref="TableRange.SheetId"/>.
  /// Deterministic so dumps are stable.
  /// </summary>
  public int SheetId { get; init; }

  /// <summary>The tables in this spreadsheet, keyed by table name.</summary>
  public Dictionary<string, LocalSheetsTable> Tables { get; init; } = new();
}

/// <summary>One table: its column schema and its data rows.</summary>
internal sealed class LocalSheetsTable
{
  /// <summary>The table's columns, in order.</summary>
  public List<LocalSheetsColumn> Columns { get; init; } = new();

  /// <summary>The data rows (header excluded); each row is fields left-to-right.</summary>
  public List<List<FieldValue>> Rows { get; init; } = new();

  /// <summary>Project the stored columns to a neutral <see cref="TableSchema"/>.</summary>
  public TableSchema ToSchema() =>
    new(Columns.Select(c => new TableColumn(c.Name, c.Type)).ToList());
}

/// <summary>A stored column: a name paired with its neutral type.</summary>
/// <param name="Name">The column's header name.</param>
/// <param name="Type">The column's neutral type.</param>
internal sealed record LocalSheetsColumn(string Name, ColumnType Type);
