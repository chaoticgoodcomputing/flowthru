using Flowthru.Cli;
using Flowthru.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpaceflightsEFCore.Data;
using SpaceflightsEFCore.Flows.DataProcessing;
using SpaceflightsEFCore.Flows.DataScience;
using SpaceflightsEFCore.Flows.Reporting;

namespace SpaceflightsEFCore;

/// <summary>
/// Main application entry point for the Spaceflights price prediction pipeline.
/// Demonstrates EFCore integration using a SQLite database for intermediate pipeline state.
/// </summary>
public class Program
{
  /// <summary>
  /// Main entry point for the Spaceflights pipeline CLI application.
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
    var dbPath = Path.Combine(basePath, "Data", "spaceflights.db");

    // Register EFCore DbContextFactory with SQLite.
    // IDbContextFactory produces a fresh DbContext per Load/Save operation, which is the
    // idiomatic pattern for concurrent pipeline execution.
    services.AddDbContextFactory<SpaceflightsDbContext>(options =>
      options.UseSqlite($"Data Source={dbPath}")
    );

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);
      flowthru.RegisterCatalog(sp => new Catalog(
        basePath: Path.Combine(basePath, "Data"),
        contextFactory: sp.GetRequiredService<IDbContextFactory<SpaceflightsDbContext>>()
      ));

      // Register data processing pipeline
      flowthru
        .RegisterFlow(label: "DataProcessing", flow: DataProcessingFlow.Create)
        .WithDescription("Preprocesses companies and shuttles data");

      // Register data science pipeline with configuration parameters
      flowthru
        .RegisterFlow(
          label: "DataScience",
          flow: DataScienceFlow.Create,
          configurationSection: "Flowthru:Flows:DataScience"
        )
        .WithDescription("Trains linear regression model for price prediction");

      // Register reporting pipeline with configuration parameters
      flowthru
        .RegisterFlow(
          label: "Reporting",
          flow: ReportingFlow.Create,
          configurationSection: "Flowthru:Flows:Reporting"
        )
        .WithDescription("Generates passenger capacity reports and visualizations");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });
  }
}
