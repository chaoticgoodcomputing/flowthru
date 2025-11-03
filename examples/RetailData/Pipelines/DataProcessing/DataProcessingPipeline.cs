using Flowthru.Pipelines;
using RetailData.Data;
using RetailData.Pipelines.DataProcessing.Nodes;

namespace RetailData.Pipelines.DataProcessing;

/// <summary>
/// Main data processing pipeline: ingests raw data, cleans it, separates descriptions, and aggregates DTU metrics
/// </summary>
public static class DataProcessingPipeline
{
  public static Pipeline Create(Catalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      // Step 1: Clean raw data (parse types, trim strings, truncate dates)
      pipeline.AddNode(
        name: "CleanData",
        transform: CleanDataNode.Create(),
        input: catalog.RawRetailData,
        output: catalog.CleanedRetailData
      );

      // Step 2: Separate core transactions from stock descriptions
      pipeline.AddNode(
        name: "SeparateDescriptions",
        transform: SeparateDescriptionsNode.Create(),
        input: catalog.CleanedRetailData,
        output: (catalog.CoreTransactions, catalog.StockDescriptions)
      );

      // Step 3: Aggregate by date and country to calculate DTU metrics
      pipeline.AddNode(
        name: "AggregateDtu",
        transform: AggregateDtuNode.Create(),
        input: catalog.CoreTransactions,
        output: catalog.DailyDtuByCountry
      );

      // Step 4: Aggregate country data by region
      pipeline.AddNode(
        name: "AggregateByRegion",
        transform: AggregateByRegionNode.Create(),
        input: (catalog.DailyDtuByCountry, catalog.CountryRegionMapping),
        output: catalog.DailyDtuByRegion
      );

      // Step 5: Generate metadata about the dataset
      pipeline.AddNode(
        name: "GenerateMetadata",
        transform: GenerateMetadataNode.Create(),
        input: catalog.DailyDtuByCountry,
        output: catalog.DatasetMetadata
      );
    });
  }
}
