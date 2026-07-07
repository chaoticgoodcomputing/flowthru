using Flowthru.Core.Tests.Storage;
using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// Tests for the streaming bulk-load capstone (#123): the FlowSinkItem output
/// and the on-DAG AddBulkLoad helper that wires a streaming source to a sink.
/// </summary>
[TestFixture]
public class AddBulkLoadTests
{
  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-bulkload-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
    }
  }

  [Test]
  public async Task FlowSinkItem_Save_CompilesSourceIntoSink()
  {
    var sink = new RecordingSink<int>(batchSize: 2);
    var item = new FlowSinkItem<int>("sink", sink);

    var result = await item.Save(FlowSource.FromEnumerable(new[] { 1, 2, 3, 4, 5 })).Run();

    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());
    Assert.That(sink.Rows, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    Assert.That(sink.Completed, Is.True);
  }

  [Test]
  public async Task FlowSinkItem_Load_Fails()
  {
    var item = new FlowSinkItem<int>("sink", new RecordingSink<int>(batchSize: 2));
    var result = await item.Load().Run();
    Assert.That(result, Is.InstanceOf<EffResult<FlowSource<int>>.Failure>());
  }

  [Test]
  public async Task AddBulkLoad_RunsOnDag_WritesAllRows()
  {
    // Eager-save a JSON array, then stream it into a sink via an on-DAG step.
    var path = Path.Combine(_tempDir, "orders.json");
    var item = ItemFactory.Enumerable.Json<TestRow>("orders", path);
    var rows = new[]
    {
      new TestRow { Id = 1, Name = "alpha" },
      new TestRow { Id = 2, Name = "beta" },
      new TestRow { Id = 3, Name = "gamma" },
    };
    await item.Save(rows).Run();

    var sink = new RecordingSink<TestRow>(batchSize: 2);
    var flow = FlowBuilder.CreateFlow("BulkLoadTest", p => p.AddBulkLoad(item.AsStream(), sink));

    var result = await flow.RunAsync();

    Assert.That(result.IsSuccess, Is.True, "The bulk-load flow should complete successfully.");
    Assert.That(sink.Rows.Select(r => r.Id), Is.EqualTo(new[] { 1, 2, 3 }));
    Assert.That(sink.Completed, Is.True);
  }

  private sealed class RecordingSink<T> : IFlowSink<T>
  {
    public RecordingSink(int batchSize) => BatchSize = batchSize;

    public List<T> Rows { get; } = new();
    public bool Completed { get; private set; }
    public int BatchSize { get; }

    public ValueTask OpenAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask WriteBatchAsync(IReadOnlyList<T> batch, CancellationToken cancellationToken)
    {
      Rows.AddRange(batch);
      return ValueTask.CompletedTask;
    }

    public ValueTask CompleteAsync(CancellationToken cancellationToken)
    {
      Completed = true;
      return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }
}
