using System;
using System.Threading;
using System.Threading.Tasks;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Core.Flows;
using Flowthru.Core.Tests.Fixtures.TestCatalogs;
using Flowthru.Core.Tests.Fixtures.TestSteps;

namespace Flowthru.Core.Tests.Execution;

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

    Flow pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "Passthrough",
        transform: PassthroughStep.Create(),
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

    Flow pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "Increment",
        transform: IncrementStep.Create(),
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
    Assert.That(result.Success, Is.True, "Flow should complete successfully with valid token");
    Assert.That(result.Exception, Is.Null);

    var output = await catalog.Output.Load().Run();
    Assert.That(output.First().Id, Is.EqualTo(2), "Step should have executed");
  }

  [Test]
  public async Task RunAsync_CancelledBetweenSteps_ThrowsOperationCanceledException()
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

    Flow pipeline = FlowBuilder.CreateFlow(builder =>
    {
      // First node: quick passthrough
      builder.AddStep(
        label: "FirstStep",
        transform: PassthroughStep.Create(),
        input: catalog.Input,
        output: catalog.StepOne
      );

      // Second node: delayed
      builder.AddStep(
        label: "SecondStep",
        transform: DelayedStep.Create(TimeSpan.FromSeconds(10)),
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
    Assert.That(exception, Is.Not.Null, "Flow should throw OperationCanceledException");
  }

  [Test]
  public async Task RunAsync_CancelledDuringStepExecution_ThrowsOperationCanceledException()
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

    Flow pipeline = FlowBuilder.CreateFlow(builder =>
    {
      // Step with long delay
      builder.AddStep(
        label: "LongRunningStep",
        transform: DelayedStep.Create(TimeSpan.FromSeconds(10)),
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
    Assert.That(exception, Is.Not.Null, "Flow should throw OperationCanceledException");
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

    catalog.SlowData = new Item<IEnumerable<TestData>>("SlowData", slowAdapter);
    catalog.FastData = new Item<IEnumerable<TestData>>("FastData", fastAdapter);

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

    Flow pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "LoadFromSlowStorage",
        transform: PassthroughStep.Create(),
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
    Assert.That(exception, Is.Not.Null, "Flow should throw when IO is cancelled");
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

    catalog.FastData = new Item<IEnumerable<TestData>>("FastData", fastAdapter);
    catalog.SlowData = new Item<IEnumerable<TestData>>("SlowData", slowAdapter);

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

    Flow pipeline = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        label: "SaveToSlowStorage",
        transform: PassthroughStep.Create(),
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
    Assert.That(exception, Is.Not.Null, "Flow should throw when IO Save is cancelled");
  }

  /// <summary>
  /// Custom test catalog with additional properties for slow storage adapters.
  /// </summary>
  private class TestCatalog : CatalogAbstract
  {
    public Item<IEnumerable<TestData>> SlowData { get; set; } = null!;
    public Item<IEnumerable<TestData>> FastData { get; set; } = null!;
  }

  /// <summary>
  /// Storage adapter that simulates slow Load operations to test cancellation during I/O.
  /// </summary>
  private class SlowLoadStorageAdapter<T> : IStorageAdapter<T>
  {
    private T? _data;

    public StorageTraits Traits => new StorageTraits();

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

    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
  }

  /// <summary>
  /// Storage adapter that simulates slow Save operations to test cancellation during I/O.
  /// </summary>
  private class SlowSaveStorageAdapter<T> : IStorageAdapter<T>
  {
    private T? _data;

    public StorageTraits Traits => new StorageTraits();

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

    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
  }
}
