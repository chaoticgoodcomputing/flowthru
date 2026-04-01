using System.Reflection;
using Flowthru.Cli;
using Flowthru.Extensions.Python;
using Flowthru.Extensions.Python.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using Flowthru.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpaceflightsPythonEFCore.Data;
using SpaceflightsPythonEFCore.Pipelines.DataProcessing;
using SpaceflightsPythonEFCore.Pipelines.DataScience;
using SpaceflightsPythonEFCore.Pipelines.Reporting;

namespace SpaceflightsPythonEFCore;

/// <summary>
/// Helper to access FlowthruServiceBuilder internals for pre-registration executor resolution.
/// </summary>
internal static class FlowthruServiceBuilderExtensions
{
  public static IServiceCollection Services(this FlowthruServiceBuilder builder)
  {
    var field = typeof(FlowthruServiceBuilder).GetField(
      "_services",
      BindingFlags.NonPublic | BindingFlags.Instance
    );
    return (IServiceCollection)field!.GetValue(builder)!;
  }
}

/// <summary>
/// Entry point for the SpaceflightsPythonEFCore advanced example.
///
/// Demonstrates mixed extension use within a single pipeline:
///   - DataProcessing: C# nodes writing to EFCore/SQLite
///   - DataScience:    Python nodes reading from and writing to EFCore/SQLite
///   - Reporting:      Python nodes reading from EFCore/SQLite for visualization
/// </summary>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, Directory.GetCurrentDirectory())
    );

  public static IServiceProvider ConfigureServices(string? basePath = null)
  {
    var services = new ServiceCollection();
    ConfigureServices(services, basePath ?? Directory.GetCurrentDirectory());
    return services.BuildServiceProvider();
  }

  private static void ConfigureServices(IServiceCollection services, string basePath)
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

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);
      flowthru.UseCatalog(sp => new Catalog(
        basePath: Path.Combine(basePath, "Data"),
        contextFactory: sp.GetRequiredService<IDbContextFactory<SpaceflightsDbContext>>()
      ));

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddProvider<JsonMetadataProvider, JsonMetadataProviderBuilder>(json =>
            json.WithOutputDirectory(metadataPath)
          )
          .AddProvider<MermaidMetadataProvider, MermaidMetadataProviderBuilder>(mermaid =>
            mermaid.WithOutputDirectory(metadataPath)
          );
      });

      // Configure Python runtime; module search paths include the project root so
      // "Pipelines.DataScience.Nodes.*" and "Pipelines.Reporting.Nodes.*" resolve correctly.
      flowthru.UsePython(python =>
      {
        python.ModuleSearchPaths.Add(basePath);
        python.ModuleSearchPaths.Add(AppDomain.CurrentDomain.BaseDirectory);
      });

      // Resolve the Python executor before pipeline registration (Phase 6 workaround).
      // NOTE: Do not dispose — singleton instances must stay alive.
      var tempProvider = flowthru.Services().BuildServiceProvider();
      var executor =
        tempProvider.GetRequiredService<Flowthru.Extensions.Python.Execution.IPythonExecutor>();

      flowthru
        .RegisterPipeline(label: "DataProcessing", pipeline: DataProcessingPipeline.Create)
        .WithDescription("Preprocesses companies and shuttles (C#), stores in EFCore");

      flowthru
        .RegisterPipeline(label: "DataScience", pipeline: DataSciencePipeline.Create)
        .WithDescription("Trains and evaluates regression model (Python); reads/writes EFCore");

      flowthru
        .RegisterPipeline(label: "Reporting", pipeline: ReportingPipeline.Create)
        .WithDescription(
          "Generates visualizations (Python); reads PreprocessedShuttles and ModelPredictions from EFCore"
        );
    });
  }
}
