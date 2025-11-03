using RetailData.Data._03_Primary.Schemas;
using RetailData.Data._05_Reporting.Schemas;

namespace RetailData.Pipelines.Reporting.Nodes;

/// <summary>
/// Transforms region-level DTU data into unified time series format
/// </summary>
public static class TransformRegionDtuNode
{
  public static Func<
    IEnumerable<DailyDtuByRegionSchema>,
    Task<IEnumerable<DtuTimeSeriesSchema>>
  > Create()
  {
    return async (input) =>
    {
      var transformed = input.Select(d => new DtuTimeSeriesSchema
      {
        GroupingType = "Region",
        GroupingValue = d.Region,
        Date = d.Date,
        Dollars = d.Dollars,
        Transactions = d.Transactions,
        Users = d.Users,
      });

      return await Task.FromResult(transformed);
    };
  }
}
