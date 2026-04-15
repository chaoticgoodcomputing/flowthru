using Flowthru.Core.Flows;
using SpaceflightsDistributed.DataProcessing.Data;
using SpaceflightsDistributed.DataScience.Data;
using SpaceflightsDistributed.DataScience.Flows.DataScience.Steps;

namespace SpaceflightsDistributed.DataScience.Flows.DataScience;

/// <summary>
/// Trains and evaluates a price prediction model using processed shuttle data.
/// Reads the model input table from the DataProcessing catalog and writes
/// all model artifacts and outputs to the DataScience catalog.
/// </summary>
public static class DataScienceFlow
{
  /// <summary>
  /// Configuration parameters for the data science pipeline.
  /// </summary>
  public record Params
  {
    public SplitDataStep.ModelOptions ModelOptions { get; init; } = new();
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
      pipeline.AddStep(
        label: "SplitData",
        description: "Splits the model input table into training and test sets.",
        transform: SplitDataStep.Create(parameters.ModelOptions),
        input: dp.ModelInputTable,
        output: (ds.TrainSplit, ds.TestSplit)
      );

      pipeline.AddStep(
        label: "TrainModel",
        description: "Trains a regression model to predict shuttle prices.",
        transform: TrainModelStep.Create(),
        input: ds.TrainSplit,
        output: ds.Regressor
      );

      pipeline.AddStep(
        label: "EvaluateModel",
        description: "Evaluates the trained model on the test set and computes metrics.",
        transform: EvaluateModelStep.Create(),
        input: (ds.Regressor, ds.TestSplit),
        output: (ds.ModelMetrics, ds.ModelPredictions)
      );
    });
  }
}
