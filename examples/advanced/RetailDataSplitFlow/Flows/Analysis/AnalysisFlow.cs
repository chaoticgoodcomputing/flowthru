using Flowthru.Core.Flows;
using RetailDataMultipipeline.Data;
using RetailDataMultipipeline.Flows.Analysis.Steps;

namespace RetailDataMultipipeline.Flows.Analysis;

/// <summary>
/// Computes weekly DTU metrics for a single country shard.
/// Instantiated once per <see cref="CountryShardCatalog"/> via <c>RegisterFlows</c> in Program.cs.
/// </summary>
public static class AnalysisFlow
{
    public static Flow Create(CoreCatalog core, CountryShardCatalog shard)
    {
        return FlowBuilder.CreateFlow(pipeline =>
        {
            pipeline.AddStep(
          label: "ComputeWeeklyDtu",
          description: $"Converts currency and aggregates weekly DTU metrics for {shard.Country}.",
          transform: ComputeWeeklyDtuStep.Create(shard.Country),
          input: (core.AllRetailTransactions, core.CountryCurrencies, core.OfxRates),
          output: shard.WeeklyDtu
        );
        });
    }
}
