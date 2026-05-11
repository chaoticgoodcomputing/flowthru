using Flowthru.Flow;
using RetailDataMultipipeline.Data;
using RetailDataMultipipeline.Data._01_Raw.Schemas;
using RetailDataMultipipeline.Data._02_Intermediate.Schemas;
using RetailDataMultipipeline.Flows.DataIngestion.Steps;

namespace RetailDataMultipipeline.Flows.DataIngestion;

/// <summary>
/// Parses the raw online-retail CSV (fetched via HTTP) into a typed Parquet dataset.
/// </summary>
public static class DataIngestionFlow
{
  public static BuiltFlow Create(CoreCatalog catalog)
  {
    return FlowBuilder.CreateFlow("DataIngestion", pipeline =>
    {
      pipeline.AddStep<IEnumerable<RetailTransactionSchema>, IEnumerable<RetailTransactionIntermediateSchema>>(
        label: "ValidateCsvTransactions",
        transform: ValidateCsvStep.Create(),
        inputs: catalog.RetailTransactionsRaw,
        outputs: catalog.AllRetailTransactions
      );
    });
  }
}
