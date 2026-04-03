using Flowthru.Flows;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestSteps;

namespace Flowthru.Tests.Validation.GraphConstruction;

/// <summary>
/// Tests verifying that multiple nodes writing to the same catalog entry are detected.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("GraphConstruction")]
public class MultipleWritersTests
{
  private SimpleThreeStepCatalog _catalog = null!;

  [SetUp]
  public void SetUp()
  {
    _catalog = new SimpleThreeStepCatalog();
  }

  [Test]
  public void Build_WhenTwoStepsWriteSameOutput_ThrowsInvalidOperationException()
  {
    // ===========
    // Arrange: Two nodes both write to StepOne
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
  public void Build_WhenThreeStepsWriteSameOutput_ThrowsInvalidOperationException()
  {
    // ===========
    // Arrange: Three nodes all write to Output
    // ===========
    var pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "StepA",
        transform: PassthroughStep.Create(),
        input: _catalog.Input,
        output: _catalog.Output
      );
      builder.AddStep(
        label: "StepB",
        transform: PassthroughStep.Create(),
        input: _catalog.StepOne,
        output: _catalog.Output
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
    Assert.Throws<InvalidOperationException>(() => pipeline.Build());
  }

  [Test]
  public void Build_WhenEachStepWritesDifferentOutput_SucceedsWithoutError()
  {
    // ===========
    // Arrange: Each node writes to a unique output
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
        input: _catalog.Input,
        output: _catalog.StepTwo
      );
      builder.AddStep(
        label: "StepC",
        transform: PassthroughStep.Create(),
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
  public void Build_WhenStepReadsWhatAnotherWrites_SucceedsWithoutError()
  {
    // ===========
    // Arrange: Linear pipeline where each node reads from previous node's output
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
