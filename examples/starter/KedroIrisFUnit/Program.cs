using Flowthru.Core.Cli;
using Flowthru.Core.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using KedroIrisFUnit.Data;
using KedroIrisFUnit.Flows.DataEngineering;
using KedroIrisFUnit.Flows.DataScience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroIrisFUnit;

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
      services => ConfigureServices(services, basePath: Directory.GetCurrentDirectory())
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
    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();

    services.AddFlowthru(
      configuration,
      flowthru =>
      {
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

        // Register data engineering pipeline with configuration parameters
        flowthru
          .RegisterFlow(
            label: "DataEngineering",
            flow: DataEngineeringFlow.Create,
            configurationSection: "Flowthru:Flows:DataEngineering"
          )
          .WithDescription("Splits iris data into training and test sets with one-hot encoding");

        // Register data science pipeline with configuration parameters
        flowthru
          .RegisterFlow(
            label: "DataScience",
            flow: DataScienceFlow.Create,
            configurationSection: "Flowthru:Flows:DataScience"
          )
          .WithDescription("Trains multi-class logistic regression model for iris classification");
      }
    );

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
