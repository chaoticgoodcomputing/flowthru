using Flowthru.Flow;
using SpaceflightsEnhanced.Data;
using SpaceflightsEnhanced.Data._02_Intermediate.Schemas;
using SpaceflightsEnhanced.Data._05_ModelOutput.Schemas;
using SpaceflightsEnhanced.Data._06_Reporting.Schemas;
using SpaceflightsEnhanced.Flows.Reporting.Steps;
using Plotly.NET;

namespace SpaceflightsEnhanced.Flows.Reporting;

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
        inputs: catalog.CleanedShuttles,
        outputs: catalog.ShuttlePassengerCapacityChart
      );

      pipeline.AddStep<GenericChart, string>(
        label: "ExportPassengerCapacityJson",
        transform: PlotlyJsonExportStep.Create(),
        inputs: catalog.ShuttlePassengerCapacityChart,
        outputs: catalog.ShuttlePassengerCapacityPlot
      );

      // ===== Confusion Matrix Visualization =====
      pipeline.AddStep<IEnumerable<CompanySchema>, GenericChart>(
        label: "GenerateConfusionMatrixChart",
        transform: CreateConfusionMatrixStep.Create(),
        inputs: catalog.CleanedCompanies,
        outputs: catalog.ConfusionMatrixChart
      );

      pipeline.AddStep<GenericChart, string>(
        label: "ExportConfusionMatrixJson",
        transform: PlotlyJsonExportStep.Create(),
        inputs: catalog.ConfusionMatrixChart,
        outputs: catalog.ConfusionMatrixPlot
      );

      // ===== Cross-Validation Results Visualization =====
      pipeline.AddStep<CrossValidationResults, GenericChart>(
        label: "GenerateCrossValidationChart",
        transform: VisualizeCrossValidationStep.Create(),
        inputs: catalog.CrossValidationResults,
        outputs: catalog.CrossValidationChart
      );

      pipeline.AddStep<GenericChart, string>(
        label: "ExportCrossValidationJson",
        transform: PlotlyJsonExportStep.Create(),
        inputs: catalog.CrossValidationChart,
        outputs: catalog.CrossValidationPlot
      );

      pipeline.AddStep<CrossValidationResults, string>(
        label: "GenerateCrossValidationReport",
        transform: GenerateCrossValidationReportStep.Create(),
        inputs: catalog.CrossValidationResults,
        outputs: catalog.CrossValidationReport
      );

      // ===== Prediction Scatter Plot Visualization =====
      pipeline.AddStep<ModelMetrics, IEnumerable<ModelPredictions>, GenericChart>(
        label: "GeneratePredictionScatterChart",
        transform: GeneratePredictionScatterStep.Create(),
        inputs: (catalog.ModelMetrics, catalog.ModelPredictions),
        outputs: catalog.PredictionScatterChart
      );

      pipeline.AddStep<GenericChart, string>(
        label: "ExportPredictionScatterJson",
        transform: PlotlyJsonExportStep.Create(),
        inputs: catalog.PredictionScatterChart,
        outputs: catalog.PredictionScatterPlot
      );
    });
  }
}
