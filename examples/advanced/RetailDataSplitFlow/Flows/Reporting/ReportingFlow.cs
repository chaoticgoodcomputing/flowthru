using Flowthru.Flow;
using RetailDataMultipipeline.Data;
using RetailDataMultipipeline.Data._02_Intermediate.Schemas;
using RetailDataMultipipeline.Data._08_Reporting.Schemas;
using RetailDataMultipipeline.Flows.Reporting.Steps;

namespace RetailDataMultipipeline.Flows.Reporting;

/// <summary>
/// Produces reporting outputs from the consolidated retail transaction dataset.
/// </summary>
public static class ReportingFlow
{
  public static BuiltFlow Create(CoreCatalog catalog)
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      pipeline.AddStep<IEnumerable<RetailTransactionIntermediateSchema>, IEnumerable<CountryTransactionSummarySchema>>(
        label: "SummarizeByCountry",
        transform: SummarizeByCountryStep.Create(),
        input1: catalog.AllRetailTransactions,
        output1: catalog.CountryTransactionSummary
      );
    });
  }
}
