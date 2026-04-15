using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;
using SpaceflightsPythonEFCore.Data;
using SpaceflightsPythonEFCore.Data._03_Primary.Schemas;
using SpaceflightsPythonEFCore.Data._05_ModelInput.Schemas;
using SpaceflightsPythonEFCore.Data._06_Models.Schemas;
using SpaceflightsPythonEFCore.Data._07_ModelOutput.Schemas;

namespace SpaceflightsPythonEFCore.Flows.DataScience;

/// <summary>
/// Data science pipeline using Python nodes for splitting, training, evaluating, and predicting.
///
/// EFCore → Python handoff: split_data reads ModelInputTable from SQLite.
/// Python → EFCore handoff: generate_predictions writes ModelPredictions to SQLite.
/// </summary>
public static class DataScienceFlow
{
    public static Flow Create(Catalog catalog, IPythonExecutor executor)
    {
        return FlowBuilder.CreateFlow(pipeline =>
        {
            pipeline.AddPythonStep<
          IEnumerable<ModelInputTableSchema>,
          IEnumerable<XValues>,
          IEnumerable<XValues>,
          IEnumerable<YValues>,
          IEnumerable<YValues>
        >(
          label: "SplitData",
          description: "Split EFCore model input table into train/test sets (Python). EFCore → Python handoff.",
          module: "Flows.DataScience.Steps.split_data",
          function: "split_data",
          input: catalog.ModelInputTable,
          output: (catalog.XTrain, catalog.XTest, catalog.YTrain, catalog.YTest),
          executor: executor
        );

            pipeline.AddPythonStep(
          label: "TrainModel",
          description: "Train linear regression model on train split (Python).",
          module: "Flows.DataScience.Steps.train_model",
          function: "train_model",
          input: (catalog.XTrain, catalog.YTrain),
          output: catalog.Regressor,
          executor: executor
        );

            pipeline.AddPythonStep<
          LinearRegressionModel,
          IEnumerable<XValues>,
          IEnumerable<YValues>,
          ModelMetrics
        >(
          label: "EvaluateModel",
          description: "Compute model performance metrics on test split (Python).",
          module: "Flows.DataScience.Steps.evaluate_model",
          function: "evaluate_model",
          input: (catalog.Regressor, catalog.XTest, catalog.YTest),
          output: catalog.ModelMetrics,
          executor: executor
        );

            pipeline.AddPythonStep<
          LinearRegressionModel,
          IEnumerable<XValues>,
          IEnumerable<YValues>,
          IEnumerable<ModelPredictions>
        >(
          label: "GeneratePredictions",
          description: "Generate predictions from trained model (Python). Python → EFCore handoff.",
          module: "Flows.DataScience.Steps.generate_predictions",
          function: "generate_predictions",
          input: (catalog.Regressor, catalog.XTest, catalog.YTest),
          output: catalog.ModelPredictions,
          executor: executor
        );
        });
    }
}
