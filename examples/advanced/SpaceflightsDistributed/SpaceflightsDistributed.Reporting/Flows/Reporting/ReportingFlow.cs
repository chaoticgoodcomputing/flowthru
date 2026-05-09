using Flowthru.Flow;
using Plotly.NET;
using SpaceflightsDistributed.DataProcessing.Data;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;
using SpaceflightsDistributed.DataScience.Data;
using SpaceflightsDistributed.DataScience.Data._07_ModelOutput.Schemas;
using SpaceflightsDistributed.Reporting.Data;
using SpaceflightsDistributed.Reporting.Data._08_Reporting.Schemas;
using SpaceflightsDistributed.Reporting.Flows.Reporting.Steps;

namespace SpaceflightsDistributed.Reporting.Flows.Reporting;

/// <summary>
/// Generates visualizations and reports from processed and modeled shuttle data.
/// </summary>
public static class ReportingFlow
{
  public static BuiltFlow Create(
    DataProcessingCatalog dp,
    DataScienceCatalog ds,
    ReportingCatalog r,
    ReportingFlowConfig config
  )
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      pipeline.AddStep<IEnumerable<PreprocessedShuttleSchema>, IEnumerable<ShuttleCapacityReport>>(
        label: "ComparePassengerCapacity",
        transform: ComparePassengerCapacityStep.Create(),
        input1: dp.PreprocessedShuttles,
        output1: r.ShuttleCapacityReport
      );

      pipeline.AddStep<IEnumerable<PreprocessedShuttleSchema>, GenericChart>(
        label: "GeneratePassengerCapacityChart",
        transform: GeneratePassengerCapacityChartStep.Create(),
        input1: dp.PreprocessedShuttles,
        output1: r.ShuttlePassengerCapacityChart
      );

      pipeline.AddStep<IEnumerable<ModelPredictions>, GenericChart>(
        label: "GenerateConfusionMatrixChart",
        transform: CreateConfusionMatrixStep.Create(config.ConfusionMatrixOptions),
        input1: ds.ModelPredictions,
        output1: r.ConfusionMatrixChart
      );
    });
  }
}
