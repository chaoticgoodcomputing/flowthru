using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// DAG-construction invariants for the single-producer law (§2.4),
/// ported from the legacy
/// <c>02_Validation/GraphConstruction/MultipleWritersTests</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>StubFlowEndToEndTests.DependencyAnalyzerRejectsDuplicateProducer</c>
/// covers the two-writer happy-path rejection;
/// <c>PreFlightEdgeCaseTests.TwoStepsWritingSameOutput_…</c> asserts the
/// message names both contenders. These granular tests pin the
/// remaining cases the gap analysis flagged as missing:
/// </para>
/// <list type="bullet">
///   <item>Three-writer conflicts surface the same
///     <see cref="DependencyAnalyzer.Result.DuplicateProducer"/> failure
///     as the two-writer case, and the error names the contested item.</item>
///   <item>A clean pipeline where every step writes a distinct item
///     builds successfully — keeps the conflict assertions honest.</item>
///   <item>A linear chain that consumes its predecessor's output (read-
///     and-write of the <em>same</em> item by different steps in a
///     chain) is not a conflict — only two <em>writers</em> trigger
///     the rule.</item>
/// </list>
/// </remarks>
[TestFixture]
public class MultipleWritersTests
{
  [Test]
  public void Build_WhenTwoStepsWriteSameOutput_ThrowsWithItemAndStepLabels()
  {
    var input = ItemFactory.Singleton.Memory<int>("two-writers-input");
    var shared = ItemFactory.Singleton.Memory<int>("two-writers-shared");

    var ex = Assert.Throws<FlowBuildException>(() =>
      FlowBuilder.CreateFlow("two-writers", builder =>
      {
        builder.AddStep<int, int>("first", x => x + 1, input, shared);
        builder.AddStep<int, int>("second", x => x + 2, input, shared);
      })
    );
    Assert.That(ex!.Message, Does.Contain("two-writers-shared"),
      "Duplicate-producer message should name the contested item.");
    Assert.That(ex.Message, Does.Contain("first"));
    Assert.That(ex.Message, Does.Contain("second"));
  }

  [Test]
  public void Build_WhenThreeStepsWriteSameOutput_ThrowsWithAllStepLabels()
  {
    // The duplicate-accumulator in DependencyAnalyzer.Analyse must
    // collect every offender, not just the first pair.
    var inputA = ItemFactory.Singleton.Memory<int>("three-writers-input-a");
    var inputB = ItemFactory.Singleton.Memory<int>("three-writers-input-b");
    var inputC = ItemFactory.Singleton.Memory<int>("three-writers-input-c");
    var shared = ItemFactory.Singleton.Memory<int>("three-writers-shared");

    var ex = Assert.Throws<FlowBuildException>(() =>
      FlowBuilder.CreateFlow("three-writers", builder =>
      {
        builder.AddStep<int, int>("alpha", x => x + 1, inputA, shared);
        builder.AddStep<int, int>("beta", x => x + 2, inputB, shared);
        builder.AddStep<int, int>("gamma", x => x + 3, inputC, shared);
      })
    );
    Assert.That(ex!.Message, Does.Contain("three-writers-shared"));
    Assert.That(ex.Message, Does.Contain("alpha"));
    Assert.That(ex.Message, Does.Contain("beta"));
    Assert.That(ex.Message, Does.Contain("gamma"),
      "Every offending step must be reported, not just the first pair."
    );
  }

  [Test]
  public void Build_WhenEachStepWritesDistinctOutput_SucceedsWithoutError()
  {
    // Negative control: ensures the rule is specific to shared outputs,
    // not any shared input.
    var input = ItemFactory.Singleton.Memory<int>("distinct-input");
    var outA = ItemFactory.Singleton.Memory<int>("distinct-out-a");
    var outB = ItemFactory.Singleton.Memory<int>("distinct-out-b");
    var outC = ItemFactory.Singleton.Memory<int>("distinct-out-c");

    var flow = FlowBuilder.CreateFlow("distinct-writers", builder =>
    {
      builder.AddStep<int, int>("a", x => x + 1, input, outA);
      builder.AddStep<int, int>("b", x => x + 2, input, outB);
      builder.AddStep<int, int>("c", x => x + 3, input, outC);
    });

    Assert.That(flow.Steps, Has.Count.EqualTo(3));
  }

  [Test]
  public void Build_WhenStepReadsWhatAnotherWrites_SucceedsWithoutError()
  {
    // The rule rejects multiple writers, not multiple readers — a
    // linear chain that re-reads each predecessor's output is legal.
    var input = ItemFactory.Singleton.Memory<int>("chain-input");
    var stepOne = ItemFactory.Singleton.Memory<int>("chain-step-one");
    var stepTwo = ItemFactory.Singleton.Memory<int>("chain-step-two");
    var output = ItemFactory.Singleton.Memory<int>("chain-output");

    var flow = FlowBuilder.CreateFlow("read-after-write", builder =>
    {
      builder.AddStep<int, int>("write-one", x => x + 1, input, stepOne);
      builder.AddStep<int, int>("read-and-write-two", x => x + 1, stepOne, stepTwo);
      builder.AddStep<int, int>("read-and-write-final", x => x + 1, stepTwo, output);
    });

    Assert.That(flow.Steps, Has.Count.EqualTo(3));
  }
}
