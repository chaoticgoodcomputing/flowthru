using Flowthru.Flows;
using SpaceflightsDistributed.DataProcessing.Data;
using SpaceflightsDistributed.DataScience.Data;
using SpaceflightsDistributed.DataScience.Pipelines.DataScience.Nodes;

namespace SpaceflightsDistributed.DataScience.Pipelines.DataScience;

/// <summary>
/// Trains and evaluates a price prediction model using processed shuttle data.
/// Reads the model input table from the DataProcessing catalog and writes
/// all model artifacts and outputs to the DataScience catalog.
/// </summary>
public static class DataSciencePipeline
{
  /// <summary>
  /// Configuration parameters for the data science pipeline.
  /// </summary>
  public record Params
  {
    public SplitDataNode.ModelOptions ModelOptions { get; init; } = new();
  }

  /// <summary>
  /// Creates the data science pipeline.
  /// This pipeline signature expresses its cross-catalog dependency directly:
  /// it requires both a DataProcessingCatalog (data source) and a
  /// DataScienceCatalog (model output sink).
  /// </summary>
  /// <param name="dp">The data processing catalog supplying the model input table.</param>
  /// <param name="ds">The data science catalog receiving splits, model, and metrics.</param>
  /// <param name="parameters">Configuration parameters for the pipeline.</param>
  public static Flow Create(DataProcessingCatalog dp, DataScienceCatalog ds, Params parameters)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddNode(
        label: "SplitData",
        description: "Splits the model input table into training and test sets.",
        transform: SplitDataNode.Create(parameters.ModelOptions),
        input: dp.ModelInputTable,
        output: (ds.TrainSplit, ds.TestSplit)
      );

      pipeline.AddStep(
        label: "TrainModel",
        description: "Trains a regression model to predict shuttle prices.",
        transform: TrainModelNode.Create(),
        input: ds.TrainSplit,
        output: ds.Regressor
      );

      pipeline.AddNode(
        label: "EvaluateModel",
        description: "Evaluates the trained model on the test set and computes metrics.",
        transform: EvaluateModelNode.Create(),
        input: (ds.Regressor, ds.TestSplit),
        output: (ds.ModelMetrics, ds.ModelPredictions)
      );
    });
  }
}
