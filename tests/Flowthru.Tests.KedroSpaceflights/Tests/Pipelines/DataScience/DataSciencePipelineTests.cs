using Flowthru.Pipelines;
using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;
using NUnit.Framework;

namespace Flowthru.Tests.KedroSpaceflights.Tests.Pipelines.DataScience;

/// <summary>
/// Integration tests for the DataSciencePipeline.
/// Tests the entire pipeline execution with a DataCatalog.
/// </summary>
[TestFixture]
public class DataSciencePipelineTests
{
  private static ModelOptions CreateDefaultOptions() => new()
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
  };

  [Test]
  [Ignore("Integration test requires full pipeline execution implementation")]
  public async Task Run_WithMissingInput_ShouldFail()
  {
    // Arrange
    var catalog = new SpaceflightsCatalog("Data/Datasets");
    // Intentionally NOT registering model_input_table - will fail when trying to load non-existent data

    var pipeline = DataSciencePipeline.Create(catalog, CreateDefaultOptions());

    // Act
    var result = await pipeline.RunAsync();

    // Assert
    Assert.That(result.Success, Is.False);
    Assert.That(result.Exception, Is.Not.Null);
  }

  /// <summary>
  /// Helper method to create a larger dataset for testing
  /// </summary>
  private static ModelInputSchema[] CreateLargerDummyDataset(int count)
  {
    return Enumerable.Range(1, count)
      .Select(i => new ModelInputSchema
      {
        Engines = i % 5 + 1,
        PassengerCapacity = (i % 10 + 1) * 50,
        Crew = i % 20 + 5,
        DCheckComplete = i % 2 == 0,
        MoonClearanceComplete = i % 3 == 0,
        IataApproved = i % 5 == 0,
        CompanyRating = 0.5m + (i % 50) * 0.01m,
        ReviewScoresRating = 2.0m + (i % 30) * 0.1m,
        Price = 5000m + i * 100m
      })
      .ToArray();
  }
}
