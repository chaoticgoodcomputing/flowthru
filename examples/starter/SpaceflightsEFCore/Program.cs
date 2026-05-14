using Flowthru.Cli;
using Flowthru.Diagnostics;
using Flowthru.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpaceflightsEFCore.Data;
using SpaceflightsEFCore.Flows.DataProcessing;
using SpaceflightsEFCore.Flows.DataScience;
using SpaceflightsEFCore.Flows.Reporting;

namespace SpaceflightsEFCore;

/// <summary>
/// Main application entry point for the Spaceflights price prediction pipeline.
/// Demonstrates EFCore integration using a SQLite database for intermediate
/// pipeline state, with metadata emission via the JSON + Mermaid extensions.
/// </summary>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, Directory.GetCurrentDirectory())
    );

  /// <summary>Build a service provider for tests / external host adapters.</summary>
  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var services = new ServiceCollection();
    ConfigureServices(services, basePath ?? Directory.GetCurrentDirectory());
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
    var dataPath = Path.Combine(basePath, "Data");
    var dbPath = Path.Combine(dataPath, "spaceflights.db");

    // IDbContextFactory produces a fresh DbContext per Load/Save operation —
    // the idiomatic pattern for concurrent pipeline execution.
    services.AddDbContextFactory<SpaceflightsDbContext>(options =>
      options.UseSqlite($"Data Source={dbPath}")
    );

    // Ensure the SQLite file + schema exist before AddFlowthru registers
    // the VerifyEFCoreConnection pre-flight hook. The hook probes the file
    // via File.Exists, so creation has to be a host-level concern that runs
    // before pre-flight — not a catalog-construction side-effect (catalogs
    // are resolved after pre-flight). A real deployment would replace this
    // with EF Core migrations.
    Directory.CreateDirectory(dataPath);
    using (var ctx = new SpaceflightsDbContext(
      new DbContextOptionsBuilder<SpaceflightsDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options))
    {
      ctx.Database.EnsureCreated();
    }

    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddFlowthru(b =>
    {
      // UseConfiguration registers the IConfiguration so the Catalog's
      // ConfigurationItem<T> bindings can resolve their sections. Option
      // records are exposed on the catalog as ordinary inputs — flow
      // factories no longer take a second FlowConfig parameter
      // (Phase 5/8 of the smart-caching RFC).
      b.UseConfiguration(configuration);
      b.RegisterCatalog(sp => new Catalog(
        basePath: Path.Combine(basePath, "Data"),
        contextFactory: sp.GetRequiredService<IDbContextFactory<SpaceflightsDbContext>>(),
        configuration: sp.GetRequiredService<IConfiguration>()
      ));

      b.RegisterFlow<Catalog>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Preprocesses companies and shuttles data");

      b.RegisterFlow<Catalog>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains linear regression model for price prediction");

      b.RegisterFlow<Catalog>("Reporting", ReportingFlow.Create)
        .WithDescription("Generates passenger capacity reports and visualizations");

      // Pre-flight registration hooks — catch host misconfiguration at
      // startup rather than at first flow run.
      b.VerifyEFCoreConnection<SpaceflightsDbContext>();
      b.VerifyEFCoreConfiguration<SpaceflightsDbContext>();

      b.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta
          .AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath))
          .AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
      });
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
