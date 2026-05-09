using Flowthru.Cli;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using Flowthru.Step.Python;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpaceflightsPythonEFCore.Data;
using SpaceflightsPythonEFCore.Flows.DataProcessing;
using SpaceflightsPythonEFCore.Flows.DataScience;
using SpaceflightsPythonEFCore.Flows.Reporting;

namespace SpaceflightsPythonEFCore;

/// <summary>
/// Entry point for the SpaceflightsPythonEFCore advanced example.
/// </summary>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services =>
        ConfigureServices(
          services,
          Directory.GetCurrentDirectory(),
          AppDomain.CurrentDomain.BaseDirectory
        )
    );

  public static IServiceProvider ConfigureServices(
    string? basePath = null,
    string? outputPath = null
  )
  {
    var services = new ServiceCollection();
    ConfigureServices(
      services,
      basePath ?? Directory.GetCurrentDirectory(),
      outputPath ?? AppDomain.CurrentDomain.BaseDirectory
    );
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(
    IServiceCollection services,
    string basePath,
    string outputPath
  )
  {
    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });

    var dbPath = Path.Combine(basePath, "Data", "spaceflights.db");

    services.AddDbContextFactory<SpaceflightsDbContext>(options =>
      options.UseSqlite($"Data Source={dbPath}")
    );

    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog(sp => new Catalog(
        basePath: Path.Combine(basePath, "Data"),
        contextFactory: sp.GetRequiredService<IDbContextFactory<SpaceflightsDbContext>>()
      ));

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
      });

      flowthru.UsePython(python =>
      {
        python.ModuleSearchPaths.Add(basePath);
        python.ModuleSearchPaths.Add(outputPath);
        python.VenvPath = outputPath;
      });

      flowthru
        .RegisterFlow<Catalog>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Preprocesses companies and shuttles (C#), stores in EFCore");

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains and evaluates regression model (Python); reads/writes EFCore");

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>("Reporting", ReportingFlow.Create)
        .WithDescription("Generates visualizations (Python); reads PreprocessedShuttles and ModelPredictions from EFCore");
    });
  }
}
