using Flowthru.Flow;
using Plotly.NET;
using SpaceflightsHybridCatalog.Data;
using SpaceflightsHybridCatalog.Data._02_Intermediate.Schemas;
using SpaceflightsHybridCatalog.Data._07_ModelOutput.Schemas;
using SpaceflightsHybridCatalog.Data._08_Reporting.Schemas;
using SpaceflightsHybridCatalog.Flows.Reporting.Steps;

namespace SpaceflightsHybridCatalog.Flows.Reporting;

/// <summary>
/// Reporting pipeline that generates the passenger-capacity report (JSON) and
/// the confusion-matrix chart (in-memory) from processed data.
/// </summary>
public static class ReportingFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      pipeline.AddStep<IEnumerable<PreprocessedShuttleSchema>, IEnumerable<ShuttleCapacityReport>>(
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
