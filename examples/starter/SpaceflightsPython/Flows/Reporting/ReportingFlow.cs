using Flowthru.Flow;
using Flowthru.Step.Python;
using SpaceflightsPython.Data;

namespace SpaceflightsPython.Flows.Reporting;

/// <summary>
/// Reporting pipeline for generating visualization outputs.
/// </summary>
public static class ReportingFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      pipeline.AddPythonStep(
        label: "ComparePassengerCapacityExpress",
        module: "Flows.Reporting.Steps.compare_passenger_capacity",
        function: "compare_passenger_capacity_exp",
        input: catalog.PreprocessedShuttles,
        output: catalog.CapacityPlotExpress,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "ComparePassengerCapacityGraphObj",
        module: "Flows.Reporting.Steps.compare_passenger_capacity",
        function: "compare_passenger_capacity_go",
        input: catalog.PreprocessedShuttles,
        output: catalog.CapacityPlotGraphObj,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "CreateConfusionMatrix",
        module: "Flows.Reporting.Steps.create_confusion_matrix",
        function: "create_confusion_matrix",
        input: catalog.ModelPredictions,
        output: catalog.ConfusionMatrix,
        executor: executor
      );
    });
  }
}
