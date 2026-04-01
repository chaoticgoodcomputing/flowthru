using Flowthru.Pipelines;
using RetailDataMultipipeline.Data;
using RetailDataMultipipeline.Pipelines.Reporting.Nodes;

namespace RetailDataMultipipeline.Pipelines.Reporting;

/// <summary>
/// Produces reporting outputs from the consolidated retail transaction dataset.
/// </summary>
public static class ReportingPipeline
{
  public static Pipeline Create(CoreCatalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "SummarizeByCountry",
        description: "Counts debit and credit line items per country across the full transaction history.",
        transform: SummarizeByCountryNode.Create(),
        input: catalog.AllRetailTransactions,
        output: catalog.CountryTransactionSummary
      );
    });
  }
}
