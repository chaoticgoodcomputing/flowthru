using Flowthru.Core.Flows;
using SpaceflightsStagingSchema.Data;
using SpaceflightsStagingSchema.Flows.Reporting.Steps;

namespace SpaceflightsStagingSchema.Flows.Reporting;

/// <summary>
/// Reads from the production database (production.Shuttles for the capacity
/// report and chart, production.ModelPredictions for the confusion matrix) and
/// produces reporting outputs. Never touches staging or the model input view.
/// </summary>
/// <remarks>
/// Capacity reports source from <c>production.Shuttles</c> rather than the
/// model input table. The model input table is restricted to shuttles that
/// have at least one review (because of the inner join on ShuttleId);
/// capacity-by-shuttle-type rollups are more useful when they cover the
/// canonical shuttle catalog.
/// </remarks>
public static class ReportingFlow
{
  public static Flow Create(ProductionCatalog production, FlowConfig config)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "ComparePassengerCapacity",
        transform: ComparePassengerCapacityStep.Create(),
        input: production.Shuttles,
        output: production.ShuttleCapacityReport
      );

      pipeline.AddStep(
        label: "GeneratePassengerCapacityChart",
        transform: GeneratePassengerCapacityChartStep.Create(),
        input: production.Shuttles,
        output: production.ShuttlePassengerCapacityChart
      );

      pipeline.AddStep(
        label: "GenerateConfusionMatrixChart",
        transform: CreateConfusionMatrixStep.Create,
        input: (production.ModelPredictions, config.ConfusionMatrixOptions),
        output: production.ConfusionMatrixChart
      );
    });
  }
}
