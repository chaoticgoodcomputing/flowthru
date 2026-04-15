using Flowthru.Core.Data;
using Flowthru.Core.Flows;
using Flowthru.Tests.Fixtures.TestCatalogs;

namespace Flowthru.Tests.Execution.StepExecution;

/// <summary>
/// Tests verifying correct execution of homogeneous fan-in nodes — the pattern where
/// N catalog entries of the same type collapse into a single aggregating node.
/// </summary>
[TestFixture]
[Category("Execution")]
[Category("StepExecution")]
public class FanInStepTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Execution
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task AddStep_FanIn_ReceivesAllInputCollections()
  {
    // Arrange — three shards, each with one row of data
    var shardA = new ShardCatalog("A");
    var shardB = new ShardCatalog("B");
    var shardC = new ShardCatalog("C");
    var master = new MasterCatalog();

    await shardA
      .ShardData.Save(
        [
          new TestData
          {
            Id = 1,
            Name = "A",
            Value = 1.0,
          },
        ]
      )
      .Run();
    await shardB
      .ShardData.Save(
        [
          new TestData
          {
            Id = 2,
            Name = "B",
            Value = 2.0,
          },
        ]
      )
      .Run();
    await shardC
      .ShardData.Save(
        [
          new TestData
          {
            Id = 3,
            Name = "C",
            Value = 3.0,
          },
        ]
      )
      .Run();

    var inputs = new List<IItem<IEnumerable<TestData>>>
    {
      shardA.ShardData,
      shardB.ShardData,
      shardC.ShardData,
    };

    var pipeline = FlowBuilder.CreateFlow(b =>
      b.AddStep(
        label: "Append",
        inputs: inputs,
        output: master.AllData,
        step: batches => batches.SelectMany(x => x)
      )
    );

    pipeline.Build();

    // Act
    await pipeline.RunAsync(CancellationToken.None);

    // Assert — all three rows present in master catalog
    var result = (await master.AllData.Load().Run()).ToList();
    Assert.That(result, Has.Count.EqualTo(3));
    Assert.That(result.Select(r => r.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));
  }

  [Test]
  public async Task AddStep_FanIn_SingleInput_BehavesLikeRegularStep()
  {
    // Edge case: fan-in with exactly one input should still work correctly.
    var shard = new ShardCatalog("only");
    var master = new MasterCatalog();

    var testData = new[]
    {
      new TestData
      {
        Id = 42,
        Name = "Solo",
        Value = 9.9,
      },
    };
    await shard.ShardData.Save(testData).Run();

    var pipeline = FlowBuilder.CreateFlow(b =>
      b.AddStep(
        label: "PassthroughFanIn",
        inputs: [shard.ShardData],
        output: master.AllData,
        step: batches => batches.SelectMany(x => x)
      )
    );

    pipeline.Build();
    await pipeline.RunAsync(CancellationToken.None);

    var result = (await master.AllData.Load().Run()).ToList();
    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].Id, Is.EqualTo(42));
  }

  [Test]
  public async Task AddStep_FanIn_DagResolvesEdges_WhenUpstreamFlowProducesShards()
  {
    // Arrange — one upstream pipeline writes to two shards; fan-in pipeline merges them.
    var shardA = new ShardCatalog("dag_a");
    var shardB = new ShardCatalog("dag_b");
    var master = new MasterCatalog();

    var source = new SimpleThreeStepCatalog();
    await source
      .Input.Save(
        [
          new TestData
          {
            Id = 10,
            Name = "Ten",
            Value = 10.0,
          },
          new TestData
          {
            Id = 20,
            Name = "Twenty",
            Value = 20.0,
          },
        ]
      )
      .Run();

    // Flow 1: distribute input across the two shards
    var distributor = FlowBuilder.CreateFlow(b =>
    {
      b.AddStep(
        label: "ToShardA",
        transform: (IEnumerable<TestData> data) => data.Where(x => x.Id == 10),
        input: source.Input,
        output: shardA.ShardData
      );
      b.AddStep(
        label: "ToShardB",
        transform: (IEnumerable<TestData> data) => data.Where(x => x.Id == 20),
        input: source.Input,
        output: shardB.ShardData
      );
    });

    // Flow 2: merge both shards into master
    var merger = FlowBuilder.CreateFlow(b =>
      b.AddStep(
        label: "Merge",
        inputs: [shardA.ShardData, shardB.ShardData],
        output: master.AllData,
        step: batches => batches.SelectMany(x => x)
      )
    );

    var merged = Flow.Merge(
      new Dictionary<string, Flow> { ["distributor"] = distributor, ["merger"] = merger }
    );
    merged.Build();

    // Act
    await merged.RunAsync(CancellationToken.None);

    // Assert — both rows arrived in master
    var result = (await master.AllData.Load().Run()).ToList();
    Assert.That(result, Has.Count.EqualTo(2));
    Assert.That(result.Select(r => r.Id), Is.EquivalentTo(new[] { 10, 20 }));
  }
}
