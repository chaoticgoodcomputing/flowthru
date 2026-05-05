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
using Testcontainers.PostgreSql;

namespace SpaceflightsStagingSchema;

/// <summary>
/// Spaceflights pipeline with an ephemeral PostgreSQL <c>staging</c> schema
/// promoted into the durable <c>public</c> schema. PostgreSQL is brought up
/// via Testcontainers for the duration of the run.
/// </summary>
/// <remarks>
/// <para>
/// Both schemas live in a single PostgreSQL database, sharing one connection
/// string. This is the architectural unlock: items in
/// <see cref="StagingCatalog"/> and <see cref="ProductionCatalog"/> share a
/// <c>DbScope</c>, so cross-schema promotion takes the framework's fused
/// <c>INSERT-FROM-SELECT</c> path — no rows materialize in C#.
/// </para>
/// <para>
/// Requires Docker on the host. The container is created on
/// <see cref="Main"/> entry and disposed on exit.
/// </para>
/// </remarks>
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

    // Both contexts target the same connection. Their schema separation is
    // declared at model build time via HasDefaultSchema in OnModelCreating.
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

    services.AddFlowthru(
      configuration,
      flowthru =>
      {
        flowthru.RegisterCatalog(_ => new RawCatalog(dataPath));
        flowthru.RegisterCatalog(sp => new StagingCatalog(
          contextFactory: sp.GetRequiredService<IDbContextFactory<StagingDbContext>>()
        ));
        flowthru.RegisterCatalog(sp => new ProductionCatalog(
          contextFactory: sp.GetRequiredService<IDbContextFactory<ProductionDbContext>>(),
          basePath: dataPath
        ));
        flowthru.RegisterCatalog(_ => new FlowConfig(configuration));

        flowthru
          .RegisterFlow(label: "DataProcessing", flow: DataProcessingFlow.Create)
          .WithDescription(
            "Reads raw inputs and writes preprocessed/joined data to the ephemeral staging schema."
          );

        flowthru
          .RegisterFlow(label: "Promotion", flow: PromotionFlow.Create)
          .WithDescription(
            "Promotes staging tables into the production schema via fused INSERT-FROM-SELECT."
          );

        flowthru
          .RegisterFlow(label: "DataScience", flow: DataScienceFlow.Create)
          .WithDescription(
            "Trains and evaluates a regression model from production data."
          );

        flowthru
          .RegisterFlow(label: "Reporting", flow: ReportingFlow.Create)
          .WithDescription(
            "Generates capacity reports and a confusion matrix from production data."
          );

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
