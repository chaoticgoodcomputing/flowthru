using Flowthru.Flow;
using Flowthru.Step.Python;
using KedroSpaceflightsPython.Data;

namespace KedroSpaceflightsPython.Flows.DataScience;

/// <summary>
/// Data science pipeline using Python nodes for training and evaluation.
/// </summary>
public static class DataScienceFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("DataScience", pipeline =>
    {
      pipeline.AddPythonStep(
        label: "SplitData",
        module: "Flows.DataScience.Steps.split_data",
        function: "split_data",
        input: catalog.ModelInputTable,
        output: (catalog.XTrain, catalog.XTest, catalog.YTrain, catalog.YTest),
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "TrainModel",
        module: "Flows.DataScience.Steps.train_model",
        function: "train_model",
        input: (catalog.XTrain, catalog.YTrain),
        output: catalog.Regressor,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "EvaluateModel",
        module: "Flows.DataScience.Steps.evaluate_model",
        function: "evaluate_model",
        input: (catalog.Regressor, catalog.XTest, catalog.YTest),
        output: catalog.ModelMetrics,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "GeneratePredictions",
        module: "Flows.DataScience.Steps.generate_predictions",
        function: "generate_predictions",
        input: (catalog.Regressor, catalog.XTest, catalog.YTest),
        output: catalog.ModelPredictions,
        executor: executor
      );
    });
  }
}
