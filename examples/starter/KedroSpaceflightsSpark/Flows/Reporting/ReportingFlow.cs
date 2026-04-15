using Flowthru.Core.Flows;
using KedroSpaceflightsSpark.Data;
using KedroSpaceflightsSpark.Flows.Reporting.Steps;

namespace KedroSpaceflightsSpark.Flows.Reporting;

public static class ReportingFlow
{
  public record Params
  {
    public CreateConfusionMatrixStep.Options ConfusionMatrixOptions { get; init; } = new();
  }

  public static Flow Create(Catalog catalog, Params? parameters = null)
  {
    var p = parameters ?? new Params();

    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "ComparePassengerCapacity",
        description: "Aggregates average passenger capacity by shuttle type using Spark GroupBy.",
        transform: ComparePassengerCapacityStep.Create(),
        input: catalog.PreprocessedShuttles,
        output: catalog.ShuttleCapacityReport
      );

      pipeline.AddStep(
        label: "GeneratePassengerCapacityChart",
        transform: GeneratePassengerCapacityChartStep.Create(),
        input: catalog.PreprocessedShuttles,
        output: catalog.ShuttlePassengerCapacityChart
      );

      pipeline.AddStep(
        label: "GenerateConfusionMatrixChart",
        transform: CreateConfusionMatrixStep.Create(p.ConfusionMatrixOptions),
        input: catalog.ModelPredictions,
        output: catalog.ConfusionMatrixChart
      );
    });
  }
}
