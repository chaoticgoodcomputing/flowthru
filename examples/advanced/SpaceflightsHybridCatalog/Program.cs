using Flowthru.Cli;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpaceflightsHybridCatalog.Data;
using SpaceflightsHybridCatalog.Flows.DataProcessing;
using SpaceflightsHybridCatalog.Flows.DataScience;
using SpaceflightsHybridCatalog.Flows.Reporting;

namespace SpaceflightsHybridCatalog;

/// <summary>
/// Spaceflights pipeline that swaps its data backend based on
/// <c>ASPNETCORE_ENVIRONMENT</c>. In <c>Development</c> the pipeline reads and
/// writes flat files (CSV / Parquet / JSON) under <c>Data/</c>; in
/// <c>Production</c> it persists intermediate, primary, and model state to a
/// SQLite database via EFCore. The same <see cref="Catalog"/> abstraction is
/// resolved from DI either way, so flows and steps are completely unaware of
/// which backend is in play.
/// </summary>
/// <remarks>
/// The <c>ASPNETCORE_ENVIRONMENT</c> convention is borrowed from ASP.NET Core
/// hosting so downstream users who embed <c>FlowthruService</c> alongside an
/// API host see one consistent switch driving both stacks.
/// </remarks>
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
    var environment =
      Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    var isProduction = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);

    var dataPath = Path.Combine(basePath, "Data");

    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    // The DbContext factory is only meaningful in Production — but registering
    // it unconditionally keeps DI symmetric, and the connection isn't opened
    // until something resolves it. In Development the factory is registered
    // but never touched.
    var dbPath = Path.Combine(dataPath, "spaceflights.db");
    services.AddDbContextFactory<SpaceflightsDbContext>(options =>
      options.UseSqlite($"Data Source={dbPath}")
    );

    // In Production, ensure the SQLite file + schema exist before pre-flight
    // hooks run. EnsureCreated() is intentionally a one-shot startup concern,
    // not a catalog-construction side-effect — a real deployment would use
    // EFCore migrations instead.
    if (isProduction)
    {
      Directory.CreateDirectory(dataPath);
      var optionsBuilder = new DbContextOptionsBuilder<SpaceflightsDbContext>()
        .UseSqlite($"Data Source={dbPath}");
      using var ctx = new SpaceflightsDbContext(optionsBuilder.Options);
      ctx.Database.EnsureCreated();
    }

    services.AddFlowthru(flowthru =>
    {
      // ── The DI swap ─────────────────────────────────────────────────────
      // RegisterCatalog<Catalog>(...) tells the framework "resolve type
      // `Catalog` from DI before invoking a flow factory." The factory we
      // supply here returns either subclass depending on environment, but
      // both are typed as the abstract base so flow signatures are stable.
      flowthru.RegisterCatalog<Catalog>(sp => isProduction
        ? new ProductionCatalog(
            basePath: dataPath,
            contextFactory: sp.GetRequiredService<IDbContextFactory<SpaceflightsDbContext>>())
        : new DevelopmentCatalog(basePath: dataPath)
      );

      flowthru.RegisterCatalog(sp => new FlowConfig(sp.GetRequiredService<IConfiguration>()));

      // Production-only pre-flight: verify the SQLite connection and EF
      // model shape before any flow runs. In Development the hook is
      // skipped because the EFCore items are never dereferenced.
      if (isProduction)
      {
        flowthru.VerifyEFCoreConnection<SpaceflightsDbContext>();
        flowthru.VerifyEFCoreConfiguration<SpaceflightsDbContext>();
      }

      flowthru
        .RegisterFlow<Catalog>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Preprocesses raw company / shuttle data and joins it with reviews.");

      flowthru
        .RegisterFlow<Catalog, FlowConfig>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains and evaluates a linear regression price model.");

      flowthru
        .RegisterFlow<Catalog, FlowConfig>("Reporting", ReportingFlow.Create)
        .WithDescription("Generates the passenger-capacity report and confusion-matrix chart.");

      flowthru.ConfigureMetadata(meta =>
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
