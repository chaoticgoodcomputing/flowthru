using Flowthru.Cli;
using KedroSpaceflights.Pure.Data;
using KedroSpaceflights.Pure.Pipelines.DataProcessing;
using KedroSpaceflights.Pure.Pipelines.DataScience;
using KedroSpaceflights.Pure.Pipelines.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroSpaceflights.Pure;

/// <summary>
/// Main application entry point for the Spaceflights price prediction pipeline.
/// </summary>
public class Program
{
  /// <summary>
  /// Configures Flowthru services for dependency injection.
  /// </summary>
  /// <returns>Configured service provider</returns>
  public static IServiceProvider ConfigureServices()
  {
    var services = new ServiceCollection();

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseConfiguration();
      flowthru.UseCatalog(_ => new Catalog("Data"));

      // Register data processing pipeline
      flowthru
        .RegisterPipeline<Catalog>(label: "DataProcessing", pipeline: DataProcessingPipeline.Create)
        .WithDescription("Preprocesses companies and shuttles data");

      // Register data science pipeline with configuration parameters
      flowthru
        .RegisterPipelineWithConfiguration<Catalog, DataSciencePipeline.Params>(
          label: "DataScience",
          pipeline: DataSciencePipeline.Create,
          configurationSection: "Flowthru:Pipelines:DataScience"
        )
        .WithDescription("Trains linear regression model for price prediction");

      // Register reporting pipeline with configuration parameters
      flowthru
        .RegisterPipelineWithConfiguration<Catalog, ReportingPipeline.Params>(
          label: "Reporting",
          pipeline: ReportingPipeline.Create,
          configurationSection: "Flowthru:Pipelines:Reporting"
        )
        .WithDescription("Generates passenger capacity reports and visualizations");
    });

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });

    return services.BuildServiceProvider();
  }

  /// <summary>
  /// Main entry point for the application. Builds the service provider and runs the CLI.
  /// </summary>
  /// <param name="args">Command-line arguments.</param>
  public static async Task<int> Main(string[] args)
  {
    var services = ConfigureServices();
    var cli = services.GetRequiredService<Flowthru.Cli.FlowthruCli>();
    return await cli.RunAsync(args);
  }
}
