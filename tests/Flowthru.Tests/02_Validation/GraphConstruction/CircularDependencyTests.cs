using Flowthru.Pipelines;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestNodes;

namespace Flowthru.Tests.Validation.GraphConstruction;

/// <summary>
/// Tests verifying that circular dependencies are detected during Pipeline.Build().
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("GraphConstruction")]
public class CircularDependencyTests
{
  private SimpleThreeNodeCatalog _catalog = null!;

  [SetUp]
  public void SetUp()
  {
    _catalog = new SimpleThreeNodeCatalog();
  }

  [Test]
  public void Build_WhenSimpleCircle_ThrowsInvalidOperationException()
  {
    // ===========
    // Arrange: Create a circular dependency A → B → C → A
    // ===========
    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode<PassthroughNode>(_catalog.Input, _catalog.StepOne, "NodeA");
      builder.AddNode<PassthroughNode>(_catalog.StepOne, _catalog.StepTwo, "NodeB");
      builder.AddNode<PassthroughNode>(_catalog.StepTwo, _catalog.Input, "NodeC"); // Circle!
    });

    // ===========
    // Act & Assert
    // ===========
    var ex = Assert.Throws<InvalidOperationException>(() => pipeline.Build());
    Assert.That(
      ex!.Message,
      Does.Contain("circular").IgnoreCase.Or.Contain("cycle").IgnoreCase,
      "Error message should indicate circular dependency"
    );
  }

  [Test]
  public void Build_WhenSelfLoop_ThrowsInvalidOperationException()
  {
    // ===========
    // Arrange: Node writes to its own input (A → A)
    // ===========
    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode<PassthroughNode>(_catalog.Input, _catalog.Input, "SelfLoop");
    });

    // ===========
    // Act & Assert
    // ===========
    var ex = Assert.Throws<InvalidOperationException>(() => pipeline.Build());
    Assert.That(
      ex!.Message,
      Does.Contain("circular").IgnoreCase.Or.Contain("cycle").IgnoreCase,
      "Error message should indicate circular dependency or cycle"
    );
  }

  [Test]
  public void Build_WhenTwoNodeCircle_ThrowsInvalidOperationException()
  {
    // ===========
    // Arrange: A → B → A
    // ===========
    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode<PassthroughNode>(_catalog.Input, _catalog.StepOne, "NodeA");
      builder.AddNode<PassthroughNode>(_catalog.StepOne, _catalog.Input, "NodeB"); // Circle back
    });

    // ===========
    // Act & Assert
    // ===========
    Assert.Throws<InvalidOperationException>(() => pipeline.Build());
  }

  [Test]
  public void Build_WhenNoCycle_SucceedsWithoutError()
  {
    // ===========
    // Arrange: Linear pipeline A → B → C
    // ===========
    var pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode<PassthroughNode>(_catalog.Input, _catalog.StepOne, "NodeA");
      builder.AddNode<PassthroughNode>(_catalog.StepOne, _catalog.StepTwo, "NodeB");
      builder.AddNode<PassthroughNode>(_catalog.StepTwo, _catalog.Output, "NodeC");
    });

    // ===========
    // Act & Assert
    // ===========
    Assert.DoesNotThrow(() => pipeline.Build());
    Assert.That(pipeline.IsBuilt, Is.True);
  }
}
