namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// A table's data rows under a known <see cref="TableSchema"/> — the unit of
/// read and write across the <see cref="ISheetsGateway"/> seam. Pure data:
/// header/column semantics live in the schema, schema interpretation and
/// serial-date coercion live in the adapter, never here.
/// </summary>
/// <remarks>
/// Rows are ordered top-to-bottom; within a row, fields are aligned
/// left-to-right to <see cref="Schema"/>'s columns. Rows need not all carry a
/// field per column — the gateway pads short rows to the schema width when
/// writing so the rows map onto the table's data range.
/// </remarks>
public sealed class TableData
{
  /// <summary>Pair a schema with its data rows.</summary>
  public TableData(TableSchema schema, IReadOnlyList<IReadOnlyList<FieldValue>> rows)
  {
    Schema = schema ?? throw new ArgumentNullException(nameof(schema));
    Rows = rows ?? throw new ArgumentNullException(nameof(rows));
  }

  /// <summary>An empty body (no rows) under <paramref name="schema"/>.</summary>
  public static TableData Empty(TableSchema schema) =>
    new(schema, Array.Empty<IReadOnlyList<FieldValue>>());

  /// <summary>The schema the rows are aligned to.</summary>
  public TableSchema Schema { get; }

  /// <summary>The data rows, top-to-bottom; each row is fields left-to-right.</summary>
  public IReadOnlyList<IReadOnlyList<FieldValue>> Rows { get; }

  /// <summary>The number of data rows.</summary>
  public int RowCount => Rows.Count;
}
