using Flowthru.Pipelines;
using Flowthru.Spaceflights.Pipelines.DataScience.Nodes;
using Flowthru.Spaceflights.Pipelines.DataScience.Parameters;

namespace Flowthru.Spaceflights.Pipelines.DataScience;

/// <summary>
/// Data science pipeline that splits data, trains model, and evaluates performance.
/// Follows Kedro's pattern: nodes are pure functions, pipeline declares the data flow.
/// </summary>
public static class DataSciencePipeline
{
  public static Pipeline Create(DataCatalog catalog, ModelOptions? options = null)
  {
    return PipelineBuilder.CreatePipeline(catalog, pipeline =>
    {
      // Node 1: Split data into train/test sets (parameterized)
      var splitNode = new SplitDataNode
      {
        Parameters = options ?? new ModelOptions()
      };

      pipeline.AddNode(
        node: splitNode,
        inputs: "model_input_table",
        outputs: "train_test_split",
        name: "split_data_node"
      );

      // Node 2: Train regression model
      pipeline.AddNode(
        node: new TrainModelNode(),
        inputs: "train_test_split",
        outputs: "regressor",
        name: "train_model_node"
      );

      // Node 3: Evaluate model (2 inputs: model + test data)
      pipeline.AddNode(
        node: new EvaluateModelNode(),
        inputs: new[] { "regressor", "train_test_split" },
        outputs: "model_metrics",
        name: "evaluate_model_node"
      );
    });
  }
}
