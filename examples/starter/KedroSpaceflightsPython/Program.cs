using Flowthru.Caching;
using Flowthru.Cli;
using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using Flowthru.Step.Python;
using KedroSpaceflightsPython.Data;
using KedroSpaceflightsPython.Flows.DataProcessing;
using KedroSpaceflightsPython.Flows.DataScience;
using KedroSpaceflightsPython.Flows.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroSpaceflightsPython;

/// <summary>
/// Main application entry point for the Spaceflights pipeline with Python nodes.
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

    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseConfiguration(configuration);
      flowthru.RegisterCatalog(sp => new Catalog(
        Path.Combine(basePath, "Data"),
        sp.GetRequiredService<IConfiguration>()));

      // Persist the cache manifest under the project root so successive
      // runs from any working directory share the same state. The
      // split_data Python step opts into caching via @step(cacheable=True)
      // and the framework auto-derives its CodeVersion from the .py
      // source + uv.lock + interpreter version.
      flowthru.UseCacheStorage(_ =>
        Item.Of<CacheManifest>("flowthru.cache")
          .Json()
          .AtPath(Path.Combine(basePath, ".flowthru", "cache.json"))
          .Build());

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
        .RegisterFlow<Catalog, IPythonExecutor>("DataProcessing", DataProcessingFlow.Create)
        .WithDescription("Preprocesses companies, shuttles, and reviews data using Python");

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>("DataScience", DataScienceFlow.Create)
        .WithDescription("Trains linear regression model for price prediction using Python/scikit-learn");

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>("Reporting", ReportingFlow.Create)
        .WithDescription("Generates visualization outputs including passenger capacity plots and confusion matrix");
    });
  }
}
