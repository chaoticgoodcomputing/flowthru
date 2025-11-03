using RetailData.Data._03_Primary.Schemas;
using RetailData.Data._05_Reporting.Schemas;

namespace RetailData.Pipelines.Reporting.Nodes;

/// <summary>
/// Transforms country-level DTU data into unified time series format
/// </summary>
public static class TransformCountryDtuNode
{
  public static Func<IEnumerable<DailyDtuSchema>, Task<IEnumerable<DtuTimeSeriesSchema>>> Create()
  {
    return async (input) =>
    {
      var transformed = input.Select(d => new DtuTimeSeriesSchema
      {
        GroupingType = "Country",
        GroupingValue = d.Country,
        Date = d.Date,
        Dollars = d.Dollars,
        Transactions = d.Transactions,
        Users = d.Users,
      });

      return await Task.FromResult(transformed);
    };
  }
}
