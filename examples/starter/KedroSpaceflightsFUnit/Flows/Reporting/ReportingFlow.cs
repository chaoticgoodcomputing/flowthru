using Flowthru.Flow;
using KedroSpaceflightsFUnit.Data;
using KedroSpaceflightsFUnit.Data._02_Intermediate.Schemas;
using KedroSpaceflightsFUnit.Data._07_ModelOutput.Schemas;
using KedroSpaceflightsFUnit.Data._08_Reporting.Schemas;
using KedroSpaceflightsFUnit.Flows.Reporting.Steps;
using Plotly.NET;

namespace KedroSpaceflightsFUnit.Flows.Reporting;

/// <summary>
/// Reporting pipeline that generates visualizations from processed data.
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
