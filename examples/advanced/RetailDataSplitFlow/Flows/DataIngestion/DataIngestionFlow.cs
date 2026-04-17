using Flowthru.Core.Flows;
using RetailDataMultipipeline.Data;
using RetailDataMultipipeline.Flows.DataIngestion.Steps;

namespace RetailDataMultipipeline.Flows.DataIngestion;

/// <summary>
/// Parses the raw online-retail CSV (fetched via HTTP) into a typed Parquet dataset.
/// </summary>
public static class DataIngestionFlow
{
  public static Flow Create(CoreCatalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "ValidateCsvTransactions",
        description: "Coerces all-string raw transaction records into fully-typed intermediate schema and writes a unified Parquet dataset.",
        transform: ValidateCsvStep.Create(),
        input: catalog.RetailTransactionsRaw,
        output: catalog.AllRetailTransactions
      );
    });
  }
}
