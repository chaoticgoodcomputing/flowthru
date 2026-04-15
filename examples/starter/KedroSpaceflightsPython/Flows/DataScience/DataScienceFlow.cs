using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;
using KedroSpaceflightsPython.Data;
using KedroSpaceflightsPython.Data._03_Primary.Schemas;
using KedroSpaceflightsPython.Data._05_ModelInput.Schemas;
using KedroSpaceflightsPython.Data._06_Models.Schemas;
using KedroSpaceflightsPython.Data._07_ModelOutput.Schemas;

namespace KedroSpaceflightsPython.Flows.DataScience;

/// <summary>
/// Data science pipeline using Python nodes for training and evaluation.
/// </summary>
public static class DataScienceFlow
{
    /// <summary>
    /// Creates the data science pipeline.
    /// </summary>
    public static Flow Create(Catalog catalog, IPythonExecutor executor)
    {
        return FlowBuilder.CreateFlow(pipeline =>
        {
            // Split data into train/test sets (1×4 output node)
            pipeline.AddPythonStep<
          IEnumerable<ModelInputTableSchema>,
          IEnumerable<XValues>,
          IEnumerable<XValues>,
          IEnumerable<YValues>,
          IEnumerable<YValues>
        >(
          label: "SplitData",
          description: "Split model input into train/test sets (Python 1×4 node)",
          module: "Flows.DataScience.Steps.split_data",
          function: "split_data",
          input: catalog.ModelInputTable,
          output: (catalog.XTrain, catalog.XTest, catalog.YTrain, catalog.YTest),
          executor: executor
        );

            // Train model (2×1 input node, returns sklearn model object)
            pipeline.AddPythonStep(
          label: "TrainModel",
          description: "Train linear regression model (Python 2×1 node)",
          module: "Flows.DataScience.Steps.train_model",
          function: "train_model",
          input: (catalog.XTrain, catalog.YTrain),
          output: catalog.Regressor,
          executor: executor
        );

            // Evaluate model (3×1 input node with sklearn model object)
            pipeline.AddPythonStep<
          LinearRegressionModel,
          IEnumerable<XValues>,
          IEnumerable<YValues>,
          ModelMetrics
        >(
          label: "EvaluateModel",
          description: "Compute model performance metrics (Python 3×1 node)",
          module: "Flows.DataScience.Steps.evaluate_model",
          function: "evaluate_model",
          input: (catalog.Regressor, catalog.XTest, catalog.YTest),
          output: catalog.ModelMetrics,
          executor: executor
        );

            // Generate predictions for visualization (3×1 input node)
            pipeline.AddPythonStep<
          LinearRegressionModel,
          IEnumerable<XValues>,
          IEnumerable<YValues>,
          IEnumerable<ModelPredictions>
        >(
          label: "GeneratePredictions",
          description: "Generate predictions from the trained model for visualization",
          module: "Flows.DataScience.Steps.generate_predictions",
          function: "generate_predictions",
          input: (catalog.Regressor, catalog.XTest, catalog.YTest),
          output: catalog.ModelPredictions,
          executor: executor
        );
        });
    }
}
