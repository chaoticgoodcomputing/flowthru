using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Raw;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataProcessing.Nodes;
using NUnit.Framework;

namespace Flowthru.Tests.KedroSpaceflights.Tests.Pipelines.DataProcessing;

/// <summary>
/// Unit tests for PreprocessCompaniesNode.
/// Tests string parsing and type conversion logic.
/// </summary>
[TestFixture]
public class PreprocessCompaniesNodeTests
{
  [Test]
  public void Transform_ShouldParsePercentageCorrectly()
  {
    // Arrange
    var node = new PreprocessCompaniesNode();
    var input = new[]
    {
      new CompanyRawSchema
      {
        Id = "company1",
        CompanyRating = "95%",
        CompanyLocation = "Earth",
        TotalFleetCount = "10",
        IataApproved = "t"
      }
    };

    // Act
    var result = node.TestTransform(input).Result.ToList();

    // Assert
    var company = result.Single();
    Assert.That(company.CompanyRating, Is.EqualTo(0.95m)); // "95%" → 0.95
    Assert.That(company.IataApproved, Is.True);
  }

  [Test]
  public void Transform_ShouldParseTrueFalseStrings()
  {
    // Arrange
    var node = new PreprocessCompaniesNode();
    var input = new[]
    {
      new CompanyRawSchema
      {
        Id = "company1",
        CompanyRating = "100%",
        CompanyLocation = "Mars",
        TotalFleetCount = "5",
        IataApproved = "t"
      },
      new CompanyRawSchema
      {
        Id = "company2",
        CompanyRating = "80%",
        CompanyLocation = "Moon",
        TotalFleetCount = "3",
        IataApproved = "f"
      }
    };

    // Act
    var result = node.TestTransform(input).Result.ToList();

    // Assert
    Assert.That(result[0].IataApproved, Is.True);
    Assert.That(result[1].IataApproved, Is.False);
  }

  [Test]
  public void Transform_ShouldHandleNullOrEmptyPercentage()
  {
    // Arrange
    var node = new PreprocessCompaniesNode();
    var input = new[]
    {
      new CompanyRawSchema
      {
        Id = "company1",
        CompanyRating = null,
        CompanyLocation = "Earth",
        TotalFleetCount = "10",
        IataApproved = "t"
      },
      new CompanyRawSchema
      {
        Id = "company2",
        CompanyRating = "  ",
        CompanyLocation = "Mars",
        TotalFleetCount = "5",
        IataApproved = "t"
      }
    };

    // Act
    var result = node.TestTransform(input).Result.ToList();

    // Assert
    Assert.That(result[0].CompanyRating, Is.EqualTo(0m));
    Assert.That(result[1].CompanyRating, Is.EqualTo(0m));
  }

  [Test]
  public void Transform_ShouldParseDecimalFleetCount()
  {
    // Arrange
    var node = new PreprocessCompaniesNode();
    var input = new[]
    {
      new CompanyRawSchema
      {
        Id = "company1",
        CompanyRating = "90%",
        CompanyLocation = "Earth",
        TotalFleetCount = "15",
        IataApproved = "t"
      }
    };

    // Act
    var result = node.TestTransform(input).Result.Single();

    // Assert
    Assert.That(result.TotalFleetCount, Is.EqualTo(15m));
  }

  [Test]
  public void Transform_ShouldReturnNullForInvalidFleetCount()
  {
    // Arrange
    var node = new PreprocessCompaniesNode();
    var input = new[]
    {
      new CompanyRawSchema
      {
        Id = "company1",
        CompanyRating = "90%",
        CompanyLocation = "Earth",
        TotalFleetCount = "invalid",
        IataApproved = "t"
      }
    };

    // Act
    var result = node.TestTransform(input).Result.Single();

    // Assert
    Assert.That(result.TotalFleetCount, Is.Null);
  }

  [Test]
  public void Transform_ShouldPreserveAllFields()
  {
    // Arrange
    var node = new PreprocessCompaniesNode();
    var input = new[]
    {
      new CompanyRawSchema
      {
        Id = "space-corp-123",
        CompanyRating = "88%",
        CompanyLocation = "Jupiter Station",
        TotalFleetCount = "42",
        IataApproved = "t"
      }
    };

    // Act
    var result = node.TestTransform(input).Result.Single();

    // Assert
    Assert.That(result.Id, Is.EqualTo("space-corp-123"));
    Assert.That(result.CompanyRating, Is.EqualTo(0.88m));
    Assert.That(result.CompanyLocation, Is.EqualTo("Jupiter Station"));
    Assert.That(result.TotalFleetCount, Is.EqualTo(42m));
    Assert.That(result.IataApproved, Is.True);
  }
}
