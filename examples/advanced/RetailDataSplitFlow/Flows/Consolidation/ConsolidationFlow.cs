using Flowthru.Flow;
using RetailDataMultipipeline.Data;
using RetailDataMultipipeline.Data._03_Primary.Schemas;

namespace RetailDataMultipipeline.Flows.Consolidation;

/// <summary>
/// Concatenates all per-country weekly DTU shards into a single unified
/// dataset using the variadic-input AddStep overload — pure reduce/fan-in.
/// The DAG edge from each shard to this node is resolved automatically
/// because each <see cref="CountryShardCatalog"/> instance is the same
/// object captured by both the Analysis flow (writer) and this flow
/// (reader).
/// </summary>
public static class ConsolidationFlow
{
  public static BuiltFlow Create(CoreCatalog core, IReadOnlyList<CountryShardCatalog> shards)
  {
    var inputs = shards.Select(s => s.WeeklyDtu).ToList();

    return FlowBuilder.CreateFlow("Consolidation", pipeline =>
    {
      pipeline.AddStep<IEnumerable<WeeklyDtuSchema>, IEnumerable<WeeklyDtuSchema>>(
        label: "ConsolidateShards",
        transform: batches => batches.SelectMany(b => b),
        inputs: inputs,
        outputs: core.AllCountriesWeeklyDtu
      );
    });
  }
}
