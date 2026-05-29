using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flowthru.Data.Storage.Sheets.InMemory;

/// <summary>
/// The JSON-serializable backing store of an <see cref="InMemorySheetsGateway"/>:
/// <c>spreadsheetId → (tableName → table)</c>. Pure data — schema + rows of
/// <see cref="FieldValue"/> — with no behavior, so the example and tests can
/// seed it programmatically and load/dump it as JSON to fixture and inspect a
/// gateway's contents.
/// </summary>
/// <remarks>
/// Mutation goes through <see cref="InMemorySheetsGateway"/>, which owns
/// atomicity and the unique-name rule. This type is the on-the-wire shape only.
/// </remarks>
public sealed class InMemorySheetsStore
{
  /// <summary>The spreadsheets in the store, keyed by spreadsheet id.</summary>
  public Dictionary<string, InMemorySpreadsheet> Spreadsheets { get; init; } = new();

  /// <summary>Serialize the store to JSON (indented, deterministic key order).</summary>
  public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

  /// <summary>Rehydrate a store from JSON produced by <see cref="ToJson"/>.</summary>
  public static InMemorySheetsStore FromJson(string json) =>
    JsonSerializer.Deserialize<InMemorySheetsStore>(json, JsonOptions)
    ?? throw new ArgumentException("JSON deserialized to a null store.", nameof(json));

  internal static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
    // Deterministic enum rendering (Kind/Type as names, not ints).
    Converters = { new JsonStringEnumConverter() },
  };
}

/// <summary>One spreadsheet: its tables keyed by table name.</summary>
public sealed class InMemorySpreadsheet
{
  /// <summary>
  /// The numeric id assigned to the first (and only) synthetic tab in this
  /// spreadsheet. Tables on it report this in their <see cref="TableRange.SheetId"/>.
  /// Deterministic so dumps are stable.
  /// </summary>
  public int SheetId { get; init; }

  /// <summary>The tables in this spreadsheet, keyed by table name.</summary>
  public Dictionary<string, InMemoryTable> Tables { get; init; } = new();
}

/// <summary>One table: its column schema and its data rows.</summary>
public sealed class InMemoryTable
{
  /// <summary>The table's columns, in order.</summary>
  public List<InMemoryColumn> Columns { get; init; } = new();

  /// <summary>The data rows (header excluded); each row is fields left-to-right.</summary>
  public List<List<FieldValue>> Rows { get; init; } = new();

  /// <summary>Project the stored columns to a neutral <see cref="TableSchema"/>.</summary>
  public TableSchema ToSchema() =>
    new(Columns.Select(c => new TableColumn(c.Name, c.Type)).ToList());
}

/// <summary>A stored column: a name paired with its neutral type.</summary>
/// <param name="Name">The column's header name.</param>
/// <param name="Type">The column's neutral type.</param>
public sealed record InMemoryColumn(string Name, ColumnType Type);
