using Flowthru.Flow;
using GoogleSheets.Data;
using GoogleSheets.Data._01_Raw.Schemas;
using GoogleSheets.Data._03_Primary.Schemas;
using GoogleSheets.Flows.Sales.Steps;

namespace GoogleSheets.Flows.Sales;

/// <summary>
/// Reads raw sales from one Sheets table, totals them by day, and writes the
/// result back to another Sheets table — the round trip a downstream user runs
/// against a spreadsheet.
/// </summary>
public static class SalesFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("Sales", pipeline =>
    {
      pipeline.AddStep<IEnumerable<RawSaleSchema>, IEnumerable<DailyTotalSchema>>(
        label: "SummarizeSales",
        transform: SummarizeSalesStep.Create(),
        inputs: catalog.RawSales,
        outputs: catalog.DailyTotals
      );
    });
  }
}
