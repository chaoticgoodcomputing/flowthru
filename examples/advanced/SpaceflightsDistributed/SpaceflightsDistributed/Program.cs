using Flowthru.Cli;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpaceflightsDistributed.DataProcessing.Data;
using SpaceflightsDistributed.DataProcessing.Flows.DataProcessing;
using SpaceflightsDistributed.DataScience.Data;
using SpaceflightsDistributed.DataScience.Flows.DataScience;
using SpaceflightsDistributed.Reporting.Data;
using SpaceflightsDistributed.Reporting.Flows.Reporting;

namespace SpaceflightsDistributed;

/// <summary>
/// Entry point for the SpaceflightsDistributed pipeline.
/// </summary>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, Directory.GetCurrentDirectory())
    );

  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var services = new ServiceCollection();
    ConfigureServices(services, basePath ?? Directory.GetCurrentDirectory());
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
    var dataPath = Path.Combine(basePath, "Data");

    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddFlowthru(flowthru =>
    {
      // UseConfiguration registers the IConfiguration so each subproject's
      // Catalog ConfigurationItem<T> bindings can resolve their sections.
      // Option records are exposed on the catalogs as ordinary inputs —
      // flow factories no longer take separate FlowConfig parameters
      // (Phase 5/8 of the smart-caching RFC).
      flowthru.UseConfiguration(configuration);
      flowthru.RegisterCatalog(_ => new DataProcessingCatalog(dataPath));
      flowthru.RegisterCatalog(sp => new DataScienceCatalog(
        dataPath,
        sp.GetRequiredService<IConfiguration>()
      ));
      flowthru.RegisterCatalog(sp => new ReportingCatalog(
        dataPath,
        sp.GetRequiredService<IConfiguration>()
      ));

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
      });

      flowthru
        .RegisterFlow<DataProcessingCatalog>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Preprocesses companies and shuttles data into a model input table");

      flowthru
        .RegisterFlow<DataProcessingCatalog, DataScienceCatalog>(
          "DataScience", DataScienceFlow.Create)
        .WithDescription("Trains linear regression model for shuttle price prediction");

      flowthru
        .RegisterFlow<DataProcessingCatalog, DataScienceCatalog, ReportingCatalog>(
          "Reporting", ReportingFlow.Create)
        .WithDescription("Generates passenger capacity reports and confusion matrix visualizations");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
