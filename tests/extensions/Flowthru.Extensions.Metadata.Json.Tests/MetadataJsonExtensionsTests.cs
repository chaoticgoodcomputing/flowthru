using Flowthru.Core.Graph.Meta.Models;
using Flowthru.Meta;

namespace Flowthru.Extensions.Metadata.Json.Tests;

/// <summary>
/// Tests for <see cref="MetadataJsonExtensions"/> serialization helpers.
/// </summary>
[TestFixture]
public class MetadataJsonExtensionsTests
{
  private static DagMetadata SampleDag() =>
    new()
    {
      FlowName = "TestFlow",
      Steps = new(),
      CatalogItems = new(),
      Edges = new(),
    };

  [Test]
  public void ToJson_ProducesIndentedJson()
  {
    var json = SampleDag().ToJson();

    Assert.That(json, Is.Not.Null.And.Not.Empty);
    Assert.That(json, Does.Contain("\"flowName\": \"TestFlow\""));
    Assert.That(json, Does.Contain("\n"), "Default ToJson is indented (WriteIndented = true).");
  }

  [Test]
  public void ToCompactJson_ProducesSingleLineJson()
  {
    var json = SampleDag().ToCompactJson();

    Assert.That(json, Is.Not.Null.And.Not.Empty);
    Assert.That(json, Does.Contain("\"flowName\":\"TestFlow\""));
    Assert.That(
      json,
      Does.Not.Contain("\n  \""),
      "Compact JSON should not have indented properties."
    );
  }

  [Test]
  public void FromJson_RoundTripsPreservingFlowName()
  {
    var original = SampleDag();
    var json = original.ToJson();
    var restored = MetadataJsonExtensions.FromJson(json);

    Assert.That(restored.FlowName, Is.EqualTo(original.FlowName));
  }

  [Test]
  public void ToJson_NullMetadata_ThrowsArgumentNullException()
  {
    DagMetadata? metadata = null;
    Assert.That(() => metadata!.ToJson(), Throws.ArgumentNullException);
  }

  [Test]
  public void ToCompactJson_NullMetadata_ThrowsArgumentNullException()
  {
    DagMetadata? metadata = null;
    Assert.That(() => metadata!.ToCompactJson(), Throws.ArgumentNullException);
  }

  [Test]
  public void FromJson_EmptyString_ThrowsArgumentException()
  {
    Assert.That(() => MetadataJsonExtensions.FromJson(""), Throws.ArgumentException);
  }

  [Test]
  public void FromJson_InvalidJson_Throws()
  {
    Assert.That(() => MetadataJsonExtensions.FromJson("{ not valid json"), Throws.Exception);
  }
}
