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
  public void Create_ShouldBuildPipelineWithThreeNodes()
  {
    // Arrange
    var catalog = new SpaceflightsCatalog();

    // Act
    var pipeline = DataSciencePipeline.Create(catalog, CreateDefaultOptions());

    // Assert
    Assert.That(pipeline, Is.Not.Null);
    // TODO: Add assertions once Pipeline exposes node count or similar metadata
  }

  [Test]
  [Ignore("Integration test requires catalog refactoring - SpaceflightsCatalog doesn't support dynamic registration")]
  public async Task Run_ShouldExecuteFullPipelineSuccessfully()
  {
    // Arrange
    var catalog = new SpaceflightsCatalog();

    // Create dummy model input data
    var modelInputData = new[]
    {
      new ModelInputSchema
      {
        Engines = 1,
        PassengerCapacity = 100,
        Crew = 10,
        DCheckComplete = true,
        MoonClearanceComplete = true,
        IataApproved = true,
        CompanyRating = 0.95m,
        ReviewScoresRating = 4.5m,
        Price = 10000m
      },
      new ModelInputSchema
      {
        Engines = 2,
        PassengerCapacity = 200,
        Crew = 20,
        DCheckComplete = true,
        MoonClearanceComplete = false,
        IataApproved = true,
        CompanyRating = 0.85m,
        ReviewScoresRating = 4.0m,
        Price = 20000m
      },
      new ModelInputSchema
      {
        Engines = 3,
        PassengerCapacity = 300,
        Crew = 30,
        DCheckComplete = false,
        MoonClearanceComplete = true,
        IataApproved = true,
        CompanyRating = 0.75m,
        ReviewScoresRating = 3.5m,
        Price = 30000m
      }
    };

    // Register test data in catalog
    // catalog.Register<ModelInputSchema>("model_input_table", modelInputData);

    var pipeline = DataSciencePipeline.Create(catalog, CreateDefaultOptions());

    // Act
    var result = await pipeline.RunAsync();

    // Assert
    Assert.That(result.Success, Is.True); // Assuming Either<Exception, Unit> pattern

    // Verify outputs were created
    // var metrics = catalog.Load<ModelMetrics>("model_metrics");
    // Assert.That(metrics, Is.Not.Null);
    // Assert.That(metrics.R2Score, Is.InRange(-1.0, 1.0)); // R² should be in valid range
  }

  [Test]
  [Ignore("Integration test requires catalog refactoring - SpaceflightsCatalog doesn't support dynamic registration")]
  public async Task Run_WithCustomParameters_ShouldUseThem()
  {
    // Arrange
    var catalog = new SpaceflightsCatalog();

    var modelInputData = CreateLargerDummyDataset(100); // Helper method

    var pipeline = DataSciencePipeline.Create(catalog, CreateDefaultOptions());

    // Act
    var result = await pipeline.RunAsync();

    // Assert
    Assert.That(result.Success, Is.True);

    // Verify the split outputs were created as individual catalog entries
    // var xTrain = catalog.Load<IEnumerable<FeatureRow>>("x_train");
    // var xTest = catalog.Load<IEnumerable<FeatureRow>>("x_test");
    // Assert.That(xTrain, Is.Not.Null);
    // Assert.That(xTest, Is.Not.Null);

    // With 100 records and 30% test size, expect 70/30 split
    // Assert.That(xTrain.Count(), Is.EqualTo(70));
    // Assert.That(xTest.Count(), Is.EqualTo(30));
  }

  [Test]
  [Ignore("Integration test requires full pipeline execution implementation")]
  public async Task Run_WithMissingInput_ShouldFail()
  {
    // Arrange
    var catalog = new SpaceflightsCatalog();
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
