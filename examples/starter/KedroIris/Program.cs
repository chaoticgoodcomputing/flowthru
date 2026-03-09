using Flowthru.Cli;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Flowthru.Services;
using KedroIris.Data;
using KedroIris.Pipelines.DataEngineering;
using KedroIris.Pipelines.DataScience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroIris;

/// <summary>
/// Main application entry point for the Iris classification pipeline.
/// </summary>
public class Program
{
  /// <summary>
  /// Main entry point for the Iris classification pipeline CLI application.
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

      // Register data engineering pipeline with configuration parameters
      flowthru
        .RegisterPipelineWithConfiguration<Catalog, DataEngineeringPipeline.Params>(
          label: "DataEngineering",
          pipeline: DataEngineeringPipeline.Create,
          configurationSection: "Flowthru:Pipelines:DataEngineering"
        )
        .WithDescription("Splits iris data into training and test sets with one-hot encoding");

      // Register data science pipeline with configuration parameters
      flowthru
        .RegisterPipelineWithConfiguration<Catalog, DataSciencePipeline.Params>(
          label: "DataScience",
          pipeline: DataSciencePipeline.Create,
          configurationSection: "Flowthru:Pipelines:DataScience"
        )
        .WithDescription("Trains multi-class logistic regression model for iris classification");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
