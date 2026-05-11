using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// DAG-construction invariants for the cycle-detection surface, ported
/// from the legacy <c>02_Validation/GraphConstruction/CircularDependencyTests</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>StubFlowEndToEndTests.DependencyAnalyzerRejectsCycle</c> covers the
/// happy-path two-step rejection; <c>PreFlightEdgeCaseTests.TwoStepCycle_…</c>
/// and <c>ThreeStepCycle_…</c> assert message shape. These granular tests
/// pin the remaining cases that the gap analysis flagged as missing:
/// </para>
/// <list type="bullet">
///   <item>Self-loops (a step whose only input equals its only output)
///     are intentionally allowed by the analyser — see
///     <see cref="Flowthru.Flow.DependencyAnalyzer.Analyse"/>'s
///     <c>producerIndex != i</c> guard. This pins that as a contract,
///     not an accident.</item>
///   <item>Two-step vs three-step cycles are reported as distinct
///     <c>CycleDetected</c> results with the right step set in the cycle
///     walk.</item>
///   <item>A linear chain of the same arity (no cycle) builds cleanly,
///     to keep the cycle assertions honest.</item>
/// </list>
/// </remarks>
[TestFixture]
public class CircularDependencyTests
{
  [Test]
  public void Build_WhenSelfLoop_IsAllowed()
  {
    // The producer-index guard in DependencyAnalyzer.Analyse skips the
    // self-edge before cycle detection runs, modelling "update-in-place"
    // steps where the output replaces the input.
    var item = ItemFactory.Singleton.Memory<int>("self-loop-item");

    Assert.DoesNotThrow(() =>
      FlowBuilder.CreateFlow("self-loop", builder =>
      {
        builder.AddStep<int, int>("update-in-place", x => x + 1, item, item);
      })
    );
  }

  [Test]
  public void Build_WhenTwoStepCycle_ThrowsFlowBuildException()
  {
    var a = ItemFactory.Singleton.Memory<int>("two-cycle-a");
    var b = ItemFactory.Singleton.Memory<int>("two-cycle-b");

    var ex = Assert.Throws<FlowBuildException>(() =>
      FlowBuilder.CreateFlow("two-cycle", builder =>
      {
        builder.AddStep<int, int>("a-to-b", x => x, a, b);
        builder.AddStep<int, int>("b-to-a", x => x, b, a);
      })
    );
    Assert.That(ex!.Message, Does.Contain("Cycle detected"));
    Assert.That(ex.Message, Does.Contain("a-to-b"));
    Assert.That(ex.Message, Does.Contain("b-to-a"));
  }

  [Test]
  public void Build_WhenThreeStepCycle_ThrowsFlowBuildException()
  {
    // Distinct from the two-step case: the cycle walk must traverse
    // three nodes and surface all three labels.
    var a = ItemFactory.Singleton.Memory<int>("three-cycle-a");
    var b = ItemFactory.Singleton.Memory<int>("three-cycle-b");
    var c = ItemFactory.Singleton.Memory<int>("three-cycle-c");

    var ex = Assert.Throws<FlowBuildException>(() =>
      FlowBuilder.CreateFlow("three-cycle", builder =>
      {
        builder.AddStep<int, int>("step-a", x => x, a, b);
        builder.AddStep<int, int>("step-b", x => x, b, c);
        builder.AddStep<int, int>("step-c", x => x, c, a);
      })
    );
    Assert.That(ex!.Message, Does.Contain("Cycle detected"));
    Assert.That(ex.Message, Does.Contain("step-a"));
    Assert.That(ex.Message, Does.Contain("step-b"));
    Assert.That(ex.Message, Does.Contain("step-c"));
  }

  [Test]
  public void Build_WhenLinearChain_SucceedsWithoutError()
  {
    // Negative control: keeps the cycle assertions honest by proving
    // detection is specific, not accidentally triggered by any 3-step flow.
    var input = ItemFactory.Singleton.Memory<int>("linear-input");
    var stepOne = ItemFactory.Singleton.Memory<int>("linear-step-one");
    var stepTwo = ItemFactory.Singleton.Memory<int>("linear-step-two");
    var output = ItemFactory.Singleton.Memory<int>("linear-output");

    var flow = FlowBuilder.CreateFlow("linear", builder =>
    {
      builder.AddStep<int, int>("a", x => x + 1, input, stepOne);
      builder.AddStep<int, int>("b", x => x + 1, stepOne, stepTwo);
      builder.AddStep<int, int>("c", x => x + 1, stepTwo, output);
    });

    Assert.That(flow.Steps.Select(s => s.Label), Is.EqualTo(new[] { "a", "b", "c" }),
      "Topological order should follow the data-flow direction.");
  }
}
