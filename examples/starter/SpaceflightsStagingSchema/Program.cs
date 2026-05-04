using Flowthru.Core.Cli;
using Flowthru.Core.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpaceflightsStagingSchema.Data;
using SpaceflightsStagingSchema.Flows.DataProcessing;
using SpaceflightsStagingSchema.Flows.DataScience;
using SpaceflightsStagingSchema.Flows.Promotion;
using SpaceflightsStagingSchema.Flows.Reporting;

namespace SpaceflightsStagingSchema;

/// <summary>
/// Spaceflights pipeline with an ephemeral staging database and explicit
/// promotion to production. The staging database is provisioned in pre-flight
/// via <see cref="StagingCatalog"/>'s <c>FlowResource&lt;DbScope&gt;</c>
/// declaration and torn down on flow completion.
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
    var stagingDbPath = Path.Combine(dataPath, "staging.db");
    var productionDbPath = Path.Combine(dataPath, "production.db");

    // Two DbContextFactories: one for the ephemeral staging database, one for persistent production.
    services.AddDbContextFactory<StagingDbContext>(options =>
      options.UseSqlite($"Data Source={stagingDbPath}")
    );
    services.AddDbContextFactory<ProductionDbContext>(options =>
      options.UseSqlite($"Data Source={productionDbPath}")
    );

    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();

    services.AddFlowthru(
      configuration,
      flowthru =>
      {
        flowthru.RegisterCatalog(_ => new RawCatalog(dataPath));
        flowthru.RegisterCatalog(sp => new StagingCatalog(
          basePath: dataPath,
          contextFactory: sp.GetRequiredService<IDbContextFactory<StagingDbContext>>()
        ));
        flowthru.RegisterCatalog(sp => new ProductionCatalog(
          contextFactory: sp.GetRequiredService<IDbContextFactory<ProductionDbContext>>()
        ));
        flowthru.RegisterCatalog(_ => new FlowConfig(configuration));

        flowthru
          .RegisterFlow(label: "DataProcessing", flow: DataProcessingFlow.Create)
          .WithDescription("Reads raw inputs and writes preprocessed/joined data to the ephemeral staging database.");

        flowthru
          .RegisterFlow(label: "Promotion", flow: PromotionFlow.Create)
          .WithDescription("Promotes the staging model input table into production.");

        flowthru
          .RegisterFlow(label: "DataScience", flow: DataScienceFlow.Create)
          .WithDescription("Trains and evaluates a regression model from production data.");

        flowthru
          .RegisterFlow(label: "Reporting", flow: ReportingFlow.Create)
          .WithDescription("Generates capacity reports and a confusion matrix from production data.");

        flowthru.ConfigureMetadata(meta =>
        {
          var metadataPath = Path.Combine(basePath, "Metadata");
          meta
            .AddProvider<JsonMetadataProvider, JsonMetadataProviderBuilder>(json =>
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
