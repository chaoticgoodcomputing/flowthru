using Flowthru.Core.Flows;
using KedroSpaceflightsSpark.Data;
using KedroSpaceflightsSpark.Flows.DataScience.Steps;

namespace KedroSpaceflightsSpark.Flows.DataScience;

public static class DataScienceFlow
{
  public record Params
  {
    public SplitDataStep.ModelOptions ModelOptions { get; init; } = new();
  }

  public static Flow Create(Catalog catalog, Params parameters)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "SplitData",
        description: "Splits model input data into training and test sets.",
        transform: SplitDataStep.Create(parameters.ModelOptions),
        input: catalog.ModelInputTable,
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
        description: "Evaluates the trained model on the test set.",
        transform: EvaluateModelStep.Create(),
        input: (catalog.Regressor, catalog.TestSplit),
        output: (catalog.ModelMetrics, catalog.ModelPredictions)
      );
    });
  }
}
