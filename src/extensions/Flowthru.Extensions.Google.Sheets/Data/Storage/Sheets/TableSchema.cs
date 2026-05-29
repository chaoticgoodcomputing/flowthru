namespace Flowthru.Data.Storage.Sheets;

/// <summary>
/// One column of a <see cref="TableSchema"/> — a name paired with its
/// Flowthru-neutral <see cref="ColumnType"/>. The store's analog of a database
/// column: a single name, a single type.
/// </summary>
/// <param name="Name">The column's header name, as it appears in the table.</param>
/// <param name="Type">The column's Flowthru-neutral type.</param>
public sealed record TableColumn(string Name, ColumnType Type);

/// <summary>
/// The Flowthru-neutral schema of a table — its columns, in order. This is the
/// store's schema vocabulary on the <see cref="ISheetsGateway"/> seam: column
/// position carries identity (it maps to the native Sheets
/// <c>TableColumnProperties</c> column index), so order is significant.
/// </summary>
/// <remarks>
/// A schema is read back from a resolved table (its native column properties),
/// and supplied to <see cref="ISheetsGateway.AddTable"/> to create a table when
/// absent. It never carries row data — that is <see cref="TableData"/>.
/// </remarks>
public sealed class TableSchema
{
  /// <summary>Build a schema from an ordered list of columns.</summary>
  public TableSchema(IReadOnlyList<TableColumn> columns)
  {
    Columns = columns ?? throw new ArgumentNullException(nameof(columns));
  }

  /// <summary>The table's columns, left-to-right. Position is significant.</summary>
  public IReadOnlyList<TableColumn> Columns { get; }

  /// <summary>The number of columns in the schema.</summary>
  public int ColumnCount => Columns.Count;
}
