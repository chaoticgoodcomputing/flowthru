using Flowthru.Flow;
using RetailDataMultipipeline.Data;
using RetailDataMultipipeline.Data._01_Raw.Schemas;
using RetailDataMultipipeline.Data._02_Intermediate.Schemas;
using RetailDataMultipipeline.Data._03_Primary.Schemas;
using RetailDataMultipipeline.Flows.Analysis.Steps;

namespace RetailDataMultipipeline.Flows.Analysis;

/// <summary>
/// Computes weekly DTU metrics for every country shard. One flow, N steps —
/// each <see cref="CountryShardCatalog"/> contributes one step that
/// closure-captures its own country and writes to its own output item.
/// The merged-DAG executor schedules siblings in parallel.
/// </summary>
public static class AnalysisFlow
{
  public static BuiltFlow Create(CoreCatalog core, IReadOnlyList<CountryShardCatalog> shards)
  {
    return FlowBuilder.CreateFlow("Analysis", pipeline =>
    {
      foreach (var shard in shards)
      {
        var captured = shard;
        pipeline.AddStep<
          IEnumerable<RetailTransactionIntermediateSchema>,
          IEnumerable<CountryCurrencySchema>,
          IEnumerable<OfxRateResponseSchema>,
          IEnumerable<WeeklyDtuSchema>
        >(
          label: $"Analyze_{Slugify(captured.Country)}",
          transform: ComputeWeeklyDtuStep.Create(captured.Country),
          input1: core.AllRetailTransactions,
          input2: core.CountryCurrencies,
          input3: core.OfxRates,
          output1: captured.WeeklyDtu
        );
      }
    });
  }

  private static string Slugify(string country) =>
    country.ToLowerInvariant().Replace(' ', '_').Replace('.', '_');
}
