using Flowthru.Core.Flows;
using RetailDataMultipipeline.Data;

namespace RetailDataMultipipeline.Flows.Consolidation;

/// <summary>
/// Concatenates all per-country daily DTU Parquet shards into a single unified dataset.
/// </summary>
/// <remarks>
/// This is a pure fan-in — no aggregation or transformation, just <c>SelectMany</c>.
/// The input list is built from the same <see cref="CountryShardCatalog"/> instances
/// used by the Analysis pipelines, so the DAG edge from each shard to this node
/// is resolved automatically.
/// </remarks>
public static class ConsolidationFlow
{
  public static Flow Create(CoreCatalog core, List<CountryShardCatalog> shards)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "ConsolidateShards",
        description: "Concatenates all per-country weekly DTU shards into a single Parquet dataset.",
        inputs: shards.Select(s => s.WeeklyDtu).ToList(),
        output: core.AllCountriesWeeklyDtu,
        step: batches => batches.SelectMany(b => b)
      );
    });
  }
}
