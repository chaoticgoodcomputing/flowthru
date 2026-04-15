using Flowthru.Core.Cli;
using Flowthru.Core.Services;
using Flowthru.Meta;
using Flowthru.Meta.Providers;
using KedroSpaceflights.Custom.Data;
using KedroSpaceflights.Custom.Flows.DataDiagnostics;
using KedroSpaceflights.Custom.Flows.DataEvaluation;
using KedroSpaceflights.Custom.Flows.DataProcessing;
using KedroSpaceflights.Custom.Flows.DataScience;
using KedroSpaceflights.Custom.Flows.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroSpaceflights.Custom;

/// <summary>
/// Entry point for the Spaceflights FlowThru example.
/// Demonstrates a hybrid configuration approach:
/// - Infrastructure (catalog, metadata, logging) configured in appsettings.json
/// - Flow registration in code for compile-time safety
/// - Flow parameters loaded from appsettings.json for easy tuning
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the custom Spaceflights pipeline CLI application.
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
        services.AddFlowthru(flowthru =>
        {
            // Enable configuration loading from appsettings.json files
            // This loads: appsettings.json (base) -> appsettings.{Environment}.json -> appsettings.Local.json
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

            flowthru
          .RegisterFlow(label: "DataProcessing", flow: DataProcessingFlow.Create)
          .WithDescription("Preprocesses raw data and creates model input table");

            flowthru
          .RegisterFlow(
            label: "DataScience",
            flow: DataScienceFlow.Create,
            configurationSection: "Flowthru:Flows:DataScience"
          )
          .WithDescription("Trains ML model");

            flowthru
          .RegisterFlow(label: "DataDiagnostics", flow: DataDiagnosticsFlow.Create)
          .WithDescription(
            "Validates pipeline outputs against Kedro reference and exports diagnostic data"
          );

            flowthru
          .RegisterFlow(
            label: "DataEvaluation",
            flow: DataEvaluationFlow.Create,
            configurationSection: "Flowthru:Flows:DataEvaluation"
          )
          .WithDescription("Evaluates ML model performance and cross-validation");

            flowthru
          .RegisterFlow(label: "Reporting", flow: ReportingFlow.Create)
          .WithDescription("Generates reports and visualizations");

            flowthru.ConfigureExecution(opts => opts.MaxDegreeOfParallelism = 8);
        });

        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });
    }
}
