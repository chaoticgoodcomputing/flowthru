using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;
using Flowthru.Flows;
using KedroSpaceflightsPython.Data;
using KedroSpaceflightsPython.Data._02_Intermediate.Schemas;
using KedroSpaceflightsPython.Data._07_ModelOutput.Schemas;

namespace KedroSpaceflightsPython.Pipelines.Reporting;

/// <summary>
/// Reporting pipeline for generating visualization outputs.
/// Contains nodes for creating passenger capacity plots and confusion matrices.
/// </summary>
public static class ReportingPipeline
{
  /// <summary>
  /// Creates the reporting pipeline.
  /// </summary>
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // Compare passenger capacity using plotly.express
      pipeline.AddPythonStep(
        label: "ComparePassengerCapacityExpress",
        description: "Generate passenger capacity bar chart using plotly.express",
        module: "Pipelines.Reporting.Nodes.compare_passenger_capacity",
        function: "compare_passenger_capacity_exp",
        input: catalog.PreprocessedShuttles,
        output: catalog.CapacityPlotExpress,
        executor: executor
      );

      // Compare passenger capacity using plotly.graph_objects
      pipeline.AddPythonStep(
        label: "ComparePassengerCapacityGraphObj",
        description: "Generate passenger capacity bar chart using plotly.graph_objects",
        module: "Pipelines.Reporting.Nodes.compare_passenger_capacity",
        function: "compare_passenger_capacity_go",
        input: catalog.PreprocessedShuttles,
        output: catalog.CapacityPlotGraphObj,
        executor: executor
      );

      // Create confusion matrix from model predictions
      pipeline.AddPythonStep(
        label: "CreateConfusionMatrix",
        description: "Generate confusion matrix heatmap from model predictions (binned into categories)",
        module: "Pipelines.Reporting.Nodes.create_confusion_matrix",
        function: "create_confusion_matrix",
        input: catalog.ModelPredictions,
        output: catalog.ConfusionMatrix,
        executor: executor
      );
    });
  }
}
