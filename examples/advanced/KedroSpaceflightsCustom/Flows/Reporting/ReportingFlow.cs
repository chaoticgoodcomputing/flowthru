using Flowthru.Flow;
using KedroSpaceflightsCustom.Data;
using KedroSpaceflightsCustom.Data._02_Intermediate.Schemas;
using KedroSpaceflightsCustom.Data._05_ModelOutput.Schemas;
using KedroSpaceflightsCustom.Data._06_Reporting.Schemas;
using KedroSpaceflightsCustom.Flows.Reporting.Steps;
using Plotly.NET;

namespace KedroSpaceflightsCustom.Flows.Reporting;

/// <summary>
/// Reporting pipeline that generates visualizations from processed data.
/// </summary>
public static class ReportingFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      // ===== Shuttle Passenger Capacity Visualization =====
      pipeline.AddStep<IEnumerable<ShuttleSchema>, GenericChart>(
        label: "GeneratePassengerCapacityChart",
        transform: ComparePassengerCapacityStep.Create(),
        input1: catalog.CleanedShuttles,
        output1: catalog.ShuttlePassengerCapacityChart
      );

      pipeline.AddStep<GenericChart, string>(
        label: "ExportPassengerCapacityJson",
        transform: PlotlyJsonExportStep.Create(),
        input1: catalog.ShuttlePassengerCapacityChart,
        output1: catalog.ShuttlePassengerCapacityPlot
      );

      // ===== Confusion Matrix Visualization =====
      pipeline.AddStep<IEnumerable<CompanySchema>, GenericChart>(
        label: "GenerateConfusionMatrixChart",
        transform: CreateConfusionMatrixStep.Create(),
        input1: catalog.CleanedCompanies,
        output1: catalog.ConfusionMatrixChart
      );

      pipeline.AddStep<GenericChart, string>(
        label: "ExportConfusionMatrixJson",
        transform: PlotlyJsonExportStep.Create(),
        input1: catalog.ConfusionMatrixChart,
        output1: catalog.ConfusionMatrixPlot
      );

      // ===== Cross-Validation Results Visualization =====
      pipeline.AddStep<CrossValidationResults, GenericChart>(
        label: "GenerateCrossValidationChart",
        transform: VisualizeCrossValidationStep.Create(),
        input1: catalog.CrossValidationResults,
        output1: catalog.CrossValidationChart
      );

      pipeline.AddStep<GenericChart, string>(
        label: "ExportCrossValidationJson",
        transform: PlotlyJsonExportStep.Create(),
        input1: catalog.CrossValidationChart,
        output1: catalog.CrossValidationPlot
      );

      pipeline.AddStep<CrossValidationResults, string>(
        label: "GenerateCrossValidationReport",
        transform: GenerateCrossValidationReportStep.Create(),
        input1: catalog.CrossValidationResults,
        output1: catalog.CrossValidationReport
      );

      // ===== Prediction Scatter Plot Visualization =====
      pipeline.AddStep<ModelMetrics, IEnumerable<ModelPredictions>, GenericChart>(
        label: "GeneratePredictionScatterChart",
        transform: GeneratePredictionScatterStep.Create(),
        input1: catalog.ModelMetrics,
        input2: catalog.ModelPredictions,
        output1: catalog.PredictionScatterChart
      );

      pipeline.AddStep<GenericChart, string>(
        label: "ExportPredictionScatterJson",
        transform: PlotlyJsonExportStep.Create(),
        input1: catalog.PredictionScatterChart,
        output1: catalog.PredictionScatterPlot
      );
    });
  }
}
