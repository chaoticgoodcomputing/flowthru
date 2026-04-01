using Flowthru.Cli;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Flowthru.Services;
using KedroSpaceflights.Data;
using KedroSpaceflights.Pipelines.DataProcessing;
using KedroSpaceflights.Pipelines.DataScience;
using KedroSpaceflights.Pipelines.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroSpaceflights;

/// <summary>
/// Main application entry point for the Spaceflights price prediction pipeline.
/// </summary>
public class Program
{
  /// <summary>
  /// Main entry point for the Spaceflights pipeline CLI application.
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
      flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);
      flowthru.RegisterCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));

      // Output pipeline metadata
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

      // Register data processing pipeline
      flowthru
        .RegisterPipeline(label: "DataProcessing", pipeline: DataProcessingPipeline.Create)
        .WithDescription("Preprocesses companies and shuttles data");

      // Register data science pipeline with configuration parameters
      flowthru
        .RegisterPipeline(
          label: "DataScience",
          pipeline: DataSciencePipeline.Create,
          configurationSection: "Flowthru:Pipelines:DataScience"
        )
        .WithDescription("Trains linear regression model for price prediction");

      // Register reporting pipeline with configuration parameters
      flowthru
        .RegisterPipeline(
          label: "Reporting",
          pipeline: ReportingPipeline.Create,
          configurationSection: "Flowthru:Pipelines:Reporting"
        )
        .WithDescription("Generates passenger capacity reports and visualizations");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
