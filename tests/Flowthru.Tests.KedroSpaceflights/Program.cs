using Flowthru.Application;
using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataProcessing;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;
using Microsoft.Extensions.Logging;

namespace Flowthru.Tests.KedroSpaceflights;

/// <summary>
/// Entry point for the Spaceflights FlowThru example.
/// Demonstrates the complete user-facing API for defining and running data pipelines.
/// </summary>
public class Program {
  public static async Task<int> Main(string[] args) {
    var app = FlowthruApplication.Create(args, builder => {
      // Configure catalog
      builder.UseCatalog(new SpaceflightsCatalog("Data/Datasets"));

      // Register pipelines inline (no separate registry class needed)
      builder
          .RegisterPipeline<SpaceflightsCatalog>("data_processing", DataProcessingPipeline.Create)
          .WithDescription("Preprocesses raw data and creates model input table");

      builder
        .RegisterPipeline<SpaceflightsCatalog, DataSciencePipelineParams>(
          "data_science",
          DataSciencePipeline.Create,
          // Provide parameters for the data science pipeline
          new DataSciencePipelineParams(
            // Options for model training
            new ModelParams {
              TestSize = 0.2,
              RandomState = 3,
              Features =
                [
                  "Engines",
                  "PassengerCapacity",
                  "Crew",
                  "DCheckComplete",
                  "MoonClearanceComplete",
                  "IataApproved",
                  "CompanyRating",
                  "ReviewScoresRating"
                ]
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

      // Configure logging
      builder.ConfigureLogging(logging => {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
      });
    });

    return await app.RunAsync();
  }
}
