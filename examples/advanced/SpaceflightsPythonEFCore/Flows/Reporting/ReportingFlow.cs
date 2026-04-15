using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;
using SpaceflightsPythonEFCore.Data;
using SpaceflightsPythonEFCore.Data._02_Intermediate.Schemas;
using SpaceflightsPythonEFCore.Data._07_ModelOutput.Schemas;

namespace SpaceflightsPythonEFCore.Flows.Reporting;

/// <summary>
/// Reporting pipeline using Python nodes for all visualizations.
/// Both inputs (PreprocessedShuttles and ModelPredictions) are read from EFCore,
/// demonstrating EFCore → Python handoffs in two separate nodes.
/// </summary>
public static class ReportingFlow
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddPythonStep(
        label: "ComparePassengerCapacityExpress",
        description: "Shuttle capacity bar chart via plotly.express (Python). Reads PreprocessedShuttles from EFCore.",
        module: "Flows.Reporting.Steps.compare_passenger_capacity",
        function: "compare_passenger_capacity_exp",
        input: catalog.PreprocessedShuttles,
        output: catalog.CapacityPlotExpress,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "ComparePassengerCapacityGraphObj",
        description: "Shuttle capacity bar chart via plotly.graph_objects (Python). Reads PreprocessedShuttles from EFCore.",
        module: "Flows.Reporting.Steps.compare_passenger_capacity",
        function: "compare_passenger_capacity_go",
        input: catalog.PreprocessedShuttles,
        output: catalog.CapacityPlotGraphObj,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "CreateConfusionMatrix",
        description: "Confusion matrix heatmap from model predictions (Python). Reads ModelPredictions from EFCore.",
        module: "Flows.Reporting.Steps.create_confusion_matrix",
        function: "create_confusion_matrix",
        input: catalog.ModelPredictions,
        output: catalog.ConfusionMatrix,
        executor: executor
      );
    });
  }
}
