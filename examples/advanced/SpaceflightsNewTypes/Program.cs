using Flowthru.Cli;
using Flowthru.Hosting;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using SpaceflightsNewTypes.Data;
using SpaceflightsNewTypes.Flows.DataProcessing;
using SpaceflightsNewTypes.Flows.DataScience;
using SpaceflightsNewTypes.Flows.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SpaceflightsNewTypes;

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
    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));
      flowthru.RegisterCatalog(sp => new FlowConfig(sp.GetRequiredService<IConfiguration>()));

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt
          .WithOutputDirectory(metadataPath)
          .WithShowFullDag(false));
      });

      flowthru
        .RegisterFlow<Catalog>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Preprocesses companies and shuttles data");

      flowthru
        .RegisterFlow<Catalog, FlowConfig>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains linear regression model for price prediction");

      flowthru
        .RegisterFlow<Catalog, FlowConfig>("Reporting", ReportingFlow.Create)
        .WithDescription("Generates passenger capacity reports and visualizations");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
