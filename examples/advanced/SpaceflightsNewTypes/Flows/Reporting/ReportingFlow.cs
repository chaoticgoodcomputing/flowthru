using Flowthru.Flow;
using SpaceflightsNewTypes.Data;
using SpaceflightsNewTypes.Data._02_Intermediate.Schemas;
using SpaceflightsNewTypes.Data._07_ModelOutput.Schemas;
using SpaceflightsNewTypes.Data._08_Reporting.Schemas;
using SpaceflightsNewTypes.Flows.Reporting.Steps;
using Plotly.NET;

namespace SpaceflightsNewTypes.Flows.Reporting;

/// <summary>
/// Reporting pipeline that generates visualizations from processed data.
/// </summary>
public static class ReportingFlow
{
  public static BuiltFlow Create(Catalog catalog, FlowConfig config)
  {
    var confusionOptions = config.ConfusionMatrixOptions;
    var confusionTransform = CreateConfusionMatrixStep.Create();

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

      // NOTE: PNG export commented out due to Plotly.NET PuppeteerSharp performance issues.
      // pipeline.AddStep<GenericChart, byte[]>(
      //   label: "ExportPassengerCapacityPng",
      //   transform: PlotlyImageExportStep.Create(),
      //   input1: catalog.ShuttlePassengerCapacityChart,
      //   output1: catalog.ShuttlePassengerCapacityPlotPng
      // );

      pipeline.AddStep<IEnumerable<ModelPredictions>, GenericChart>(
        label: "GenerateConfusionMatrixChart",
        transform: predictions => confusionTransform((predictions, confusionOptions)),
        input1: catalog.ModelPredictions,
        output1: catalog.ConfusionMatrixChart
      );

      // NOTE: PNG export commented out due to Plotly.NET PuppeteerSharp performance issues.
      // pipeline.AddStep<GenericChart, byte[]>(
      //   label: "ExportConfusionMatrixPng",
      //   transform: PlotlyImageExportStep.Create(),
      //   input1: catalog.ConfusionMatrixChart,
      //   output1: catalog.ConfusionMatrixPlotPng
      // );
    });
  }
}
