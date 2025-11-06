using Flowthru.Pipelines;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestNodes;

namespace Flowthru.Tests.Validation.GraphConstruction;

/// <summary>
/// Tests verifying that multiple nodes writing to the same catalog entry are detected.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("GraphConstruction")]
public class MultipleWritersTests
{
  private SimpleThreeNodeCatalog _catalog = null!;

  [SetUp]
  public void SetUp()
  {
    _catalog = new SimpleThreeNodeCatalog();
  }

  [Test]
  public void Build_WhenTwoNodesWriteSameOutput_ThrowsInvalidOperationException()
  {
    // ===========
    // Arrange: Two nodes both write to StepOne
    // ===========
    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "NodeA",
        transform: PassthroughNode.Create(),
        input: _catalog.Input,
        output: _catalog.StepOne
      );
      builder.AddNode(
        label: "NodeB",
        transform: PassthroughNode.Create(),
        input: _catalog.Input,
        output: _catalog.StepOne
      ); // Conflict!
    });

    // ===========
    // Act & Assert
    // ===========
    var ex = Assert.Throws<InvalidOperationException>(() => pipeline.Build());
    Assert.That(
      ex!.Message,
      Does.Contain("multiple").IgnoreCase,
      "Error message should indicate multiple writers"
    );
  }

  [Test]
  public void Build_WhenThreeNodesWriteSameOutput_ThrowsInvalidOperationException()
  {
    // ===========
    // Arrange: Three nodes all write to Output
    // ===========
    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "NodeA",
        transform: PassthroughNode.Create(),
        input: _catalog.Input,
        output: _catalog.Output
      );
      builder.AddNode(
        label: "NodeB",
        transform: PassthroughNode.Create(),
        input: _catalog.StepOne,
        output: _catalog.Output
      );
      builder.AddNode(
        label: "NodeC",
        transform: PassthroughNode.Create(),
        input: _catalog.StepTwo,
        output: _catalog.Output
      );
    });

    // ===========
    // Act & Assert
    // ===========
    Assert.Throws<InvalidOperationException>(() => pipeline.Build());
  }

  [Test]
  public void Build_WhenEachNodeWritesDifferentOutput_SucceedsWithoutError()
  {
    // ===========
    // Arrange: Each node writes to a unique output
    // ===========
    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "NodeA",
        transform: PassthroughNode.Create(),
        input: _catalog.Input,
        output: _catalog.StepOne
      );
      builder.AddNode(
        label: "NodeB",
        transform: PassthroughNode.Create(),
        input: _catalog.Input,
        output: _catalog.StepTwo
      );
      builder.AddNode(
        label: "NodeC",
        transform: PassthroughNode.Create(),
        input: _catalog.Input,
        output: _catalog.Output
      );
    });

    // ===========
    // Act & Assert
    // ===========
    Assert.DoesNotThrow(() => pipeline.Build());
    Assert.That(pipeline.IsBuilt, Is.True);
  }

  [Test]
  public void Build_WhenNodeReadsWhatAnotherWrites_SucceedsWithoutError()
  {
    // ===========
    // Arrange: Linear pipeline where each node reads from previous node's output
    // ===========
    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "NodeA",
        transform: PassthroughNode.Create(),
        input: _catalog.Input,
        output: _catalog.StepOne
      );
      builder.AddNode(
        label: "NodeB",
        transform: PassthroughNode.Create(),
        input: _catalog.StepOne,
        output: _catalog.StepTwo
      );
      builder.AddNode(
        label: "NodeC",
        transform: PassthroughNode.Create(),
        input: _catalog.StepTwo,
        output: _catalog.Output
      );
    });

    // ===========
    // Act & Assert
    // ===========
    Assert.DoesNotThrow(() => pipeline.Build());
    Assert.That(pipeline.IsBuilt, Is.True);
  }
}
