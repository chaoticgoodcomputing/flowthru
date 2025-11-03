using RetailData.Data._03_Primary.Schemas;
using RetailData.Data._05_Reporting.Schemas;

namespace RetailData.Pipelines.Reporting.Nodes;

/// <summary>
/// Transforms country correlation data into unified heatmap format
/// </summary>
public static class TransformCountryCorrelationNode
{
  public static Func<
    IEnumerable<CountryCorrelationSchema>,
    Task<IEnumerable<CorrelationHeatmapSchema>>
  > Create()
  {
    return async (input) =>
    {
      var transformed = input.Select(c => new CorrelationHeatmapSchema
      {
        GroupingType = "Country",
        Group1 = c.Country1,
        Group2 = c.Country2,
        DollarsCorrelation = c.DollarsCorrelation,
        TransactionsCorrelation = c.TransactionsCorrelation,
        UsersCorrelation = c.UsersCorrelation,
      });

      return await Task.FromResult(transformed);
    };
  }
}
