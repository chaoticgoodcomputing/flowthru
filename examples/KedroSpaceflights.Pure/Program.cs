using Flowthru.Application;
using KedroSpaceflights.Pure.Data;
using KedroSpaceflights.Pure.Pipelines.DataProcessing;
using KedroSpaceflights.Pure.Pipelines.DataScience;
using KedroSpaceflights.Pure.Pipelines.Reporting;

namespace KedroSpaceflights.Pure;

/// <summary>
/// Main application entry point for the Spaceflights price prediction pipeline.
/// </summary>
public class Program
{
  /// <summary>
  /// Application entry point. Configures and runs the Flowthru application with three pipelines:
  /// DataProcessing, DataScience, and Reporting.
  /// </summary>
  /// <param name="args">Command-line arguments.</param>
  /// <returns>Exit code (0 for success).</returns>
  public static async Task<int> Main(string[] args)
  {
    var app = FlowthruApplication.Create(
      args,
      builder =>
      {
        builder.UseConfiguration();

        // Register data processing pipeline
        builder
          .RegisterPipeline<Catalog>(
            label: "DataProcessing",
            pipeline: DataProcessingPipeline.Create
          )
          .WithDescription("Preprocesses companies and shuttles data");

        // Register data science pipeline with configuration parameters
        builder
          .RegisterPipelineWithConfiguration<Catalog, DataSciencePipeline.Params>(
            label: "DataScience",
            pipeline: DataSciencePipeline.Create,
            configurationSection: "Flowthru:Pipelines:DataScience"
          )
          .WithDescription("Trains linear regression model for price prediction");

        // Register reporting pipeline with configuration parameters
        builder
          .RegisterPipelineWithConfiguration<Catalog, ReportingPipeline.Params>(
            label: "Reporting",
            pipeline: ReportingPipeline.Create,
            configurationSection: "Flowthru:Pipelines:Reporting"
          )
          .WithDescription("Generates passenger capacity reports and visualizations");
      }
    );

    return await app.RunAsync();
  }
}
