using Flowthru.Caching;
using Flowthru.Cli;
using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Hosting;
using Flowthru.Step.Python;
using FlowthruCoverage.Data;
using FlowthruCoverage.Flows.Coverage;
using FlowthruCoverage.Flows.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowthruCoverage;

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
      flowthru.RegisterCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));

      // Persist the cache manifest under the project root so successive
      // runs from any working directory share the same state. The
      // file is gitignored alongside Data/ and Metadata/. C# steps
      // attributed with [FlowthruStep] over CSV-backed catalog items
      // automatically participate; Python steps and inline-Func steps
      // remain uncacheable (see ReportingFlow comments for why).
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
        .RegisterFlow<Catalog>("Coverage", CoverageAnalysisFlow.Create)
        .WithDescription(
          "Aggregates staged Cobertura XML reports into a pivot-ready coverage heatmap CSV."
        );

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>("Reporting", ReportingFlow.Create)
        .WithDescription(
          "Generates an interactive Plotly HTML heatmap from the aggregated coverage CSV."
        );
    });
  }
}
