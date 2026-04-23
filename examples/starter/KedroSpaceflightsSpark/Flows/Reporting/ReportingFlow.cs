using Flowthru.Core.Flows;
using KedroSpaceflightsSpark.Data;
using KedroSpaceflightsSpark.Flows.Reporting.Steps;

namespace KedroSpaceflightsSpark.Flows.Reporting;

public static class ReportingFlow
{
  public static Flow Create(Catalog catalog, FlowConfig config)
  {
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
        transform: CreateConfusionMatrixStep.Create,
        input: (catalog.ModelPredictions, config.ConfusionMatrixOptions),
        output: catalog.ConfusionMatrixChart
      );

      pipeline.AddStep(
        label: "RankShuttlesByPrice",
        description: """
          Annotates each shuttle with its dense price rank and average price within its shuttle
          type using Spark window functions (SelectOver + FrameWindowSpec).
        """,
        transform: RankShuttlesByPriceStep.Create(),
        input: catalog.ModelInputTable,
        output: catalog.ShuttlePriceRanks
      );
    });
  }
}
