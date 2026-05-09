using Flowthru.Flow;
using Flowthru.Step.Python;
using KedroIrisPython.Data;

namespace KedroIrisPython.Flows.DataScience;

/// <summary>
/// Data science pipeline using Python nodes for model training, prediction, and evaluation.
/// </summary>
public static class DataScienceFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("DataScience", pipeline =>
    {
      pipeline.AddPythonStep(
        label: "TrainModel",
        module: "Flows.DataScience.Steps.train_model",
        function: "train_model",
        input: (catalog.TrainX, catalog.TrainY),
        output: catalog.ModelWeights,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "Predict",
        module: "Flows.DataScience.Steps.predict",
        function: "predict",
        input: (catalog.ModelWeights, catalog.TestX),
        output: catalog.Predictions,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "ReportAccuracy",
        module: "Flows.DataScience.Steps.report_accuracy",
        function: "report_accuracy",
        input: (catalog.Predictions, catalog.TestY),
        output: catalog.AccuracyReport,
        executor: executor
      );
    });
  }
}
