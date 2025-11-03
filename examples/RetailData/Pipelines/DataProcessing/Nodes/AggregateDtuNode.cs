using RetailData.Data._02_Intermediate.Schemas;
using RetailData.Data._03_Primary.Schemas;

namespace RetailData.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Aggregates transactions by date and country to calculate DTU metrics
/// </summary>
public static class AggregateDtuNode
{
  public static Func<IEnumerable<CoreTransactionSchema>, Task<IEnumerable<DailyDtuSchema>>> Create()
  {
    return async (input) =>
    {
      var aggregated = input
        .GroupBy(t => new { t.InvoiceDate, t.Country })
        .Select(g => new DailyDtuSchema
        {
          Date = g.Key.InvoiceDate,
          Country = g.Key.Country,
          Dollars = g.Sum(t => t.TotalAmount),
          Transactions = g.Select(t => t.InvoiceNo).Distinct().Count(),
          Users = g.Select(t => t.CustomerID).Distinct().Count(),
        })
        .OrderBy(d => d.Date)
        .ThenBy(d => d.Country);

      return await Task.FromResult(aggregated);
    };
  }
}
