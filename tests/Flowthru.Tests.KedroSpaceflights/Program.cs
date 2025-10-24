using Flowthru.Application;
using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataProcessing;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataValidation;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataValidation.Nodes;
using Flowthru.Tests.KedroSpaceflights.Pipelines.Reporting;
using Microsoft.Extensions.Logging;

namespace Flowthru.Tests.KedroSpaceflights;

/// <summary>
/// Entry point for the Spaceflights FlowThru example.
/// Demonstrates the complete user-facing API for defining and running data pipelines.
/// </summary>
public class Program {
  public static async Task<int> Main(string[] args) {
    var app = FlowthruApplication.Create(args, builder => {

      // Register the Spaceflights catalog for all pipelines in this application
      builder.UseCatalog(new SpaceflightsCatalog("Data/Datasets"));

      // Enable metadata collection for Flowthru.Viz
      builder.IncludeMetadata();

      // Register the Data Processing Pipeline, which serves as the initial ingest and cleaning
      // phase for subsequent pipelines.
      builder
        .RegisterPipeline<SpaceflightsCatalog>(
          label: "DataProcessing",
          creator: DataProcessingPipeline.Create
        )
        .WithDescription("Preprocesses raw data and creates model input table");

      builder
        .RegisterPipeline<SpaceflightsCatalog, DataSciencePipelineParams>(
          label: "DataScience",
          creator: DataSciencePipeline.Create,
          parameters: new DataSciencePipelineParams(
            new ModelParams {
              TestSize = 0.2,
              RandomState = 3
            },
            // Options for cross-validation
            new CrossValidationParams {
              NumFolds = 10, // Standard 10-fold cross-validation  
              BaseSeed = 42, // A magic number, nothing up our sleeves!
              KedroReferenceR2Score = 0.387f // Baseline comparison to the seeded run of the
                                             // unmodified Kedro implementation in Python.
            }
          )
        )
        .WithDescription("Trains and evaluates ML model");

      builder
        .RegisterPipeline<SpaceflightsCatalog>(
          label: "DataValidation",
          creator: DataValidationPipeline.Create
        )
        .WithDescription("Validates pipeline outputs against Kedro reference and exports diagnostic data");

      builder
        .RegisterPipeline<SpaceflightsCatalog>(
          label: "Reporting",
          creator: ReportingPipeline.Create
        )
        .WithDescription("Generate visualizations and reports from processed data");

      // Configure logging
      builder.ConfigureLogging(logging => {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
      });
    });

    return await app.RunAsync();
  }
}
