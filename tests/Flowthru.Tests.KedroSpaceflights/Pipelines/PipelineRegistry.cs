using Flowthru.Registry;
using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataProcessing;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines;

/// <summary>
/// Central registry for all pipelines in the Spaceflights project.
/// Provides named access to configured pipelines.
/// </summary>
public class SpaceflightsPipelineRegistry : PipelineRegistry<SpaceflightsCatalog>
{
  /// <summary>
  /// Registers all available pipelines.
  /// </summary>
  protected override void RegisterPipelines(IPipelineRegistrar<SpaceflightsCatalog> registrar)
  {
    // Register data processing pipeline (no parameters)
    registrar
      .Register("data_processing", DataProcessingPipeline.Create)
      .WithDescription("Preprocesses raw data and creates model input table")
      .WithTags("etl", "preprocessing");

    // Register data science pipeline (with parameters)
    registrar
      .Register("data_science", DataSciencePipeline.Create, new ModelOptions
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
  }
}
