using Flowthru.Cli;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using KedroSpaceflightsFUnit.Data;
using KedroSpaceflightsFUnit.Flows.DataProcessing;
using KedroSpaceflightsFUnit.Flows.DataScience;
using KedroSpaceflightsFUnit.Flows.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroSpaceflightsFUnit;

/// <summary>
/// Main application entry point for the Spaceflights price prediction pipeline.
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
    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddFlowthru(flowthru =>
    {
      // UseConfiguration registers the IConfiguration so the Catalog's
      // ConfigurationItem<T> bindings can resolve their sections. Option
      // records are exposed on the catalog as ordinary inputs — flow
      // factories no longer take a second FlowConfig parameter
      // (Phase 5/8 of the smart-caching RFC).
      flowthru.UseConfiguration(configuration);
      flowthru.RegisterCatalog(sp => new Catalog(
        Path.Combine(basePath, "Data"),
        sp.GetRequiredService<IConfiguration>()));

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
      });

      flowthru
        .RegisterFlow<Catalog>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Preprocesses companies and shuttles data");

      flowthru
        .RegisterFlow<Catalog>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains linear regression model for price prediction");

      flowthru
        .RegisterFlow<Catalog>("Reporting", ReportingFlow.Create)
        .WithDescription("Generates passenger capacity reports and visualizations");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
