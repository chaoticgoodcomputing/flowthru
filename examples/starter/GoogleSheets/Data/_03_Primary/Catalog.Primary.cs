using Flowthru.Data.Catalog;
using GoogleSheets.Data._03_Primary.Schemas;

namespace GoogleSheets.Data;

public partial class Catalog
{
  /// <summary>
  /// The daily-totals table — the Sheets output, and the "Raw Data" surface
  /// Flowthru owns. The table is created from <see cref="DailyTotalSchema"/> on
  /// first write if it is absent, then atomically replaced every run, scoped to
  /// this one tab so sibling formula tabs that reference it are never clobbered.
  /// Replace is the upsert: when Flowthru owns the table it holds the full
  /// dataset each run.
  /// </summary>
  public IItem<IEnumerable<DailyTotalSchema>> DailyTotals =>
    CreateItem(() => ItemFactory.Enumerable.GoogleSheets<DailyTotalSchema>(
      label: "DailyTotals",
      spreadsheetId: _spreadsheetId,
      tableName: "DailyTotals",
      gateway: _sheets));
}
