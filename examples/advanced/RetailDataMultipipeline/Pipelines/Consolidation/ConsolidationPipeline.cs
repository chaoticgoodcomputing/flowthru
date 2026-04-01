using Flowthru.Pipelines;
using RetailDataMultipipeline.Data;

namespace RetailDataMultipipeline.Pipelines.Consolidation;

/// <summary>
/// Concatenates all per-country daily DTU Parquet shards into a single unified dataset.
/// </summary>
/// <remarks>
/// This is a pure fan-in — no aggregation or transformation, just <c>SelectMany</c>.
/// The input list is built from the same <see cref="CountryShardCatalog"/> instances
/// used by the Analysis pipelines, so the DAG edge from each shard to this node
/// is resolved automatically.
/// </remarks>
public static class ConsolidationPipeline
{
  public static Pipeline Create(CoreCatalog core, List<CountryShardCatalog> shards)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "ConsolidateShards",
        description: "Concatenates all per-country weekly DTU shards into a single Parquet dataset.",
        inputs: shards.Select(s => s.WeeklyDtu).ToList(),
        output: core.AllCountriesWeeklyDtu,
        node: batches => batches.SelectMany(b => b)
      );
    });
  }
}
