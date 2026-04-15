using System.Linq;
using Flowthru.Core.Flows;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestSteps;

namespace Flowthru.Tests.Execution.StepExecution;

/// <summary>
/// Tests verifying correct execution behavior of single-input single-output nodes.
/// </summary>
[TestFixture]
[Category("Execution")]
[Category("StepExecution")]
public class SingleInputOutputTests
{
    [Test]
    public async Task Execute_WithPassthroughStep_PreservesData()
    {
        // ===========
        // Arrange
        // ===========
        var catalog = new SimpleThreeStepCatalog();
        var testData = new[]
        {
      new TestData
      {
        Id = 1,
        Name = "Test",
        Value = 42.0,
      },
    };
        await catalog.Input.Save(testData).Run();

        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Passthrough",
          transform: PassthroughStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        pipeline.Build();

        // ===========
        // Act
        // ===========
        await pipeline.RunAsync(CancellationToken.None);

        // ===========
        // Assert
        // ===========
        var result = await catalog.Output.Load().Run();
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(1));
        Assert.That(resultList[0].Id, Is.EqualTo(1));
        Assert.That(resultList[0].Name, Is.EqualTo("Test"));
        Assert.That(resultList[0].Value, Is.EqualTo(42.0));
    }

    [Test]
    public async Task Execute_WithIncrementStep_IncrementsId()
    {
        // ===========
        // Arrange
        // ===========
        var catalog = new SimpleThreeStepCatalog();
        var testData = new[]
        {
      new TestData
      {
        Id = 5,
        Name = "Test",
        Value = 10.0,
      },
    };
        await catalog.Input.Save(testData).Run();

        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Increment",
          transform: IncrementStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        pipeline.Build();

        // ===========
        // Act
        // ===========
        await pipeline.RunAsync(CancellationToken.None);

        // ===========
        // Assert
        // ===========
        var result = await catalog.Output.Load().Run();
        var resultList = result.ToList();
        Assert.That(resultList, Has.Count.EqualTo(1));
        Assert.That(resultList[0].Id, Is.EqualTo(6));
        Assert.That(resultList[0].Name, Is.EqualTo("Test"));
        Assert.That(resultList[0].Value, Is.EqualTo(10.0));
    }

    [Test]
    public async Task Execute_WithDoubleValueStep_DoublesValue()
    {
        // ===========
        // Arrange
        // ===========
        var catalog = new SimpleThreeStepCatalog();
        var testData = new[]
        {
      new TestData
      {
        Id = 1,
        Name = "Test",
        Value = 21.0,
      },
    };
        await catalog.Input.Save(testData).Run();

        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "DoubleValue",
          transform: DoubleValueStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        pipeline.Build();

        // ===========
        // Act
        // ===========
        await pipeline.RunAsync(CancellationToken.None);

        // ===========
        // Assert
        // ===========
        var result = await catalog.Output.Load().Run();
        var resultList = result.ToList();
        Assert.That(resultList[0].Value, Is.EqualTo(42.0));
    }

    [Test]
    public async Task Execute_WithFailingStep_ThrowsExpectedException()
    {
        // ===========
        // Arrange
        // ===========
        var catalog = new SimpleThreeStepCatalog();
        var testData = new[]
        {
      new TestData
      {
        Id = 1,
        Name = "Test",
        Value = 1.0,
      },
    };
        await catalog.Input.Save(testData).Run();

        var pipeline = FlowBuilder.CreateFlow(builder =>
        {
            builder.AddStep(
          label: "Failing",
          transform: FailingStep.Create(),
          input: catalog.Input,
          output: catalog.Output
        );
        });

        pipeline.Build();

        // ===========
        // Act
        // ===========
        var result = await pipeline.RunAsync(CancellationToken.None);

        // ===========
        // Assert
        // ===========
        Assert.That(result.Success, Is.False, "Flow with failing node should not succeed");
        Assert.That(result.Exception, Is.Not.Null, "Failed pipeline should have an exception");
        Assert.That(result.Exception, Is.InstanceOf<InvalidOperationException>());
        Assert.That(result.Exception!.Message, Does.Contain("Test step failure"));
    }
}
