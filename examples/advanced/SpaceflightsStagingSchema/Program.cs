using Flowthru.Cli;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpaceflightsStagingSchema.Data;
using SpaceflightsStagingSchema.Flows.DataProcessing;
using SpaceflightsStagingSchema.Flows.DataScience;
using SpaceflightsStagingSchema.Flows.Promotion;
using SpaceflightsStagingSchema.Flows.Reporting;
using Testcontainers.PostgreSql;

namespace SpaceflightsStagingSchema;

/// <summary>
/// Spaceflights pipeline with an ephemeral PostgreSQL <c>staging</c> schema
/// promoted into the durable <c>public</c> schema. PostgreSQL is brought up
/// via Testcontainers for the duration of the run.
/// </summary>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, Directory.GetCurrentDirectory())
    );

  /// <summary>
  /// Harness-conformant entry point. Boots a PostgreSQL Testcontainer
  /// synchronously (the only async work needed before flow execution),
  /// registers it as an <see cref="IAsyncDisposable"/> factory-singleton
  /// so the returned <see cref="IServiceProvider"/>'s disposal tears it
  /// down, and wires up Flowthru against its connection string.
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

    // Boot the PostgreSQL container synchronously and register it as a
    // factory-singleton so DI disposes it when the provider is torn down.
    // The blocking ~2s start is the price of the harness's sync contract.
    var pg = new PostgreSqlBuilder()
      .WithImage("postgres:17")
      .WithDatabase("spaceflights")
      .WithUsername("flowthru")
      .WithPassword("flowthru")
      .Build();
    Console.WriteLine("→ Starting PostgreSQL container...");
    pg.StartAsync().GetAwaiter().GetResult();
    var connectionString = pg.GetConnectionString();
    Console.WriteLine(
      $"  ✓ PostgreSQL ready at {pg.Hostname}:{pg.GetMappedPublicPort(5432)}"
    );
    services.AddSingleton<PostgreSqlContainer>(_ => pg);

    services.AddDbContextFactory<StagingDbContext>(options =>
      options.UseNpgsql(connectionString)
    );
    services.AddDbContextFactory<ProductionDbContext>(options =>
      options.UseNpgsql(connectionString)
    );

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
      // factories no longer take a separate FlowConfig parameter
      // (Phase 5/8 of the smart-caching RFC).
      flowthru.UseConfiguration(configuration);
      flowthru.RegisterCatalog(sp => new RawCatalog(
        dataPath,
        sp.GetRequiredService<IConfiguration>()
      ));
      flowthru.RegisterCatalog(sp => new StagingCatalog(
        contextFactory: sp.GetRequiredService<IDbContextFactory<StagingDbContext>>()
      ));
      flowthru.RegisterCatalog(sp => new ProductionCatalog(
        contextFactory: sp.GetRequiredService<IDbContextFactory<ProductionDbContext>>(),
        basePath: dataPath,
        configuration: sp.GetRequiredService<IConfiguration>()
      ));

      flowthru.VerifyEFCoreConnection<ProductionDbContext>();

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
      });

      flowthru
        .RegisterFlow<RawCatalog, StagingCatalog>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Reads raw inputs and writes preprocessed/joined data to the ephemeral staging schema.");

      flowthru
        .RegisterFlow<StagingCatalog, ProductionCatalog>("Promotion", PromotionFlow.Create)
        .WithDescription("Promotes staging tables into the production schema via fused INSERT-FROM-SELECT.");

      flowthru
        .RegisterFlow<ProductionCatalog>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains and evaluates a regression model from production data.");

      flowthru
        .RegisterFlow<ProductionCatalog>("Reporting", ReportingFlow.Create)
        .WithDescription("Generates capacity reports and a confusion matrix from production data.");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
