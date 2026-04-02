using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;
using Flowthru.Flows;
using KedroIrisPython.Data;
using KedroIrisPython.Data._05_ModelInput.Schemas;
using KedroIrisPython.Data._07_ModelOutput.Schemas;
using KedroIrisPython.Data._08_Reporting.Schemas;

namespace KedroIrisPython.Pipelines.DataScience;

/// <summary>
/// Data science pipeline using Python nodes for model training, prediction, and evaluation.
/// </summary>
public static class DataSciencePipeline
{
  /// <summary>
  /// Creates the data science pipeline.
  /// </summary>
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // Train model using training data
      pipeline.AddPythonNode<
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<TargetLabelSchema>,
        byte[]
      >(
        label: "TrainModel",
        description: "Train multi-class logistic regression (Python 2×1 node)",
        module: "Pipelines.DataScience.Nodes.train_model",
        function: "train_model",
        input: (catalog.TrainX, catalog.TrainY),
        output: catalog.ModelWeights,
        executor: executor
      );

      // Generate predictions using trained model
      pipeline.AddPythonNode<
        byte[],
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<PredictionSchema>
      >(
        label: "Predict",
        description: "Generate predictions on test set (Python 2×1 node)",
        module: "Pipelines.DataScience.Nodes.predict",
        function: "predict",
        input: (catalog.ModelWeights, catalog.TestX),
        output: catalog.Predictions,
        executor: executor
      );

      // Report accuracy metrics
      pipeline.AddPythonNode<
        IEnumerable<PredictionSchema>,
        IEnumerable<TargetLabelSchema>,
        AccuracyReportSchema
      >(
        label: "ReportAccuracy",
        description: "Calculate and save accuracy metrics (Python 2×1 node)",
        module: "Pipelines.DataScience.Nodes.report_accuracy",
        function: "report_accuracy",
        input: (catalog.Predictions, catalog.TestY),
        output: catalog.AccuracyReport,
        executor: executor
      );
    });
  }
}
