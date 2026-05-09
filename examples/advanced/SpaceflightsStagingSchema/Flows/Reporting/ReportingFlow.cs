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
  public static BuiltFlow Create(ProductionCatalog production, FlowConfig config)
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      pipeline.AddStep<IEnumerable<PreprocessedShuttleSchema>, IEnumerable<ShuttleCapacityReport>>(
        label: "ComparePassengerCapacity",
        transform: ComparePassengerCapacityStep.Create(),
        input1: production.Shuttles,
        output1: production.ShuttleCapacityReport
      );

      pipeline.AddStep<IEnumerable<PreprocessedShuttleSchema>, GenericChart>(
        label: "GeneratePassengerCapacityChart",
        transform: GeneratePassengerCapacityChartStep.Create(),
        input1: production.Shuttles,
        output1: production.ShuttlePassengerCapacityChart
      );

      pipeline.AddStep<IEnumerable<ModelPredictions>, GenericChart>(
        label: "GenerateConfusionMatrixChart",
        transform: CreateConfusionMatrixStep.Create(config.ConfusionMatrixOptions),
        input1: production.ModelPredictions,
        output1: production.ConfusionMatrixChart
      );
    });
  }
}
