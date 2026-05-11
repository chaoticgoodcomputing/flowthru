using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;
using Flowthru.Flow;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// Tests for the variadic-input AddStep overload — the (N×1) reduce shape
/// where <em>N</em> is runtime-determined and all inputs share a type.
/// Distinct from the source-generated typed AddStep matrix, which handles
/// fixed-arity heterogeneous inputs.
/// </summary>
[TestFixture]
[Category("Execution")]
[Category("StepExecution")]
public class VariadicAddStepTests
{
  // ── Fixture: minimal flat row + per-test in-memory catalog ──────────

  [FlowthruSchema]
  public partial record Row
  {
    [SerializedLabel("id")]
    public required int Id { get; init; }

    [SerializedLabel("value")]
    public required string Value { get; init; }
  }

  /// <summary>
  /// Build N memory shards plus one master output. Each shard preloaded
  /// with one row whose <see cref="Row.Value"/> is the shard's letter.
  /// </summary>
  private static (List<IItem<IEnumerable<Row>>> shards, IItem<IEnumerable<Row>> master)
    MakeShardsAndMaster(int count)
  {
    var shards = new List<IItem<IEnumerable<Row>>>(count);
    for (var i = 0; i < count; i++)
    {
      shards.Add(Item.Of<IEnumerable<Row>>($"shard_{i}").Memory().Build());
    }
    var master = Item.Of<IEnumerable<Row>>("master").Memory().Build();
    return (shards, master);
  }

  private static async Task PreloadAsync(IItem<IEnumerable<Row>> shard, params Row[] rows)
  {
    var result = await shard.Save(rows).Run();
    if (result is EffResult<FlowUnit>.Failure f)
    {
      Assert.Fail($"Preload of '{shard.Label}' failed: {f.Error.Message}");
    }
  }

  private static async Task<List<Row>> LoadAsync(IItem<IEnumerable<Row>> item)
  {
    var result = await item.Load().Run();
    return result switch
    {
      EffResult<IEnumerable<Row>>.Success ok => ok.Value.ToList(),
      EffResult<IEnumerable<Row>>.Failure f =>
        throw new InvalidOperationException($"Load failed: {f.Error.Message}"),
      _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
    };
  }

  // ── Tests ───────────────────────────────────────────────────────────

  [Test]
  public async Task ThreeShards_ConcatPreservesAllRows()
  {
    var (shards, master) = MakeShardsAndMaster(3);
    await PreloadAsync(shards[0], new Row { Id = 1, Value = "A" });
    await PreloadAsync(shards[1], new Row { Id = 2, Value = "B" });
    await PreloadAsync(shards[2], new Row { Id = 3, Value = "C" });

    var flow = FlowBuilder.CreateFlow("VariadicTest", b =>
      b.AddStep<IEnumerable<Row>, IEnumerable<Row>>(
        label: "Concat",
        transform: batches => batches.SelectMany(b => b),
        inputs: shards,
        outputs: master
      )
    );

    var run = await flow.RunAsync();
    Assert.That(run.IsSuccess, Is.True,
      $"Run failed: {run.FirstFailure?.Error.Message ?? "<no failure?>"}");

    var result = await LoadAsync(master);
    Assert.That(result, Has.Count.EqualTo(3));
    Assert.That(result.Select(r => r.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));
    Assert.That(result.Select(r => r.Value), Is.EquivalentTo(new[] { "A", "B", "C" }));
  }

