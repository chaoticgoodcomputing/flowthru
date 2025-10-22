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
public class Program
{
  public static async Task<int> Main(string[] args)
  {
    var app = FlowthruApplication.Create(args, builder =>
    {
      // Configure catalog
      builder.UseCatalog(new SpaceflightsCatalog("Data/Datasets"));

      // Register pipelines inline (no separate registry class needed)
      builder
          .RegisterPipeline<SpaceflightsCatalog>("data_processing", DataProcessingPipeline.Create)
          .WithDescription("Preprocesses raw data and creates model input table")
          .WithTags("etl", "preprocessing");

      builder
        .RegisterPipeline<SpaceflightsCatalog, ModelOptions>(
          "data_science",
          DataSciencePipeline.Create,
          new ModelOptions
          {
            TestSize = 0.2,
            RandomState = 3,
            Features = new List<string>
              {
                "Engines",
                "PassengerCapacity",
                "Crew",
                "DCheckComplete",
                "MoonClearanceComplete",
                "IataApproved",
                "CompanyRating",
                "ReviewScoresRating"
              }
          })
        .WithDescription("Trains and evaluates ML model")
        .WithTags("ml", "training");

      // Configure logging
      builder.ConfigureLogging(logging =>
      {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
      });
    });

    return await app.RunAsync();
  }
}
