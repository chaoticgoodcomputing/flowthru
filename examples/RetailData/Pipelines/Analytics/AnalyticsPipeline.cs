using Flowthru.Pipelines;
using RetailData.Data;
using RetailData.Pipelines.Analytics.Nodes;

namespace RetailData.Pipelines.Analytics;

/// <summary>
/// Analytics pipeline: calculates country-to-country and region-to-region correlations
/// </summary>
public static class AnalyticsPipeline
{
  public static Pipeline Create(Catalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      // Calculate correlations between countries' DTU metrics
      pipeline.AddNode(
        label: "CalculateCountryCorrelations",
        transform: CalculateCorrelationsNode.Create(),
        input: catalog.DailyDtuByCountry,
        output: catalog.CountryCorrelations
      );

      // Calculate correlations between regions' DTU metrics
      pipeline.AddNode(
        label: "CalculateRegionalCorrelations",
        transform: CalculateRegionalCorrelationsNode.Create(),
        input: catalog.DailyDtuByRegion,
        output: catalog.RegionCorrelations
      );
    });
  }
}
