using Flowthru.Spaceflights.Data.Schemas.Processed;
using Flowthru.Spaceflights.Data.Schemas.Models;
using Flowthru.Spaceflights.Pipelines.DataScience.Nodes;
using Flowthru.Spaceflights.Pipelines.DataScience.Parameters;
using NUnit.Framework;

namespace Flowthru.Spaceflights.Tests.Pipelines.DataScience;

/// <summary>
/// Unit tests for SplitDataNode.
/// Tests node logic in isolation without pipeline orchestration.
/// </summary>
[TestFixture]
public class SplitDataNodeTests
{
  [Test]
  public void Transform_ShouldSplitDataCorrectly()
  {
    // Arrange
    var node = new SplitDataNode
    {
      Parameters = new ModelOptions
      {
        TestSize = 0.2,
        RandomState = 42
      }
    };

    var inputData = new[]
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

    // Act
    var result = node.Transform(inputData).Result.Single();

    // Assert - verify multi-output structure
    Assert.That(result, Is.Not.Null);
    Assert.That(result, Is.TypeOf<SplitDataOutputs>());
    Assert.That(result.XTrain.Count(), Is.EqualTo(2)); // 80% of 3 = 2.4 → 2
    Assert.That(result.XTest.Count(), Is.EqualTo(1));  // 20% of 3 = 0.6 → 1
    Assert.That(result.YTrain.Count(), Is.EqualTo(2));
    Assert.That(result.YTest.Count(), Is.EqualTo(1));
  }

  [Test]
  public void Transform_ShouldConvertFieldsToFloat()
  {
    // Arrange
    var node = new SplitDataNode
    {
      Parameters = new ModelOptions { TestSize = 0.0, RandomState = 42 }
    };

    var inputData = new[]
    {
      new ModelInputSchema
      {
        Engines = 5,
        PassengerCapacity = 250,
        Crew = 15,
        DCheckComplete = true,
        MoonClearanceComplete = true,
        IataApproved = false,
        CompanyRating = 0.88m,
        ReviewScoresRating = 4.2m,
        Price = 15000m
      }
    };

    // Act
    var result = node.Transform(inputData).Result.Single();

    // Assert
    var trainRow = result.XTrain.Single();
    Assert.That(trainRow.Engines, Is.EqualTo(5f));
    Assert.That(trainRow.PassengerCapacity, Is.EqualTo(250f));
    Assert.That(trainRow.Crew, Is.EqualTo(15f));
    Assert.That(trainRow.CompanyRating, Is.EqualTo(0.88f));
    Assert.That(trainRow.ReviewScoresRating, Is.EqualTo(4.2f));
    Assert.That(trainRow.Price, Is.EqualTo(15000f));
  }

  [Test]
  public void Transform_ShouldHandleNullValues()
  {
    // Arrange
    var node = new SplitDataNode
    {
      Parameters = new ModelOptions { TestSize = 0.0, RandomState = 42 }
    };

    var inputData = new[]
    {
      new ModelInputSchema
      {
        Engines = null,
        PassengerCapacity = null,
        Crew = null,
        DCheckComplete = false,
        MoonClearanceComplete = false,
        IataApproved = false,
        CompanyRating = 0.5m,
        ReviewScoresRating = null,
        Price = 5000m
      }
    };

    // Act
    var result = node.Transform(inputData).Result.Single();

    // Assert
    var trainRow = result.XTrain.Single();
    Assert.That(trainRow.Engines, Is.EqualTo(0f)); // null → 0
    Assert.That(trainRow.PassengerCapacity, Is.EqualTo(0f)); // null → 0
    Assert.That(trainRow.Crew, Is.EqualTo(0f)); // null → 0
    Assert.That(trainRow.ReviewScoresRating, Is.EqualTo(0f)); // null → 0
  }

  [Test]
  public void Transform_WithDifferentRandomState_ShouldProduceDifferentSplits()
  {
    // Arrange
    var inputData = Enumerable.Range(1, 100)
      .Select(i => new ModelInputSchema
      {
        Engines = i,
        PassengerCapacity = i * 10,
        Crew = i,
        DCheckComplete = i % 2 == 0,
        MoonClearanceComplete = i % 3 == 0,
        IataApproved = i % 5 == 0,
        CompanyRating = 0.5m,
        ReviewScoresRating = 3.0m,
        Price = i * 1000m
      })
      .ToArray();

    var node1 = new SplitDataNode
    {
      Parameters = new ModelOptions { TestSize = 0.2, RandomState = 42 }
    };

    var node2 = new SplitDataNode
    {
      Parameters = new ModelOptions { TestSize = 0.2, RandomState = 123 }
    };

    // Act
    var result1 = node1.Transform(inputData).Result.Single();
    var result2 = node2.Transform(inputData).Result.Single();

    // Assert - Same sizes
    Assert.That(result1.XTrain.Count, Is.EqualTo(result2.XTrain.Count));
    Assert.That(result1.XTest.Count, Is.EqualTo(result2.XTest.Count));

    // Assert - Different ordering (highly probable with different seeds)
    var firstTrainPrice1 = result1.YTrain.First();
    var firstTrainPrice2 = result2.YTrain.First();
    Assert.That(firstTrainPrice1, Is.Not.EqualTo(firstTrainPrice2));
  }
}
