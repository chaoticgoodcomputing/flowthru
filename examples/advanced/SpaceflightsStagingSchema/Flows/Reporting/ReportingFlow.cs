using Flowthru.Flow;
using SpaceflightsStagingSchema.Data;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;
using SpaceflightsStagingSchema.Data._07_ModelOutput.Schemas;
using SpaceflightsStagingSchema.Data._08_Reporting.Schemas;
using SpaceflightsStagingSchema.Flows.Reporting.Steps;
using Plotly.NET;

namespace SpaceflightsStagingSchema.Flows.Reporting;

/// <summary>
/// Reads from the production database and produces reporting outputs.
/// Never touches staging or the model input view.
/// </summary>
public static class ReportingFlow
{
  public static BuiltFlow Create(ProductionCatalog production)
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      pipeline.AddStep<IEnumerable<PreprocessedShuttleSchema>, IEnumerable<ShuttleCapacityReport>>(
        label: "ComparePassengerCapacity",
        transform: ComparePassengerCapacityStep.Create(),
        inputs: production.Shuttles,
        outputs: production.ShuttleCapacityReport
      );

      pipeline.AddStep<IEnumerable<PreprocessedShuttleSchema>, GenericChart>(
        label: "GeneratePassengerCapacityChart",
        transform: GeneratePassengerCapacityChartStep.Create(),
        inputs: production.Shuttles,
        outputs: production.ShuttlePassengerCapacityChart
      );

      pipeline.AddStep<
        IEnumerable<ModelPredictions>,
        CreateConfusionMatrixStep.Options,
        GenericChart
      >(
        label: "GenerateConfusionMatrixChart",
        transform: CreateConfusionMatrixStep.Create(),
        inputs: (production.ModelPredictions, production.ConfusionMatrixOptions),
        outputs: production.ConfusionMatrixChart
      );
    });
  }
}
