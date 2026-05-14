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
    ReportingCatalog r
  )
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      pipeline.AddStep<IEnumerable<PreprocessedShuttleSchema>, IEnumerable<ShuttleCapacityReport>>(
        label: "ComparePassengerCapacity",
        transform: ComparePassengerCapacityStep.Create(),
        inputs: dp.PreprocessedShuttles,
        outputs: r.ShuttleCapacityReport
      );

      pipeline.AddStep<IEnumerable<PreprocessedShuttleSchema>, GenericChart>(
        label: "GeneratePassengerCapacityChart",
        transform: GeneratePassengerCapacityChartStep.Create(),
        inputs: dp.PreprocessedShuttles,
        outputs: r.ShuttlePassengerCapacityChart
      );

      pipeline.AddStep<
        IEnumerable<ModelPredictions>,
        CreateConfusionMatrixStep.Options,
        GenericChart
      >(
        label: "GenerateConfusionMatrixChart",
        transform: CreateConfusionMatrixStep.Create(),
        inputs: (ds.ModelPredictions, r.ConfusionMatrixOptions),
        outputs: r.ConfusionMatrixChart
      );
    });
  }
}
