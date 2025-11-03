using RetailData.Data._03_Primary.Schemas;
using RetailData.Data._99_Configuration.Schemas;

namespace RetailData.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Aggregates country-level DTU data by region using country-region mapping
/// </summary>
public static class AggregateByRegionNode
{
  public static Func<
    (IEnumerable<DailyDtuSchema>, CountryRegionMapping),
    Task<IEnumerable<DailyDtuByRegionSchema>>
  > Create()
  {
    return async (input) =>
    {
      var (dtuData, mapping) = input;

      // Create country -> region lookup from the region-centric mapping
      var countryToRegion = new Dictionary<string, string>();
      foreach (var (region, countries) in mapping.Regions)
      {
        foreach (var country in countries)
        {
          countryToRegion[country] = region;
        }
      }

      // Aggregate by date and region
      var regionalData = dtuData
        .Where(d => countryToRegion.ContainsKey(d.Country))
        .GroupBy(d => new { d.Date, Region = countryToRegion[d.Country] })
        .Select(g => new DailyDtuByRegionSchema
        {
          Date = g.Key.Date,
          Region = g.Key.Region,
          Dollars = g.Sum(d => d.Dollars),
          Transactions = g.Sum(d => d.Transactions),
          Users = g.Sum(d => d.Users),
        })
        .OrderBy(d => d.Date)
        .ThenBy(d => d.Region);

      return await Task.FromResult(regionalData);
    };
  }
}
