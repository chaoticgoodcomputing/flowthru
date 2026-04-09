using Flowthru.Core.Flows;
using SpaceflightsDistributed.DataProcessing.Data;
using SpaceflightsDistributed.DataScience.Data;
using SpaceflightsDistributed.Reporting.Data;
using SpaceflightsDistributed.Reporting.Flows.Reporting.Steps;

namespace SpaceflightsDistributed.Reporting.Flows.Reporting;

/// <summary>
/// Generates visualizations and reports from processed and modeled shuttle data.
/// This pipeline signature expresses its cross-catalog dependencies directly:
/// it reads from DataProcessing (preprocessed shuttles) and DataScience (model predictions),
/// and writes all outputs to its own ReportingCatalog.
/// </summary>
public static class ReportingFlow
{
  public record Params
  {
    public CreateConfusionMatrixStep.Options ConfusionMatrixOptions { get; init; } = new();
  }

  /// <summary>
  /// Creates the reporting pipeline.
  /// </summary>
  /// <param name="dp">The data processing catalog supplying preprocessed shuttle data.</param>
  /// <param name="ds">The data science catalog supplying model predictions.</param>
  /// <param name="r">The reporting catalog receiving all report and chart outputs.</param>
  /// <param name="parameters">Configuration for the pipeline.</param>
  public static Flow Create(
    DataProcessingCatalog dp,
    DataScienceCatalog ds,
    ReportingCatalog r,
    Params parameters
  )
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "ComparePassengerCapacity",
        description: "Aggregates average shuttle passenger capacity grouped by shuttle type.",
        transform: ComparePassengerCapacityStep.Create(),
        input: dp.PreprocessedShuttles,
        output: r.ShuttleCapacityReport
      );

      pipeline.AddStep(
        label: "GeneratePassengerCapacityChart",
        description: "Generates a bar chart of passenger capacity rankings by shuttle type.",
        transform: GeneratePassengerCapacityChartStep.Create(),
        input: dp.PreprocessedShuttles,
        output: r.ShuttlePassengerCapacityChart
      );

      pipeline.AddStep(
        label: "GenerateConfusionMatrixChart",
        description: "Generates a confusion matrix heatmap from model price predictions.",
        transform: CreateConfusionMatrixStep.Create(parameters.ConfusionMatrixOptions),
        input: ds.ModelPredictions,
        output: r.ConfusionMatrixChart
      );
    });
  }
}
