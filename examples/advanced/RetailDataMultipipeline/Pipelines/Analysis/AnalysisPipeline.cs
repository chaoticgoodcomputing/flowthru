using Flowthru.Pipelines;
using RetailDataMultipipeline.Data;
using RetailDataMultipipeline.Pipelines.Analysis.Nodes;

namespace RetailDataMultipipeline.Pipelines.Analysis;

/// <summary>
/// Computes weekly DTU metrics for a single country shard.
/// Instantiated once per <see cref="CountryShardCatalog"/> via <c>UsePipelines</c> in Program.cs.
/// </summary>
public static class AnalysisPipeline
{
  public static Pipeline Create(CoreCatalog core, CountryShardCatalog shard)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "ComputeWeeklyDtu",
        description: $"Converts currency and aggregates weekly DTU metrics for {shard.Country}.",
        transform: ComputeWeeklyDtuNode.Create(shard.Country),
        input: (core.AllRetailTransactions, core.CountryCurrencies, core.OfxRates),
        output: shard.WeeklyDtu
      );
    });
  }
}
