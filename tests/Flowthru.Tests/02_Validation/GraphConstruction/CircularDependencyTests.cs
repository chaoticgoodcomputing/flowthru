using Flowthru.Flows;
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
    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "NodeA",
        transform: PassthroughNode.Create(),
        input: _catalog.Input,
        output: _catalog.StepOne
      );
      builder.AddStep(
        label: "NodeB",
        transform: PassthroughNode.Create(),
        input: _catalog.StepOne,
        output: _catalog.StepTwo
      );
      builder.AddStep(
        label: "NodeC",
        transform: PassthroughNode.Create(),
        input: _catalog.StepTwo,
        output: _catalog.Input
      ); // Circle!
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
  public void Build_WhenSelfLoop_IsAllowed()
  {
    // ===========
    // Arrange: Node writes to its own input (A → A)
    // ===========
    // Note: Self-loops are currently allowed in the implementation
    // as they can represent update-in-place operations
    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "SelfLoop",
        transform: PassthroughNode.Create(),
        input: _catalog.Input,
        output: _catalog.Input
      );
    });

    // ===========
    // Act & Assert
    // ===========
    Assert.DoesNotThrow(() => pipeline.Build());
    Assert.That(pipeline.IsBuilt, Is.True);
  }

  [Test]
  public void Build_WhenTwoNodeCircle_ThrowsInvalidOperationException()
  {
    // ===========
    // Arrange: A → B → A
    // ===========
    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "NodeA",
        transform: PassthroughNode.Create(),
        input: _catalog.Input,
        output: _catalog.StepOne
      );
      builder.AddStep(
        label: "NodeB",
        transform: PassthroughNode.Create(),
        input: _catalog.StepOne,
        output: _catalog.Input
      ); // Circle back
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
    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "NodeA",
        transform: PassthroughNode.Create(),
        input: _catalog.Input,
        output: _catalog.StepOne
      );
      builder.AddStep(
        label: "NodeB",
        transform: PassthroughNode.Create(),
        input: _catalog.StepOne,
        output: _catalog.StepTwo
      );
      builder.AddStep(
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
