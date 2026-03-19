using Flowthru.Cli;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Flowthru.Services;
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
  /// Main entry point for the custom Spaceflights pipeline CLI application.
  /// </summary>
  /// <param name="args">Command-line arguments</param>
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, Directory.GetCurrentDirectory())
    );

  /// <summary>
  /// Configures services for the application. Used by test infrastructure.
  /// </summary>
  /// <param name="basePath">Optional base path for data files (defaults to current directory)</param>
  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var services = new ServiceCollection();
    ConfigureServices(services, basePath ?? Directory.GetCurrentDirectory());
    return services.BuildServiceProvider();
  }

  /// <summary>
  /// Shared service configuration logic.
  /// </summary>
  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
    services.AddFlowthru(flowthru =>
    {
      // Enable configuration loading from appsettings.json files
      // This loads: appsettings.json (base) -> appsettings.{Environment}.json -> appsettings.Local.json
      flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);
      flowthru.UseCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));
      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddProvider<JsonMetadataProvider, JsonMetadataProviderBuilder>(json =>
            json.WithOutputDirectory(metadataPath)
          )
          .AddProvider<MermaidMetadataProvider, MermaidMetadataProviderBuilder>(mermaid =>
            mermaid.WithOutputDirectory(metadataPath)
          );
      });

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
  }
}
