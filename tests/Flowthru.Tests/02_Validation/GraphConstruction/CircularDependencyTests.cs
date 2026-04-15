using Flowthru.Core.Flows;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestSteps;

namespace Flowthru.Tests.Validation.GraphConstruction;

/// <summary>
/// Tests verifying that circular dependencies are detected during Flow.Build().
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("GraphConstruction")]
public class CircularDependencyTests
{
    private SimpleThreeStepCatalog _catalog = null!;

    [SetUp]
    public void SetUp()
    {
        _catalog = new SimpleThreeStepCatalog();
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
          label: "StepA",
          transform: PassthroughStep.Create(),
          input: _catalog.Input,
          output: _catalog.StepOne
        );
            builder.AddStep(
          label: "StepB",
          transform: PassthroughStep.Create(),
          input: _catalog.StepOne,
          output: _catalog.StepTwo
        );
            builder.AddStep(
          label: "StepC",
          transform: PassthroughStep.Create(),
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
        // Arrange: Step writes to its own input (A → A)
        // ===========
        // Note: Self-loops are currently allowed in the implementation
        // as they can represent update-in-place operations
        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "SelfLoop",
          transform: PassthroughStep.Create(),
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
    public void Build_WhenTwoStepCircle_ThrowsInvalidOperationException()
    {
        // ===========
        // Arrange: A → B → A
        // ===========
        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "StepA",
          transform: PassthroughStep.Create(),
          input: _catalog.Input,
          output: _catalog.StepOne
        );
            builder.AddStep(
          label: "StepB",
          transform: PassthroughStep.Create(),
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
          label: "StepA",
          transform: PassthroughStep.Create(),
          input: _catalog.Input,
          output: _catalog.StepOne
        );
            builder.AddStep(
          label: "StepB",
          transform: PassthroughStep.Create(),
          input: _catalog.StepOne,
          output: _catalog.StepTwo
        );
            builder.AddStep(
          label: "StepC",
          transform: PassthroughStep.Create(),
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
