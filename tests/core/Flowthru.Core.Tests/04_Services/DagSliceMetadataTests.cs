using Flowthru.Core.Graph;
using Flowthru.Core.Graph.Meta.Models;

namespace Flowthru.Core.Tests.Services;

/// <summary>
/// Tests for <see cref="DagSliceMetadata"/>'s descriptor logic. Used by
/// <c>FilenameTemplateParser</c> to substitute the <c>{SliceType}</c> placeholder in
/// metadata export filename templates.
/// </summary>
[TestFixture]
[Category("Services")]
public class DagSliceMetadataTests
{
  [Test]
  public void FromStrategy_NullStrategy_ReturnsNull()
  {
    Assert.That(DagSliceMetadata.FromStrategy(null), Is.Null);
  }

  [Test]
  public void FromStrategy_AllSliceCriteria_PopulatesAllFields()
  {
    var strategy = new FlowSliceStrategy
    {
      Flows = new HashSet<string> { "FlowA" },
      From = new HashSet<string> { "step1" },
      To = new HashSet<string> { "step9" },
      Only = new HashSet<string> { "stepX" },
    };

    var metadata = DagSliceMetadata.FromStrategy(strategy);

    Assert.That(metadata, Is.Not.Null);
    Assert.That(metadata!.Flows, Is.EquivalentTo(new[] { "FlowA" }));
    Assert.That(metadata.From, Is.EquivalentTo(new[] { "step1" }));
    Assert.That(metadata.To, Is.EquivalentTo(new[] { "step9" }));
    Assert.That(metadata.Only, Is.EquivalentTo(new[] { "stepX" }));
  }

  [Test]
  public void GetSliceTypeDescriptor_SingleFlow_ReturnsFlow()
  {
    var metadata = new DagSliceMetadata { Flows = new[] { "DataScience" } };
    Assert.That(metadata.GetSliceTypeDescriptor(), Is.EqualTo("Flow"));
  }

  [Test]
  public void GetSliceTypeDescriptor_MultipleFlows_ReturnsFlows()
  {
    var metadata = new DagSliceMetadata { Flows = new[] { "FlowA", "FlowB" } };
    Assert.That(metadata.GetSliceTypeDescriptor(), Is.EqualTo("Flows"));
  }

  [Test]
  public void GetSliceTypeDescriptor_FromOnly_ReturnsFrom()
  {
    var metadata = new DagSliceMetadata { From = new[] { "step1" } };
    Assert.That(metadata.GetSliceTypeDescriptor(), Is.EqualTo("From"));
  }

  [Test]
  public void GetSliceTypeDescriptor_ToOnly_ReturnsTo()
  {
    var metadata = new DagSliceMetadata { To = new[] { "step9" } };
    Assert.That(metadata.GetSliceTypeDescriptor(), Is.EqualTo("To"));
  }

  [Test]
  public void GetSliceTypeDescriptor_OnlyAllowlist_ReturnsOnly()
  {
    var metadata = new DagSliceMetadata { Only = new[] { "stepX" } };
    Assert.That(metadata.GetSliceTypeDescriptor(), Is.EqualTo("Only"));
  }

  [Test]
  public void GetSliceTypeDescriptor_MultipleCriteria_ReturnsComposedSlice()
  {
    var metadata = new DagSliceMetadata
    {
      From = new[] { "step1" },
      To = new[] { "step9" },
    };
    Assert.That(metadata.GetSliceTypeDescriptor(), Is.EqualTo("ComposedSlice"));
  }
}
