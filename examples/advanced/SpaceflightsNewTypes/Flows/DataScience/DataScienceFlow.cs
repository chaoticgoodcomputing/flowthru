using Flowthru.Core.Flows;
using SpaceflightsNewTypes.Data;
using SpaceflightsNewTypes.Flows.DataScience.Steps;

namespace SpaceflightsNewTypes.Flows.DataScience;

/// <summary>
/// Creates the data science pipeline that trains and evaluates a price prediction model.
/// </summary>
public static class DataScienceFlow
{
  /// <summary>
  /// Creates the data science pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <param name="config">Configuration catalog providing pipeline parameters.</param>
  /// <returns>A configured pipeline that produces a trained model and evaluation metrics.</returns>
  public static Flow Create(Catalog catalog, FlowConfig config)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "SplitData",
        description: "Splits model input data into training and test sets.",
        transform: SplitDataStep.Create,
        input: (catalog.ModelInputTable, config.ModelOptions),
        output: (catalog.TrainSplit, catalog.TestSplit)
      );

      pipeline.AddStep(
        label: "TrainModel",
        description: "Trains a regression model to predict shuttle prices.",
        transform: TrainModelStep.Create(),
        input: catalog.TrainSplit,
        output: catalog.Regressor
      );

      pipeline.AddStep(
        label: "EvaluateModel",
        description: "Evaluates the trained model on the test set and computes metrics and predictions.",
        transform: EvaluateModelStep.Create(),
        input: (catalog.Regressor, catalog.TestSplit),
        output: (catalog.ModelMetrics, catalog.ModelPredictions)
      );
    });
  }
}
