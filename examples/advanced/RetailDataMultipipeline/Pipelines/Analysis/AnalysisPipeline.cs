using Flowthru.Flows;
using RetailDataMultipipeline.Data;
using RetailDataMultipipeline.Pipelines.Analysis.Nodes;

namespace RetailDataMultipipeline.Pipelines.Analysis;

/// <summary>
/// Computes weekly DTU metrics for a single country shard.
/// Instantiated once per <see cref="CountryShardCatalog"/> via <c>RegisterPipelines</c> in Program.cs.
/// </summary>
public static class AnalysisPipeline
{
  public static Flow Create(CoreCatalog core, CountryShardCatalog shard)
  {
    return FlowBuilder.CreateFlow(pipeline =>
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
