using Flowthru.Core.Flows;
using RetailDataMultipipeline.Data;
using RetailDataMultipipeline.Flows.Reporting.Steps;

namespace RetailDataMultipipeline.Flows.Reporting;

/// <summary>
/// Produces reporting outputs from the consolidated retail transaction dataset.
/// </summary>
public static class ReportingFlow
{
    public static Flow Create(CoreCatalog catalog)
    {
        return FlowBuilder.CreateFlow(pipeline =>
        {
            pipeline.AddStep(
          label: "SummarizeByCountry",
          description: "Counts debit and credit line items per country across the full transaction history.",
          transform: SummarizeByCountryStep.Create(),
          input: catalog.AllRetailTransactions,
          output: catalog.CountryTransactionSummary
        );
        });
    }
}
