using RetailData.Data._02_Intermediate.Schemas;
using RetailData.Data._03_Primary.Schemas;
using RetailData.Data._99_Configuration.Schemas;

namespace RetailData.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Aggregates transactions by date and region to calculate DTU metrics.
/// Works directly from raw transactions to ensure correct distinct user counts
/// (users who purchase in multiple countries are not double-counted).
/// </summary>
public static class AggregateByRegionNode
{
  public static Func<
    (IEnumerable<CoreTransactionSchema>, CountryRegionMapping),
    Task<IEnumerable<DailyDtuByRegionSchema>>
  > Create()
  {
    return async (input) =>
    {
      var (transactions, mapping) = input;

      // Create country -> region lookup from the region-centric mapping
      var countryToRegion = new Dictionary<string, string>();
      foreach (var (region, countries) in mapping.Regions)
      {
        foreach (var country in countries)
        {
          countryToRegion[country] = region;
        }
      }

      // Aggregate by date and region with distinct counts
      var regionalData = transactions
        .Where(t => countryToRegion.ContainsKey(t.Country))
        .GroupBy(t => new { t.InvoiceDate, Region = countryToRegion[t.Country] })
        .Select(g => new DailyDtuByRegionSchema
        {
          Date = g.Key.InvoiceDate,
          Region = g.Key.Region,
          Dollars = g.Sum(t => t.TotalAmount),
          Transactions = g.Select(t => t.InvoiceNo).Distinct().Count(),
          Users = g.Select(t => t.CustomerID).Distinct().Count(),
        })
        .OrderBy(d => d.Date)
        .ThenBy(d => d.Region);

      return await Task.FromResult(regionalData);
    };
  }
}