  [Test]
  public async Task SingleShard_BehavesLikeIdentity()
  {
    var (shards, master) = MakeShardsAndMaster(1);
    await PreloadAsync(shards[0], new Row { Id = 42, Value = "Solo" });

    var flow = FlowBuilder.CreateFlow("VariadicSingle", b =>
      b.AddStep<IEnumerable<Row>, IEnumerable<Row>>(
        label: "Concat",
        transform: batches => batches.SelectMany(b => b),
        inputs: shards,
        outputs: master
      )
    );

    var run = await flow.RunAsync();
    Assert.That(run.IsSuccess, Is.True,
      $"Run failed: {run.FirstFailure?.Error.Message ?? "<no failure?>"}");

    var result = await LoadAsync(master);
    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].Id, Is.EqualTo(42));
  }

  [Test]
  public async Task PreservesOrderInTransform()
  {
    var (shards, master) = MakeShardsAndMaster(4);
    await PreloadAsync(shards[0], new Row { Id = 10, Value = "first" });
    await PreloadAsync(shards[1], new Row { Id = 20, Value = "second" });
    await PreloadAsync(shards[2], new Row { Id = 30, Value = "third" });
    await PreloadAsync(shards[3], new Row { Id = 40, Value = "fourth" });

    var flow = FlowBuilder.CreateFlow("VariadicOrder", b =>
      b.AddStep<IEnumerable<Row>, IEnumerable<Row>>(
        label: "Concat",
        transform: batches => batches.SelectMany(b => b),
        inputs: shards,
        outputs: master
      )
    );

    var run = await flow.RunAsync();
    Assert.That(run.IsSuccess, Is.True,
      $"Run failed: {run.FirstFailure?.Error.Message ?? "<no failure?>"}");

    var result = await LoadAsync(master);
    // Order preservation: shard 0 → shard 1 → shard 2 → shard 3.
    Assert.That(result.Select(r => r.Value), Is.EqualTo(new[] { "first", "second", "third", "fourth" }));
  }

  [Test]
  public async Task AsyncTransform_AwaitsCorrectly()
  {
    var (shards, master) = MakeShardsAndMaster(2);
    await PreloadAsync(shards[0], new Row { Id = 1, Value = "X" });
    await PreloadAsync(shards[1], new Row { Id = 2, Value = "Y" });

    var flow = FlowBuilder.CreateFlow("VariadicAsync", b =>
      b.AddStep<IEnumerable<Row>, IEnumerable<Row>>(
        label: "ConcatAsync",
        transform: async batches =>
        {
          await Task.Yield();
          return batches.SelectMany(b => b).ToList();
        },
        inputs: shards,
        outputs: master
      )
    );

    var run = await flow.RunAsync();
    Assert.That(run.IsSuccess, Is.True,
      $"Run failed: {run.FirstFailure?.Error.Message ?? "<no failure?>"}");

    var result = await LoadAsync(master);
    Assert.That(result, Has.Count.EqualTo(2));
  }

  [Test]
  public async Task AsyncWithCancellation_PassesTokenThrough()
  {
    var (shards, master) = MakeShardsAndMaster(2);
    await PreloadAsync(shards[0], new Row { Id = 1, Value = "P" });
    await PreloadAsync(shards[1], new Row { Id = 2, Value = "Q" });

    var receivedNonDefaultToken = false;
    var flow = FlowBuilder.CreateFlow("VariadicCt", b =>
      b.AddStep<IEnumerable<Row>, IEnumerable<Row>>(
        label: "ConcatCt",
        transform: async (batches, ct) =>
        {
          // The runtime should pass through a real token; we just observe
          // that the lambda receives one without blowing up.
          receivedNonDefaultToken = ct != default;
          await Task.Yield();
          return batches.SelectMany(b => b).ToList();
        },
        inputs: shards,
        outputs: master
      )
    );

    using var cts = new CancellationTokenSource();
    var run = await flow.RunAsync(cts.Token);
    Assert.That(run.IsSuccess, Is.True,
      $"Run failed: {run.FirstFailure?.Error.Message ?? "<no failure?>"}");
    Assert.That(receivedNonDefaultToken, Is.True,
      "transform should receive the cancellation token threaded by the runtime");

    var result = await LoadAsync(master);
    Assert.That(result, Has.Count.EqualTo(2));
  }

  [Test]
  public void NullArgs_Throw()
  {
    var (shards, master) = MakeShardsAndMaster(1);

    var builder = new FlowBuilderProbe();

    Assert.Throws<ArgumentNullException>(() =>
      builder.Inner.AddStep<IEnumerable<Row>, IEnumerable<Row>>(
        label: null!,
        transform: batches => batches.SelectMany(b => b),
        inputs: shards,
        outputs: master
      ));

    Assert.Throws<ArgumentNullException>(() =>
      builder.Inner.AddStep<IEnumerable<Row>, IEnumerable<Row>>(
        label: "x",
        transform: (Func<IEnumerable<IEnumerable<Row>>, IEnumerable<Row>>)null!,
        inputs: shards,
        outputs: master
      ));

    Assert.Throws<ArgumentNullException>(() =>
      builder.Inner.AddStep<IEnumerable<Row>, IEnumerable<Row>>(
        label: "x",
        transform: batches => batches.SelectMany(b => b),
        inputs: null!,
        outputs: master
      ));

    Assert.Throws<ArgumentNullException>(() =>
      builder.Inner.AddStep<IEnumerable<Row>, IEnumerable<Row>>(
        label: "x",
        transform: batches => batches.SelectMany(b => b),
        inputs: shards,
        outputs: null!
      ));
  }

  /// <summary>
  /// Acquire a real <see cref="FlowBuilder"/> via <see cref="FlowBuilder.CreateFlow"/>'s
  /// configure callback, since the constructor is internal.
  /// </summary>
  private sealed class FlowBuilderProbe
  {
    public FlowBuilder Inner { get; }
    public FlowBuilderProbe()
    {
      FlowBuilder? captured = null;
      // CreateFlow runs the configure callback synchronously, so we can grab the builder.
      try
      {
        FlowBuilder.CreateFlow("probe", b =>
        {
          captured = b;
          // Add a dummy step so Build() doesn't fail on empty.
          b.AddStep(label: "noop", transform: () => { });
        });
      }
      catch
      {
        // Build may fail; we already captured the builder reference.
      }
      Inner = captured ?? throw new InvalidOperationException("Failed to capture FlowBuilder");
    }
  }
}
