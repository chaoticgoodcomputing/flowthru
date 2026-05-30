using Flowthru.Data.Catalog;
using GoogleSheets.Data._01_Raw.Schemas;

namespace GoogleSheets.Data;

public partial class Catalog
{
  /// <summary>
  /// The raw sales table — the Sheets input. Addressed by
  /// <c>(spreadsheetId, table name)</c>: a stable native-table name, not a
  /// fragile cell range. The factory matches the table's columns to
  /// <see cref="RawSaleSchema"/>'s properties by name and coerces each cell to the
  /// property's declared type on read.
  /// </summary>
  public IItem<IEnumerable<RawSaleSchema>> RawSales =>
    CreateItem(() => ItemFactory.Enumerable.GoogleSheets<RawSaleSchema>(
      label: "RawSales",
      spreadsheetId: _spreadsheetId,
      tableName: "RawSales",
      gateway: _sheets));
}
