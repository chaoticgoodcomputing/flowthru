using Flowthru.Flow;
using Plotly.NET;
using SpaceflightsEFCore.Data;
using SpaceflightsEFCore.Data._02_Intermediate.Schemas;
using SpaceflightsEFCore.Data._07_ModelOutput.Schemas;
using SpaceflightsEFCore.Data._08_Reporting.Schemas;
using SpaceflightsEFCore.Flows.Reporting.Steps;

namespace SpaceflightsEFCore.Flows.Reporting;

/// <summary>
/// Reporting pipeline: produces tabular reports + in-memory chart objects
/// for downstream visualisation. PNG export is currently disabled because
/// Plotly.NET's image pipeline is too slow to run as part of every flow.
/// </summary>
public static class ReportingFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<PreprocessedShuttleSchema>,
        IEnumerable<ShuttleCapacityReport>
      >(
        label: "ComparePassengerCapacity",
        transform: ComparePassengerCapacityStep.Create(),
        inputs: catalog.PreprocessedShuttles,
        outputs: catalog.ShuttleCapacityReport
      );

      pipeline.AddStep<IEnumerable<PreprocessedShuttleSchema>, GenericChart>(
        label: "GeneratePassengerCapacityChart",
        transform: GeneratePassengerCapacityChartStep.Create(),
        inputs: catalog.PreprocessedShuttles,
        outputs: catalog.ShuttlePassengerCapacityChart
      );

      pipeline.AddStep<
        IEnumerable<ModelPredictions>,
        CreateConfusionMatrixStep.Options,
        GenericChart
      >(
        label: "GenerateConfusionMatrixChart",
        transform: CreateConfusionMatrixStep.Create(),
        inputs: (catalog.ModelPredictions, catalog.ConfusionMatrixOptions),
        outputs: catalog.ConfusionMatrixChart
      );
    });
  }
}
