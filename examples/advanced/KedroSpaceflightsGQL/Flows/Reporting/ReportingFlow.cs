using Flowthru.Core.Flows;
using KedroSpaceflightsGQL.Data;
using KedroSpaceflightsGQL.Flows.Reporting.Steps;

namespace KedroSpaceflightsGQL.Flows.Reporting;

/// <summary>
/// Reporting pipeline that generates visualizations from processed data.
/// Matches Kedro spaceflights reporting pipeline structure.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Flow Purpose:</strong> Generate visualizations for data exploration using Plotly.NET.
/// Charts are first created in memory, then exported to PNG format for reports.
/// </para>
/// <para>
/// <strong>Architecture:</strong>
/// This pipeline follows a two-stage pattern for each visualization:
/// 1. Chart Generation (data → GenericChart in memory)
/// 2. PNG Export (GenericChart → PNG binary file)
///
/// This separation enables reusable export nodes across different chart types.
/// </para>
/// </remarks>
public static class ReportingFlow
{
  /// <summary>
  /// Creates the reporting pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <param name="config">Configuration catalog providing pipeline parameters.</param>
  /// <returns>A configured pipeline that produces visualizations and reports.</returns>
  public static Flow Create(Catalog catalog, FlowConfig config)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // ===== Shuttle Passenger Capacity Report (JSON) =====

      pipeline.AddStep(
        label: "ComparePassengerCapacity",
        transform: ComparePassengerCapacityStep.Create(),
        input: catalog.PreprocessedShuttles,
        output: catalog.ShuttleCapacityReport
      );

      // ===== Shuttle Passenger Capacity Visualization =====

      // Step 1: Generate chart from preprocessed shuttle data
      pipeline.AddStep(
        label: "GeneratePassengerCapacityChart",
        transform: GeneratePassengerCapacityChartStep.Create(),
        input: catalog.PreprocessedShuttles,
        output: catalog.ShuttlePassengerCapacityChart
      );

      // NOTE: Commented out due to performance issues with Plotly.NET
      // // Step 2: Export chart to PNG for static reports
      // pipeline.AddStep(
      //   label: "ExportPassengerCapacityPng",
      //   transform: PlotlyImageExportStep.Create(),
      //   input: catalog.ShuttlePassengerCapacityChart,
      //   output: catalog.ShuttlePassengerCapacityPlotPng
      // );

      // ===== Confusion Matrix Visualization =====

      // Step 1: Generate confusion matrix heatmap from model predictions
      pipeline.AddStep(
        label: "GenerateConfusionMatrixChart",
        transform: CreateConfusionMatrixStep.Create,
        input: (catalog.ModelPredictions, config.ConfusionMatrixOptions),
        output: catalog.ConfusionMatrixChart
      );

      // NOTE: Commented out due to performance issues with Plotly.NET
      // // Step 2: Export chart to PNG for static reports
      // pipeline.AddStep(
      //   label: "ExportConfusionMatrixPng",
      //   transform: PlotlyImageExportStep.Create(),
      //   input: catalog.ConfusionMatrixChart,
      //   output: catalog.ConfusionMatrixPlotPng
      // );
    });
  }
}
