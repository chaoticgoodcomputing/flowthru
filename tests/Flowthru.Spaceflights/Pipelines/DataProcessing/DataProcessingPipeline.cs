using Flowthru.Pipelines;
using Flowthru.Spaceflights.Pipelines.DataProcessing.Nodes;

namespace Flowthru.Spaceflights.Pipelines.DataProcessing;

/// <summary>
/// Data processing pipeline that preprocesses raw data and creates model input table.
/// Follows Kedro's pattern: nodes are pure functions, pipeline declares the data flow.
/// </summary>
public static class DataProcessingPipeline
{
  public static Pipeline Create(DataCatalog catalog)
  {
    return PipelineBuilder.CreatePipeline(catalog, pipeline =>
    {
      // Node 1: Preprocess companies
      pipeline.AddNode(
        node: new PreprocessCompaniesNode(),
        inputs: "companies",
        outputs: "preprocessed_companies",
        name: "preprocess_companies_node"
      );

      // Node 2: Preprocess shuttles
      pipeline.AddNode(
        node: new PreprocessShuttlesNode(),
        inputs: "shuttles",
        outputs: "preprocessed_shuttles",
        name: "preprocess_shuttles_node"
      );

      // Node 3: Create model input table (3 inputs)
      pipeline.AddNode(
        node: new CreateModelInputTableNode(),
        inputs: new[] { "preprocessed_shuttles", "preprocessed_companies", "reviews" },
        outputs: "model_input_table",
        name: "create_model_input_table_node"
      );
    });
  }
}
