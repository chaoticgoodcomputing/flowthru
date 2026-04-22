using Flowthru.Core.Cli;
using Flowthru.Core.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
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
///
/// This example demonstrates Flowthru's multi-catalog pipeline registration API.
/// Three independently-versioned library projects each define their own catalog
/// and the pipelines between them are expressed via multi-catalog RegisterFlow
/// overloads — making cross-domain data dependencies part of the type signature, not
/// a runtime convention.
///
///   DataProcessingFlow.Create(DataProcessingCatalog dc)
///   DataScienceFlow.Create(DataProcessingCatalog dp, DataScienceCatalog ds, Params p)
///   ReportingFlow.Create(DataProcessingCatalog dp, DataScienceCatalog ds, ReportingCatalog r)
/// </summary>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, Directory.GetCurrentDirectory())
    );

  /// <summary>
  /// Configures services for the application.
  /// </summary>
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

    services.AddFlowthru(
      configuration,
      flowthru =>
      {
        // ─── Catalog Registration ──────────────────────────────────────────────
        // Each library owns its own catalog.

        flowthru.RegisterCatalog(_ => new DataProcessingCatalog(dataPath));
        flowthru.RegisterCatalog(_ => new DataScienceCatalog(dataPath));
        flowthru.RegisterCatalog(_ => new ReportingCatalog(dataPath));

        // ─── Flow Registration ─────────────────────────────────────────────

        flowthru
          .RegisterFlow(label: "DataProcessing", flow: DataProcessingFlow.Create)
          .WithDescription("Preprocesses companies and shuttles data into a model input table");

        flowthru
          .RegisterFlow(
            label: "DataScience",
            flow: DataScienceFlow.Create,
            configurationSection: "Flowthru:Flows:DataScience"
          )
          .WithDescription("Trains linear regression model for shuttle price prediction");

        flowthru
          .RegisterFlow(
            label: "Reporting",
            flow: ReportingFlow.Create,
            configurationSection: "Flowthru:Flows:Reporting"
          )
          .WithDescription(
            "Generates passenger capacity reports and confusion matrix visualizations"
          );

        // ─── Metadata Providers ───────────────────────────────────────────────

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
      }
    );

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
