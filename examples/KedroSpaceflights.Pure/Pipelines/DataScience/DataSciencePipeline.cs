using Flowthru.Pipelines;
using KedroSpaceflights.Pure.Data;

namespace KedroSpaceflights.Pure.Pipelines.DataScience;

/// <summary>
/// Creates the data science pipeline that trains and evaluates a price prediction model.
/// </summary>
public static class DataSciencePipeline
{
  /// <summary>
  /// Configuration parameters for the data science pipeline.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Configuration options for data splitting and model training.
    /// </summary>
    public Nodes.SplitDataNode.ModelOptions ModelOptions { get; init; } = new();
  }

  /// <summary>
  /// Creates the data science pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <param name="parameters">Configuration parameters for the pipeline.</param>
  /// <returns>A configured pipeline that produces a trained model and evaluation metrics.</returns>
  public static Pipeline Create(Catalog catalog, Params parameters)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        name: "SplitData",
        transform: Nodes.SplitDataNode.Create(parameters.ModelOptions),
        input: catalog.ModelInputTable,
        output: (catalog.XTrain, catalog.XTest)
      );

      pipeline.AddNode(
        name: "TrainModel",
        transform: Nodes.TrainModelNode.Create(),
        input: catalog.XTrain,
        output: catalog.Regressor
      );

      pipeline.AddNode(
        name: "EvaluateModel",
        transform: Nodes.EvaluateModelNode.Create(),
        input: (catalog.Regressor, catalog.XTest),
        output: catalog.ModelMetrics
      );
    });
  }
}
