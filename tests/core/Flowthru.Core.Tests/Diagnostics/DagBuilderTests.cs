using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Flow;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Diagnostics;

[TestFixture]
public class DagBuilderTests
{
  [Test]
  public async Task Build_ProducesBipartiteGraphWithItemAndStepNodes()
  {
    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var doubled = ItemFactory.Singleton.Memory<int>("doubled");
    var plusOne = ItemFactory.Singleton.Memory<int>("plusOne");
    await raw.Save(1).Run();

    var flow = FlowBuilder.CreateFlow("dag-test", b =>
    {
      b.AddStep<int, int>("double", x => x * 2, raw, doubled);
      b.AddStep<int, int>("plus-one", x => x + 1, doubled, plusOne);
    });

    var dag = DagBuilder.Build(flow);

    Assert.That(dag.FlowLabel, Is.EqualTo("dag-test"));
    Assert.That(dag.Items.Select(i => i.Label),
      Is.EquivalentTo(new[] { "raw", "doubled", "plusOne" }),
      "Item labels should be deduplicated even when an item appears as both an output and a downstream input.");
    Assert.That(dag.Steps.Select(s => s.Label),
      Is.EquivalentTo(new[] { "double", "plus-one" }));

    Assert.That(dag.Edges, Has.Some.Matches<DagEdge>(
      e => e.From == "raw" && e.To == "double" && e.Kind == DagEdgeKind.ItemToStep
    ));
    Assert.That(dag.Edges, Has.Some.Matches<DagEdge>(
      e => e.From == "double" && e.To == "doubled" && e.Kind == DagEdgeKind.StepToItem
    ));
  }
}
