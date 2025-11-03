using RetailData.Data._03_Primary.Schemas;
using RetailData.Data._05_Reporting.Schemas;

namespace RetailData.Pipelines.Reporting.Nodes;

/// <summary>
/// Transforms region correlation data into unified heatmap format
/// </summary>
public static class TransformRegionCorrelationNode
{
  public static Func<
    IEnumerable<RegionCorrelationSchema>,
    Task<IEnumerable<CorrelationHeatmapSchema>>
  > Create()
  {
    return async (input) =>
    {
      var transformed = input.Select(c => new CorrelationHeatmapSchema
      {
        GroupingType = "Region",
        Group1 = c.Region1,
        Group2 = c.Region2,
        DollarsCorrelation = c.DollarsCorrelation,
        TransactionsCorrelation = c.TransactionsCorrelation,
        UsersCorrelation = c.UsersCorrelation,
      });

      return await Task.FromResult(transformed);
    };
  }
}
