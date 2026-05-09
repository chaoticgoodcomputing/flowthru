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
  public static async Task<int> Main(string[] args)
  {
    await using var pg = new PostgreSqlBuilder()
      .WithImage("postgres:17")
      .WithDatabase("spaceflights")
      .WithUsername("flowthru")
      .WithPassword("flowthru")
      .Build();

    Console.WriteLine("→ Starting PostgreSQL container...");
    await pg.StartAsync();
    var connectionString = pg.GetConnectionString();
    Console.WriteLine(
      $"  ✓ PostgreSQL ready at {pg.Hostname}:{pg.GetMappedPublicPort(5432)}"
    );

    try
    {
      return await FlowthruCli.RunStandaloneAsync(
        args,
        services =>
          ConfigureServices(services, Directory.GetCurrentDirectory(), connectionString)
      );
    }
    finally
    {
      Console.WriteLine("→ Stopping PostgreSQL container...");
      await pg.DisposeAsync();
    }
  }

  private static void ConfigureServices(
    IServiceCollection services,
    string basePath,
    string connectionString
  )
  {
    var dataPath = Path.Combine(basePath, "Data");

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
      flowthru.RegisterCatalog(_ => new RawCatalog(dataPath));
      flowthru.RegisterCatalog(sp => new StagingCatalog(
        contextFactory: sp.GetRequiredService<IDbContextFactory<StagingDbContext>>()
      ));
      flowthru.RegisterCatalog(sp => new ProductionCatalog(
        contextFactory: sp.GetRequiredService<IDbContextFactory<ProductionDbContext>>(),
        basePath: dataPath
      ));
      flowthru.RegisterCatalog(sp => new FlowConfig(sp.GetRequiredService<IConfiguration>()));

      flowthru.VerifyEFCoreConnection<ProductionDbContext>();

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
      });

      flowthru
        .RegisterFlow<RawCatalog, StagingCatalog, FlowConfig>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Reads raw inputs and writes preprocessed/joined data to the ephemeral staging schema.");

      flowthru
        .RegisterFlow<StagingCatalog, ProductionCatalog>("Promotion", PromotionFlow.Create)
        .WithDescription("Promotes staging tables into the production schema via fused INSERT-FROM-SELECT.");

      flowthru
        .RegisterFlow<ProductionCatalog, FlowConfig>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains and evaluates a regression model from production data.");

      flowthru
        .RegisterFlow<ProductionCatalog, FlowConfig>("Reporting", ReportingFlow.Create)
        .WithDescription("Generates capacity reports and a confusion matrix from production data.");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
