using Flowthru.Application;
using KedroSpaceflights.Custom.Data;
using KedroSpaceflights.Custom.Pipelines.DataDiagnostics;
using KedroSpaceflights.Custom.Pipelines.DataEvaluation;
using KedroSpaceflights.Custom.Pipelines.DataProcessing;
using KedroSpaceflights.Custom.Pipelines.DataScience;
using KedroSpaceflights.Custom.Pipelines.Reporting;

namespace KedroSpaceflights.Custom;

/// <summary>
/// Entry point for the Spaceflights FlowThru example.
/// Demonstrates a hybrid configuration approach:
/// - Infrastructure (catalog, metadata, logging) configured in appsettings.json
/// - Pipeline registration in code for compile-time safety
/// - Pipeline parameters loaded from appsettings.json for easy tuning
/// </summary>
public class Program
{
  public static async Task<int> Main(string[] args)
  {
    var app = FlowthruApplication.Create(
      args,
      builder =>
      {
        // Enable configuration loading from appsettings.json files
        // This loads: appsettings.json (base) -> appsettings.{Environment}.json -> appsettings.Local.json
        builder.UseConfiguration();

        builder
          .RegisterPipeline<SpaceflightsCatalog>(
            label: "DataProcessing",
            pipeline: DataProcessingPipeline.Create
          )
          .WithDescription("Preprocesses raw data and creates model input table");

        builder
          .RegisterPipelineWithConfiguration<SpaceflightsCatalog, DataSciencePipeline.Params>(
            label: "DataScience",
            pipeline: DataSciencePipeline.Create,
            configurationSection: "Flowthru:Pipelines:DataScience"
          )
          .WithDescription("Trains ML model");

        builder
          .RegisterPipeline<SpaceflightsCatalog>(
            label: "DataDiagnostics",
            pipeline: DataDiagnosticsPipeline.Create
          )
          .WithDescription(
            "Validates pipeline outputs against Kedro reference and exports diagnostic data"
          );

        builder
          .RegisterPipelineWithConfiguration<SpaceflightsCatalog, DataEvaluationPipeline.Params>(
            label: "DataEvaluation",
            pipeline: DataEvaluationPipeline.Create,
            configurationSection: "Flowthru:Pipelines:DataEvaluation"
          )
          .WithDescription("Evaluates ML model performance and cross-validation");

        builder
          .RegisterPipeline<SpaceflightsCatalog>(
            label: "Reporting",
            pipeline: ReportingPipeline.Create
          )
          .WithDescription("Generates reports and visualizations");
      }
    );

    return await app.RunAsync();
  }
}
