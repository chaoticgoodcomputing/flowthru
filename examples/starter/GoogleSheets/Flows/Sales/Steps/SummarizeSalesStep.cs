using Flowthru.Step;
using GoogleSheets.Data._01_Raw.Schemas;
using GoogleSheets.Data._03_Primary.Schemas;

namespace GoogleSheets.Flows.Sales.Steps;

/// <summary>
/// Totals the raw sales by day — the trivial transform between the Sheets input
/// table and the Sheets output table.
/// </summary>
[FlowthruStep]
public static class SummarizeSalesStep
{
  /// <summary>
  /// Creates a transformation that groups raw sales by their recorded day and
  /// sums each day's amount.
  /// </summary>
  public static Func<IEnumerable<RawSaleSchema>, IEnumerable<DailyTotalSchema>> Create()
  {
    return sales =>
      sales
        .GroupBy(sale => sale.SoldOn)
        .OrderBy(group => group.Key)
        .Select(group => new DailyTotalSchema
        {
          Day = group.Key,
          Total = group.Sum(sale => sale.Amount),
        });
  }
}
