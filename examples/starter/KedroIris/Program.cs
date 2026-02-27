using Flowthru.Cli;
using Flowthru.Services;
using KedroIris.Data;
using KedroIris.Pipelines.DataEngineering;
using KedroIris.Pipelines.DataScience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KedroIris;

/// <summary>
/// Main application entry point for the Iris classification pipeline.
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
      flowthru.ConfigureMetadata(meta =>
      {
        meta.WithOutputDirectory("Metadata").AddJson().AddMermaid();
      });

      // Register data engineering pipeline with configuration parameters
      flowthru
        .RegisterPipelineWithConfiguration<Catalog, DataEngineeringPipeline.Params>(
          label: "DataEngineering",
          pipeline: DataEngineeringPipeline.Create,
          configurationSection: "Flowthru:Pipelines:DataEngineering"
        )
        .WithDescription("Splits iris data into training and test sets with one-hot encoding");

      // Register data science pipeline with configuration parameters
      flowthru
        .RegisterPipelineWithConfiguration<Catalog, DataSciencePipeline.Params>(
          label: "DataScience",
          pipeline: DataSciencePipeline.Create,
          configurationSection: "Flowthru:Pipelines:DataScience"
        )
        .WithDescription("Trains multi-class logistic regression model for iris classification");
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

    // Resolve core service and construct CLI wrapper
    var service = services.GetRequiredService<IFlowthruService>();
    var logger = services.GetRequiredService<ILogger<FlowthruCli>>();
    var cli = new FlowthruCli(service, logger);

    return await cli.RunAsync(args);
  }
}
