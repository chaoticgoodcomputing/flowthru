using System;
using System.Threading;
using System.Threading.Tasks;
using Flowthru.Data;
using Flowthru.Data.Storage;
using Flowthru.Effects;
using Flowthru.Pipelines;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestNodes;

namespace Flowthru.Tests.Execution;

/// <summary>
/// Tests verifying cancellation token propagation and handling throughout pipeline execution.
/// </summary>
[TestFixture]
[Category("Execution")]
[Category("Cancellation")]
public class CancellationTests
{
  [Test]
  public async Task RunAsync_WithAlreadyCancelledToken_ThrowsOperationCanceledException()
  {
    // ===========
    // Arrange
    // ===========
    var catalog = new SimpleThreeNodeCatalog();
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

    Pipeline pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "Passthrough",
        transform: PassthroughNode.Create(),
        input: catalog.Input,
        output: catalog.Output
      );
    });

    pipeline.Build();

    using var cts = new CancellationTokenSource();
    await cts.CancelAsync();

    // ===========
    // Act & Assert
    // ===========
    var exception = Assert.ThrowsAsync<OperationCanceledException>(
      async () => await pipeline.RunAsync(cts.Token)
    );
    Assert.That(exception, Is.Not.Null);
  }

  [Test]
  public async Task RunAsync_WithValidToken_CompletesSuccessfully()
  {
    // ===========
    // Arrange
    // ===========
    var catalog = new SimpleThreeNodeCatalog();
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

    Pipeline pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "Increment",
        transform: IncrementNode.Create(),
        input: catalog.Input,
        output: catalog.Output
      );
    });

    pipeline.Build();

    using var cts = new CancellationTokenSource();

    //===========
    // Act
    // ===========
    var result = await pipeline.RunAsync(CancellationToken.None);

    // ===========
    // Assert
    // ===========
    Assert.That(result.Success, Is.True, "Pipeline should complete successfully with valid token");
    Assert.That(result.Exception, Is.Null);

    var output = await catalog.Output.Load().Run();
    Assert.That(output.First().Id, Is.EqualTo(2), "Node should have executed");
  }

  [Test]
  public async Task RunAsync_CancelledBetweenNodes_ThrowsOperationCanceledException()
  {
    // ===========
    // Arrange
    // ===========
    var catalog = new SimpleThreeNodeCatalog();
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

    Pipeline pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      // First node: quick passthrough
      builder.AddNode(
        label: "FirstNode",
        transform: PassthroughNode.Create(),
        input: catalog.Input,
        output: catalog.StepOne
      );

      // Second node: delayed
      builder.AddNode(
        label: "SecondNode",
        transform: DelayedNode.Create(TimeSpan.FromSeconds(10)),
        input: catalog.StepOne,
        output: catalog.Output
      );
    });

    pipeline.Build();

    using var cts = new CancellationTokenSource();

    // ===========
    // Act
    // ===========
    // Run pipeline in background task and cancel shortly after first node completes
    var pipelineTask = pipeline.RunAsync(cts.Token);

    // Give first node time to complete, then cancel
    await Task.Delay(100);
    await cts.CancelAsync();

    // ===========
    // Assert
    // ===========
    var exception = Assert.ThrowsAsync<OperationCanceledException>(async () => await pipelineTask);
    Assert.That(exception, Is.Not.Null, "Pipeline should throw OperationCanceledException");
  }

  [Test]
  public async Task RunAsync_CancelledDuringNodeExecution_ThrowsOperationCanceledException()
  {
    // ===========
    // Arrange
    // ===========
    var catalog = new SimpleThreeNodeCatalog();
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

    Pipeline pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      // Node with long delay
      builder.AddNode(
        label: "LongRunningNode",
        transform: DelayedNode.Create(TimeSpan.FromSeconds(10)),
        input: catalog.Input,
        output: catalog.Output
      );
    });

    pipeline.Build();

    using var cts = new CancellationTokenSource();

    // ===========
    // Act
    // ===========
    // Start pipeline and cancel during execution
    var pipelineTask = pipeline.RunAsync(cts.Token);

    // Cancel shortly after start
    await Task.Delay(50);
    await cts.CancelAsync();

    // ===========
    // Assert
    // ===========
    var exception = Assert.ThrowsAsync<OperationCanceledException>(async () => await pipelineTask);
    Assert.That(exception, Is.Not.Null, "Pipeline should throw OperationCanceledException");
  }

  [Test]
  public async Task RunAsync_CancelledDuringIOLoad_ThrowsOperationCanceledException()
  {
    // ===========
    // Arrange
    // ===========
    var slowAdapter = new SlowLoadStorageAdapter<IEnumerable<TestData>>();
    var fastAdapter = new MemoryStorageAdapter<IEnumerable<TestData>>();
    var catalog = new TestCatalog();

    catalog.SlowData = new CatalogEntry<IEnumerable<TestData>>("SlowData", slowAdapter);
    catalog.FastData = new CatalogEntry<IEnumerable<TestData>>("FastData", fastAdapter);

    var testData = new[]
    {
      new TestData
      {
        Id = 1,
        Name = "Test",
        Value = 42.0,
      },
    };

    // Seed the slow adapter with data
    slowAdapter.SetData(testData);

    Pipeline pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "LoadFromSlowStorage",
        transform: PassthroughNode.Create(),
        input: catalog.SlowData,
        output: catalog.FastData
      );
    });

    pipeline.Build();

    using var cts = new CancellationTokenSource();

    // ===========
    // Act
    // ===========
    var pipelineTask = pipeline.RunAsync(cts.Token);

    // Cancel shortly after start, during the slow Load operation
    await Task.Delay(50);
    await cts.CancelAsync();

    // ===========
    // Assert
    // ===========
    var exception = Assert.ThrowsAsync<OperationCanceledException>(async () => await pipelineTask);
    Assert.That(exception, Is.Not.Null, "Pipeline should throw when IO is cancelled");
  }

  [Test]
  public async Task RunAsync_CancelledDuringIOSave_ThrowsOperationCanceledException()
  {
    // ===========
    // Arrange
    // ===========
    var slowAdapter = new SlowSaveStorageAdapter<IEnumerable<TestData>>();
    var fastAdapter = new MemoryStorageAdapter<IEnumerable<TestData>>();
    var catalog = new TestCatalog();

    catalog.FastData = new CatalogEntry<IEnumerable<TestData>>("FastData", fastAdapter);
    catalog.SlowData = new CatalogEntry<IEnumerable<TestData>>("SlowData", slowAdapter);

    var testData = new[]
    {
      new TestData
      {
        Id = 1,
        Name = "Test",
        Value = 42.0,
      },
    };
    await catalog.FastData.Save(testData).Run();

    Pipeline pipeline = PipelineBuilder.CreatePipeline(builder =>
    {
      builder.AddNode(
        label: "SaveToSlowStorage",
        transform: PassthroughNode.Create(),
        input: catalog.FastData,
        output: catalog.SlowData
      );
    });

    pipeline.Build();

    using var cts = new CancellationTokenSource();

    // ===========
    // Act
    // ===========
    var pipelineTask = pipeline.RunAsync(cts.Token);

    // Cancel shortly after start, during the slow Save operation
    await Task.Delay(50);
    await cts.CancelAsync();

    // ===========
    // Assert
    // ===========
    var exception = Assert.ThrowsAsync<OperationCanceledException>(async () => await pipelineTask);
    Assert.That(exception, Is.Not.Null, "Pipeline should throw when IO Save is cancelled");
  }

  /// <summary>
  /// Custom test catalog with additional properties for slow storage adapters.
  /// </summary>
  private class TestCatalog : DataCatalogBase
  {
    public CatalogEntry<IEnumerable<TestData>> SlowData { get; set; } = null!;
    public CatalogEntry<IEnumerable<TestData>> FastData { get; set; } = null!;
  }

  /// <summary>
  /// Storage adapter that simulates slow Load operations to test cancellation during I/O.
  /// </summary>
  private class SlowLoadStorageAdapter<T> : IStorageAdapter<T>
  {
    private T? _data;

    public void SetData(T data) => _data = data;

    public FlowIO<T> Load()
    {
      Func<CancellationToken, ValueTask<T>> loader = async (CancellationToken ct) =>
      {
        // Simulate slow I/O
        await Task.Delay(TimeSpan.FromSeconds(10), ct);
        return _data ?? throw new InvalidOperationException("No data");
      };
      return FlowIO.LiftAsync(loader);
    }

    public FlowIO<FlowUnit> Save(T data)
    {
      return FlowIO.Lift(() =>
      {
        _data = data;
        return FlowUnit.Default;
      });
    }

    public FlowIO<bool> Exists() => FlowIO.Lift(() => _data != null);
  }

  /// <summary>
  /// Storage adapter that simulates slow Save operations to test cancellation during I/O.
  /// </summary>
  private class SlowSaveStorageAdapter<T> : IStorageAdapter<T>
  {
    private T? _data;

    public FlowIO<T> Load()
    {
      return FlowIO.Lift(() => _data ?? throw new InvalidOperationException("No data"));
    }

    public FlowIO<FlowUnit> Save(T data)
    {
      Func<CancellationToken, ValueTask<FlowUnit>> saver = async (CancellationToken ct) =>
      {
        // Simulate slow I/O
        await Task.Delay(TimeSpan.FromSeconds(10), ct);
        _data = data;
        return FlowUnit.Default;
      };
      return FlowIO.LiftAsync(saver);
    }

    public FlowIO<bool> Exists() => FlowIO.Lift(() => _data != null);
  }
}
