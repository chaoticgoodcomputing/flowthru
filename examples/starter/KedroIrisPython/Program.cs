using System.Reflection;
using Flowthru.Cli;
using Flowthru.Extensions.Python;
using Flowthru.Extensions.Python.Services;
using Flowthru.Services;
using KedroIrisPython.Data;
using KedroIrisPython.Pipelines.DataEngineering;
using KedroIrisPython.Pipelines.DataScience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroIrisPython;

/// <summary>
/// Helper extension for accessing FlowthruServiceBuilder internals.
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
/// Main application entry point for the Iris pipeline with Python nodes.
/// </summary>
public class Program
{
  /// <summary>
  /// Main entry point for the CLI application.
  /// </summary>
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
    // Add logging first (required by PythonRuntime)
    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);
      flowthru.UseCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));

      // Configure Python runtime
      flowthru.UsePython(python =>
      {
        // Add project root to sys.path (for importing from Pipelines/)
        python.ModuleSearchPaths.Add(basePath);

        // Add output directory for flowthru Python package (contains @node decorator)
        var outputDir = AppDomain.CurrentDomain.BaseDirectory;
        python.ModuleSearchPaths.Add(outputDir);

        // Note: PythonRuntime auto-discovers .venv in AppContext.BaseDirectory via uv sync
      });

      // Phase 6 workaround: Resolve Python dependencies for pipeline registration
      // Build temp provider after UsePython registers services but before pipeline registration
      // NOTE: Don't dispose - we need the singleton instances to stay alive
      var tempProvider = flowthru.Services().BuildServiceProvider();
      var executor =
        tempProvider.GetRequiredService<Flowthru.Extensions.Python.Execution.IPythonExecutor>();

      // Register pipelines with resolved executor
      flowthru
        .RegisterPipeline<Catalog, Flowthru.Extensions.Python.Execution.IPythonExecutor>(
          label: "DataEngineering",
          pipeline: DataEngineeringPipeline.Create,
          parameters: executor
        )
        .WithDescription("Splits iris data into training and test sets using Python");

      flowthru
        .RegisterPipeline<Catalog, Flowthru.Extensions.Python.Execution.IPythonExecutor>(
          label: "DataScience",
          pipeline: DataSciencePipeline.Create,
          parameters: executor
        )
        .WithDescription(
          "Trains multi-class logistic regression model and evaluates predictions using Python"
        );

      // Enable metadata export using configuration from appsettings.json
      flowthru.ConfigureMetadata(_ => { });
    });
  }
}
