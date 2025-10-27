using System.Linq;
using Flowthru.Pipelines;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestNodes;

namespace Flowthru.Tests.Execution.NodeExecution;

/// <summary>
/// Tests verifying correct execution behavior of single-input single-output nodes.
/// </summary>
[TestFixture]
[Category("Execution")]
[Category("NodeExecution")]
public class SingleInputOutputTests {
  [Test]
  public async Task Execute_WithPassthroughNode_PreservesData() {
    // ===========
    // Arrange
    // ===========
    var catalog = new SimpleThreeNodeCatalog();
    var testData = new[] { new TestData { Id = 1, Name = "Test", Value = 42.0 } };
    await catalog.Input.Save(testData).Run();

    var pipeline = PipelineBuilder.CreatePipeline(builder => {
      builder.AddNode<PassthroughNode>(catalog.Input, catalog.Output);
    });

    pipeline.Build();

    // ===========
    // Act
    // ===========
    await pipeline.RunAsync();

    // ===========
    // Assert
    // ===========
    var resultFin = await catalog.Output.Load().Run();
    var result = resultFin.Match(
        Succ: data => data.ToList(),
        Fail: error => throw new Exception($"Load failed: {error}")
    );
    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].Id, Is.EqualTo(1));
    Assert.That(result[0].Name, Is.EqualTo("Test"));
    Assert.That(result[0].Value, Is.EqualTo(42.0));
  }

  [Test]
  public async Task Execute_WithIncrementNode_IncrementsId() {
    // ===========
    // Arrange
    // ===========
    var catalog = new SimpleThreeNodeCatalog();
    var testData = new[] { new TestData { Id = 5, Name = "Test", Value = 10.0 } };
    await catalog.Input.Save(testData).Run();

    var pipeline = PipelineBuilder.CreatePipeline(builder => {
      builder.AddNode<IncrementNode>(catalog.Input, catalog.Output);
    });

    pipeline.Build();

    // ===========
    // Act
    // ===========
    await pipeline.RunAsync();

    // ===========
    // Assert
    // ===========
    var resultFin = await catalog.Output.Load().Run();
    var result = resultFin.Match(
        Succ: data => data.ToList(),
        Fail: error => throw new Exception($"Load failed: {error}")
    );
    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].Id, Is.EqualTo(6));
    Assert.That(result[0].Name, Is.EqualTo("Test"));
    Assert.That(result[0].Value, Is.EqualTo(10.0));
  }

  [Test]
  public async Task Execute_WithDoubleValueNode_DoublesValue() {
    // ===========
    // Arrange
    // ===========
    var catalog = new SimpleThreeNodeCatalog();
    var testData = new[] { new TestData { Id = 1, Name = "Test", Value = 21.0 } };
    await catalog.Input.Save(testData).Run();

    var pipeline = PipelineBuilder.CreatePipeline(builder => {
      builder.AddNode<DoubleValueNode>(catalog.Input, catalog.Output);
    });

    pipeline.Build();

    // ===========
    // Act
    // ===========
    await pipeline.RunAsync();

    // ===========
    // Assert
    // ===========
    var resultFin = await catalog.Output.Load().Run();
    var result = resultFin.Match(
        Succ: data => data.ToList(),
        Fail: error => throw new Exception($"Load failed: {error}")
    );
    Assert.That(result[0].Value, Is.EqualTo(42.0));
  }

  [Test]
  public async Task Execute_WithFailingNode_ThrowsExpectedException() {
    // ===========
    // Arrange
    // ===========
    var catalog = new SimpleThreeNodeCatalog();
    var testData = new[] { new TestData { Id = 1, Name = "Test", Value = 1.0 } };
    await catalog.Input.Save(testData).Run();

    var pipeline = PipelineBuilder.CreatePipeline(builder => {
      builder.AddNode<FailingNode>(catalog.Input, catalog.Output);
    });

    pipeline.Build();

    // ===========
    // Act
    // ===========
    var result = await pipeline.RunAsync();

    // ===========
    // Assert
    // ===========
    Assert.That(result.Success, Is.False, "Pipeline with failing node should not succeed");
    Assert.That(result.Exception, Is.Not.Null, "Failed pipeline should have an exception");
    Assert.That(result.Exception, Is.InstanceOf<InvalidOperationException>());
    Assert.That(result.Exception!.Message, Does.Contain("Test node failure"));
  }
}
