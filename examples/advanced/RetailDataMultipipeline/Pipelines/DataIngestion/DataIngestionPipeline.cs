using Flowthru.Flows;
using RetailDataMultipipeline.Data;
using RetailDataMultipipeline.Pipelines.DataIngestion.Nodes;

namespace RetailDataMultipipeline.Pipelines.DataIngestion;

/// <summary>
/// Ingests the 305 daily retail transaction CSV files and consolidates them
/// into a single Parquet dataset.
/// </summary>
public static class DataIngestionPipeline
{
  public static Flow Create(CoreCatalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "ConsolidateDailyFiles",
        description: "Reads all daily CSV files from the raw directory and writes a unified Parquet dataset.",
        transform: ConsolidateDailyFilesNode.Create(),
        input: catalog.RetailTransactionsRaw,
        output: catalog.AllRetailTransactions
      );
    });
  }
}
