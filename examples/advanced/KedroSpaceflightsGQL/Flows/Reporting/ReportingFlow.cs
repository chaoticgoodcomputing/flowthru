using Flowthru.Flow;
using KedroSpaceflightsGQL.Data;
using KedroSpaceflightsGQL.Data._02_Intermediate.Schemas;
using KedroSpaceflightsGQL.Data._07_ModelOutput.Schemas;
using KedroSpaceflightsGQL.Data._08_Reporting.Schemas;
using KedroSpaceflightsGQL.Flows.Reporting.Steps;
using Plotly.NET;

namespace KedroSpaceflightsGQL.Flows.Reporting;

/// <summary>
/// Reporting pipeline that generates visualizations from processed data.
/// Matches Kedro spaceflights reporting pipeline structure.
/// </summary>
public static class ReportingFlow
{
  public static BuiltFlow Create(Catalog catalog, FlowConfig config)
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      pipeline.AddStep<IEnumerable<PreprocessedShuttleSchema>, IEnumerable<ShuttleCapacityReport>>(
        label: "ComparePassengerCapacity",
        transform: ComparePassengerCapacityStep.Create(),
        input1: catalog.PreprocessedShuttles,
        output1: catalog.ShuttleCapacityReport
      );

      pipeline.AddStep<IEnumerable<PreprocessedShuttleSchema>, GenericChart>(
        label: "GeneratePassengerCapacityChart",
        transform: GeneratePassengerCapacityChartStep.Create(),
        input1: catalog.PreprocessedShuttles,
        output1: catalog.ShuttlePassengerCapacityChart
      );

      pipeline.AddStep<IEnumerable<ModelPredictions>, GenericChart>(
        label: "GenerateConfusionMatrixChart",
        transform: CreateConfusionMatrixStep.Create(config.ConfusionMatrixOptions),
        input1: catalog.ModelPredictions,
        output1: catalog.ConfusionMatrixChart
      );
    });
  }
}
