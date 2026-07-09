using Flowthru.Cli;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using Flowthru.Step.DuckDb;
using SpaceflightsDuckDB.Data;
using SpaceflightsDuckDB.Flows.DataProcessing;
using SpaceflightsDuckDB.Flows.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SpaceflightsDuckDB;

/// <summary>
/// Main application entry point for the Spaceflights DuckDB pipeline.
/// </summary>
public class Program
{
  /// <summary>
  /// Main entry point for the Spaceflights DuckDB pipeline CLI application.
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
      flowthru.RegisterCatalog(sp => new Catalog(Path.Combine(basePath, "Data")));

      // UseDuckDb registers the embedded engine (tunable via the
      // Flowthru:DuckDb section of appsettings.json) and the pre-flight
      // check that binds every transform's SQL against its declared
      // Schemas before any step runs.
      flowthru.UseDuckDb();

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt
          .WithOutputDirectory(metadataPath)
          .WithShowFullDag(false));
      });

      flowthru
        .RegisterFlow<Catalog, IDuckDbEngine, ILogger>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Preprocesses raw data in C# and joins it into the model input table in SQL");

      flowthru
        .RegisterFlow<Catalog, IDuckDbEngine, ILogger>("Reporting", ReportingFlow.Create)
        .WithDescription("Aggregates per-company summaries in SQL and formats the top-rated report");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
