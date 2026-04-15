using System.Reflection;
using Flowthru.Core.Cli;
using Flowthru.Core.Services;
using Flowthru.Extensions.Python;
using Flowthru.Extensions.Python.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using KedroIrisPython.Data;
using KedroIrisPython.Flows.DataEngineering;
using KedroIrisPython.Flows.DataScience;
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

        services.AddFlowthru(flowthru =>
        {
            flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);
            flowthru.RegisterCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));

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
            var tempProvider = flowthru.Services().BuildServiceProvider();
            var executor =
          tempProvider.GetRequiredService<Flowthru.Extensions.Python.Execution.IPythonExecutor>();

            // Register pipelines with resolved executor
            flowthru
          .RegisterFlow(label: "DataEngineering", flow: DataEngineeringFlow.Create)
          .WithDescription("Splits iris data into training and test sets using Python");

            flowthru
          .RegisterFlow(label: "DataScience", flow: DataScienceFlow.Create)
          .WithDescription(
            "Trains multi-class logistic regression model and evaluates predictions using Python"
          );
        });
    }
}
