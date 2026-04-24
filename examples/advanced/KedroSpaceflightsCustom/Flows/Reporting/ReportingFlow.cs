using Flowthru.Core.Flows;
using Flowthru.Core.Steps;
using KedroSpaceflightsCustom.Data;
using KedroSpaceflightsCustom.Flows.Reporting.Steps;

namespace KedroSpaceflightsCustom.Flows.Reporting;

/// <summary>
/// Reporting pipeline that generates visualizations from processed data.
/// Matches Kedro spaceflights reporting pipeline structure with improved architecture.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Flow Purpose:</strong> Generate interactive and static visualizations for data
/// exploration and model evaluation using Plotly.NET. Charts are first created in memory,
/// then exported to multiple formats (JSON and base64-encoded PNG).
/// </para>
/// <para>
/// <strong>Architecture:</strong>
/// This pipeline follows a three-stage pattern for each visualization:
/// 1. Chart Generation (data → GenericChart in memory)
/// 2. JSON Export (GenericChart → plotly.js JSON file)
/// 3. PNG Export (GenericChart → base64-encoded PNG string)
///
/// This separation enables:
/// - Reusable export nodes across different chart types
/// - Multiple output formats from single chart generation
/// - Better type safety with compile-time checked data flow
/// - Clear separation between visualization logic and serialization
/// - Pure functional architecture with no side-effects
/// </para>
/// <para>
/// <strong>Base64 PNG Storage:</strong>
/// PNG images are stored as base64-encoded strings in FileCatalogObject&lt;string&gt;,
/// allowing binary image data to be stored without requiring BinaryFileCatalogObject support.
/// Base64 strings can be decoded to raw PNG bytes or embedded directly in HTML data URLs.
/// </para>
/// <para>
/// <strong>Kedro Equivalence:</strong> This pipeline matches the Kedro spaceflights reporting
/// pipeline, replacing matplotlib/plotly.express with Plotly.NET for .NET-native visualization.
/// </para>
/// </remarks>
public static class ReportingFlow
{
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // ===== Shuttle Passenger Capacity Visualization =====

      // Step 1: Generate chart from processed shuttle data
      pipeline.AddStep(
        label: "GeneratePassengerCapacityChart",
        transform: ComparePassengerCapacityStep.Create(),
        input: catalog.CleanedShuttles,
        output: catalog.ShuttlePassengerCapacityChart
      );

      // Step 2: Export chart to JSON for interactive visualization
      pipeline.AddStep(
        label: "ExportPassengerCapacityJson",
        transform: PlotlyJsonExportStep.Create(),
        input: catalog.ShuttlePassengerCapacityChart,
        output: catalog.ShuttlePassengerCapacityPlot
      );

      // Removed to cut down Chromium dependency for test runs.
      // // Step 3: Export chart to base64-encoded PNG for static reports
      // pipeline.AddStep(
      //   label: "ExportPassengerCapacityPng",
      //   transform: PlotlyImageExportStep.Create(),
      //   input: catalog.ShuttlePassengerCapacityChart,
      //   output: catalog.ShuttlePassengerCapacityPlotPng
      // );

      // ===== Confusion Matrix Visualization =====

      // Step 1: Generate confusion matrix heatmap from company data
      pipeline.AddStep(
        label: "GenerateConfusionMatrixChart",
        transform: CreateConfusionMatrixStep.Create(),
        input: catalog.CleanedCompanies,
        output: catalog.ConfusionMatrixChart
      );

      // Step 2: Export chart to JSON for interactive visualization
      pipeline.AddStep(
        label: "ExportConfusionMatrixJson",
        transform: PlotlyJsonExportStep.Create(),
        input: catalog.ConfusionMatrixChart,
        output: catalog.ConfusionMatrixPlot
      );

      // Removed to cut down Chromium dependency for test runs.
      // // Step 3: Export chart to base64-encoded PNG for static reports
      // pipeline.AddStep(
      //   label: "ExportConfusionMatrixPng",
      //   transform: PlotlyImageExportStep.Create(),
      //   input: catalog.ConfusionMatrixChart,
      //   output: catalog.ConfusionMatrixPlotPng
      // );

      // ===== Cross-Validation Results Visualization =====

      // Step 1: Generate comprehensive cross-validation chart
      pipeline.AddStep(
        label: "GenerateCrossValidationChart",
        transform: VisualizeCrossValidationStep.Create(),
        input: catalog.CrossValidationResults,
        output: catalog.CrossValidationChart
      );

      // Step 2: Export chart to JSON for interactive visualization
      pipeline.AddStep(
        label: "ExportCrossValidationJson",
        transform: PlotlyJsonExportStep.Create(),
        input: catalog.CrossValidationChart,
        output: catalog.CrossValidationPlot
      );

      // Removed to cut down Chromium dependency for test runs.
      // Step 3: Export chart to base64-encoded PNG for static reports
      // pipeline.AddStep(
      //   label: "ExportCrossValidationPng",
      //   transform: PlotlyImageExportStep.Create(),
      //   input: catalog.CrossValidationChart,
      //   output: catalog.CrossValidationPlotPng
      // );

      // Step 6: Generate human-readable Markdown report from cross-validation results
      pipeline.AddStep(
        label: "GenerateCrossValidationReport",
        transform: GenerateCrossValidationReportStep.Create(),
        input: catalog.CrossValidationResults,
        output: catalog.CrossValidationReport
      );

      // ===== Prediction Scatter Plot Visualization =====

      // Step 1: Generate scatter plot from model metrics and predictions
      pipeline.AddStep(
        label: "GeneratePredictionScatterChart",
        transform: GeneratePredictionScatterStep.Create(),
        input: (catalog.ModelMetrics, catalog.ModelPredictions),
        output: catalog.PredictionScatterChart
      );

      // Step 2: Export chart to JSON for interactive visualization
      pipeline.AddStep(
        label: "ExportPredictionScatterJson",
        transform: PlotlyJsonExportStep.Create(),
        input: catalog.PredictionScatterChart,
        output: catalog.PredictionScatterPlot
      );

      // Removed to cut down Chromium dependency for test runs.
      // // Step 3: Export chart to PNG for static reports
      // pipeline.AddStep(
      //   label: "ExportPredictionScatterPng",
      //   transform: PlotlyImageExportStep.Create(),
      //   input: catalog.PredictionScatterChart,
      //   output: catalog.PredictionScatterPlotPng
      // );
    });
  }
}
