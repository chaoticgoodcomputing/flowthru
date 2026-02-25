using Flowthru.Cli;
using KedroSpaceflights.Custom.Data;
using KedroSpaceflights.Custom.Pipelines.DataDiagnostics;
using KedroSpaceflights.Custom.Pipelines.DataEvaluation;
using KedroSpaceflights.Custom.Pipelines.DataProcessing;
using KedroSpaceflights.Custom.Pipelines.DataScience;
using KedroSpaceflights.Custom.Pipelines.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
  /// <summary>
  /// Configures Flowthru services for dependency injection.
  /// </summary>
  /// <returns>Configured service provider</returns>
  public static IServiceProvider ConfigureServices()
  {
    var services = new ServiceCollection();

    services.AddFlowthru(flowthru =>
    {
      // Enable configuration loading from appsettings.json files
      // This loads: appsettings.json (base) -> appsettings.{Environment}.json -> appsettings.Local.json
      flowthru.UseConfiguration();
      flowthru.UseCatalog(_ => new Catalog("Data"));

      flowthru
        .RegisterPipeline<Catalog>(label: "DataProcessing", pipeline: DataProcessingPipeline.Create)
        .WithDescription("Preprocesses raw data and creates model input table");

      flowthru
        .RegisterPipelineWithConfiguration<Catalog, DataSciencePipeline.Params>(
          label: "DataScience",
          pipeline: DataSciencePipeline.Create,
          configurationSection: "Flowthru:Pipelines:DataScience"
        )
        .WithDescription("Trains ML model");

      flowthru
        .RegisterPipeline<Catalog>(
          label: "DataDiagnostics",
          pipeline: DataDiagnosticsPipeline.Create
        )
        .WithDescription(
          "Validates pipeline outputs against Kedro reference and exports diagnostic data"
        );

      flowthru
        .RegisterPipelineWithConfiguration<Catalog, DataEvaluationPipeline.Params>(
          label: "DataEvaluation",
          pipeline: DataEvaluationPipeline.Create,
          configurationSection: "Flowthru:Pipelines:DataEvaluation"
        )
        .WithDescription("Evaluates ML model performance and cross-validation");

      flowthru
        .RegisterPipeline<Catalog>(label: "Reporting", pipeline: ReportingPipeline.Create)
        .WithDescription("Generates reports and visualizations");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });

    return services.BuildServiceProvider();
  }

  /// <summary>
  /// Main entry point for the application. Builds the service provider and runs the CLI.
  /// </summary>
  /// <param name="args">Command-line arguments.</param>
  public static async Task<int> Main(string[] args)
  {
    var services = ConfigureServices();
    var cli = services.GetRequiredService<Flowthru.Cli.FlowthruCli>();
    return await cli.RunAsync(args);
  }
}
