using Flowthru.Core.Cli;
using Flowthru.Core.Services;
using Flowthru.Extensions.Python;
using Flowthru.Extensions.Python.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
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
  /// <summary>
  /// Main entry point for the CLI application.
  /// </summary>
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

  /// <summary>
  /// Configures services for the application. Used by test infrastructure.
  /// </summary>
  /// <param name="basePath">Optional base path for data files (defaults to current directory)</param>
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

  /// <summary>
  /// Shared service configuration logic.
  /// </summary>
  private static void ConfigureServices(
    IServiceCollection services,
    string basePath,
    string outputPath
  )
  {
    // Add logging first (required by PythonRuntime)
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

    services.AddFlowthru(
      configuration,
      flowthru =>
      {
        flowthru.RegisterCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));

        // Output pipeline metadata
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

        // Configure Python runtime
        flowthru.UsePython(python =>
        {
          // Project root: makes Flows/ importable as a Python module tree
          python.ModuleSearchPaths.Add(basePath);
          // Output directory: contains the flowthru package (@step decorator)
          python.ModuleSearchPaths.Add(outputPath);
          // Use this example's own output directory for venv isolation
          python.VenvPath = outputPath;
        });

        // Phase 6 workaround: Resolve Python dependencies for pipeline registration
        // Build temp provider after UsePython registers services but before pipeline registration
        // NOTE: Don't dispose - we need the singleton instances to stay alive
        var tempProvider = flowthru.Services.BuildServiceProvider();
        var executor =
          tempProvider.GetRequiredService<Flowthru.Extensions.Python.Execution.IPythonExecutor>();

        // Register pipelines with resolved executor
        flowthru
          .RegisterFlow(label: "DataProcessing", flow: DataProcessingFlow.Create)
          .WithDescription("Preprocesses companies, shuttles, and reviews data using Python");

        flowthru
          .RegisterFlow(label: "DataScience", flow: DataScienceFlow.Create)
          .WithDescription(
            "Trains linear regression model for price prediction using Python/scikit-learn"
          );

        flowthru
          .RegisterFlow(label: "Reporting", flow: ReportingFlow.Create)
          .WithDescription(
            "Generates visualization outputs including passenger capacity plots and confusion matrix"
          );
      }
    );
  }
}
