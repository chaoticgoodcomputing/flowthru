using Flowthru.Pipelines;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Flowthru.Spaceflights.Pipelines.DataScience.Nodes;
using Flowthru.Spaceflights.Pipelines.DataScience.Parameters;

namespace Flowthru.Spaceflights.Pipelines.DataScience;

/// <summary>
/// Data science pipeline that splits data, trains model, and evaluates performance.
/// Follows Kedro's pattern: nodes are pure functions, pipeline declares the data flow.
/// 
/// Demonstrates multi-output pattern using OutputMapping&lt;T&gt; for type-safe
/// property-to-catalog mappings.
/// </summary>
public static class DataSciencePipeline
{
  public static Pipeline Create(DataCatalog catalog, ModelOptions? options = null)
  {
    return PipelineBuilder.CreatePipeline(catalog, pipeline =>
    {
      // Node 1: Split data into train/test sets (multi-output with expression-based mapping)
      // OutputMapping<T> uses property selectors for type-safe, refactor-safe mappings
      // Each property of SplitDataOutputs becomes a separate catalog entry
      pipeline.AddNode<SplitDataNode>(
        inputs: "model_input_table",
        outputs: new OutputMapping<SplitDataOutputs>
        {
          { s => s.XTrain, "x_train" },
          { s => s.XTest, "x_test" },
          { s => s.YTrain, "y_train" },
          { s => s.YTest, "y_test" }
        },
        name: "split_data_node",
        configureNode: node => node.Parameters = options ?? new ModelOptions()
      );

      // Node 2: Train regression model (consumes x_train and y_train individually)
      pipeline.AddNode<TrainModelNode>(
        inputs: new[] { "x_train", "y_train" },
        outputs: "regressor",
        name: "train_model_node"
      );

      // Node 3: Evaluate model (consumes model, x_test, and y_test individually)
      // Logger can be injected via configureNode if needed
      pipeline.AddNode<EvaluateModelNode>(
        inputs: new[] { "regressor", "x_test", "y_test" },
        outputs: "model_metrics",
        name: "evaluate_model_node"
      // Optional: configureNode: node => node.Logger = logger
      );
    });
  }
}
