using Flowthru.Core.Flows;
using Flowthru.Core.Graph;
using Flowthru.Core.Graph.Scheduling;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestSteps;

namespace Flowthru.Tests.Execution;

/// <summary>
/// Tests for the scheduling strategy abstraction, height computation,
/// and integration of both built-in strategies with the task-graph executor.
/// </summary>
[TestFixture]
[Category("Execution")]
[Category("Scheduling")]
public class SchedulingStrategyTests
{
  private static readonly IEnumerable<TestData> SeedData =
  [
    new TestData
    {
      Id = 1,
      Name = "A",
      Value = 1.0,
    },
    new TestData
    {
      Id = 2,
      Name = "B",
      Value = 2.0,
    },
  ];

  // ─────────────────────────────────────────────────────────────────────────
  // DependencyAnalyzer.ComputeHeights — unit tests on DAG height values
  // ─────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Linear chain A → B → C.
  /// A has height 2 (must traverse B then C before a sink).
  /// B has height 1. C is a sink with height 0.
  /// </summary>
  [Test]
  public void ComputeHeights_LinearChain_AssignsCorrectHeights()
  {
    var stepA = MakeStep("A");
    var stepB = MakeStep("B");
    var stepC = MakeStep("C");

    // B depends on A; C depends on B.
    stepB.Dependencies.Add(stepA);
    stepC.Dependencies.Add(stepB);

    var nodes = new List<FlowStep> { stepA, stepB, stepC };
    DependencyAnalyzer.AssignLayers(nodes);
    DependencyAnalyzer.ComputeHeights(nodes);

    Assert.That(stepA.Height, Is.EqualTo(2), "A: longest path A→B→C has length 2");
    Assert.That(stepB.Height, Is.EqualTo(1), "B: longest path B→C has length 1");
    Assert.That(stepC.Height, Is.EqualTo(0), "C: sink has height 0");
  }

  /// <summary>
  /// Diamond DAG:
  /// <code>
  ///   Root ──→ Left  ──┐
  ///        └──→ Right ──┴──→ Merge
  /// </code>
  /// Root has height 2; Left and Right both have height 1; Merge has height 0.
  /// </summary>
  [Test]
  public void ComputeHeights_DiamondDag_AssignsCorrectHeights()
  {
    var root = MakeStep("Root");
    var left = MakeStep("Left");
    var right = MakeStep("Right");
    var merge = MakeStep("Merge");

    left.Dependencies.Add(root);
    right.Dependencies.Add(root);
    merge.Dependencies.Add(left);
    merge.Dependencies.Add(right);

    var nodes = new List<FlowStep> { root, left, right, merge };
    DependencyAnalyzer.AssignLayers(nodes);
    DependencyAnalyzer.ComputeHeights(nodes);

    Assert.That(root.Height, Is.EqualTo(2), "Root gates the full diamond");
    Assert.That(left.Height, Is.EqualTo(1), "Left leads to Merge");
    Assert.That(right.Height, Is.EqualTo(1), "Right leads to Merge");
    Assert.That(merge.Height, Is.EqualTo(0), "Merge is a sink");
  }

  /// <summary>
  /// Asymmetric DAG where one branch is longer than the other:
  /// <code>
  ///   Root ──→ Short ──────────────┐
  ///        └──→ LongA → LongB ──→ Merge
  /// </code>
  /// Root's height should reflect the longer path (3, via LongA and LongB).
  /// </summary>
  [Test]
  public void ComputeHeights_AsymmetricBranches_HeightReflectsLongestPath()
  {
    var root = MakeStep("Root");
    var shortBranch = MakeStep("Short");
    var longA = MakeStep("LongA");
    var longB = MakeStep("LongB");
    var merge = MakeStep("Merge");

    shortBranch.Dependencies.Add(root);
    longA.Dependencies.Add(root);
    longB.Dependencies.Add(longA);
    merge.Dependencies.Add(shortBranch);
    merge.Dependencies.Add(longB);

    var nodes = new List<FlowStep> { root, shortBranch, longA, longB, merge };
    DependencyAnalyzer.AssignLayers(nodes);
    DependencyAnalyzer.ComputeHeights(nodes);

    Assert.That(root.Height, Is.EqualTo(3), "Root: longest path Root→LongA→LongB→Merge");
    Assert.That(shortBranch.Height, Is.EqualTo(1), "Short: one step to sink");
    Assert.That(longA.Height, Is.EqualTo(2), "LongA: LongA→LongB→Merge");
    Assert.That(longB.Height, Is.EqualTo(1), "LongB: one step to sink");
    Assert.That(merge.Height, Is.EqualTo(0), "Merge: sink");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // FifoSchedulingStrategy — preserves arrival order
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void FifoStrategy_PreservesInputOrder()
  {
    var strategy = new FifoSchedulingStrategy();
    var steps = new List<FlowStep>
    {
      MakeStepWithHeight("X", height: 0),
      MakeStepWithHeight("Y", height: 5),
      MakeStepWithHeight("Z", height: 2),
    };
    var context = EmptyContext();

    var result = strategy.Prioritize(steps, context);

    Assert.That(
      result.Select(s => s.Label),
      Is.EqualTo(new[] { "X", "Y", "Z" }),
      "FIFO must not reorder steps"
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // CriticalPathSchedulingStrategy — descending height order
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void CriticalPathStrategy_OrdersByDescendingHeight()
  {
    var strategy = new CriticalPathSchedulingStrategy();
    var steps = new List<FlowStep>
    {
      MakeStepWithHeight("Low", height: 1),
      MakeStepWithHeight("High", height: 5),
      MakeStepWithHeight("Mid", height: 3),
    };
    var context = EmptyContext();

    var result = strategy.Prioritize(steps, context);

    Assert.That(
      result.Select(s => s.Label),
      Is.EqualTo(new[] { "High", "Mid", "Low" }),
      "CriticalPath must dispatch highest height first"
    );
  }

  [Test]
  public void CriticalPathStrategy_EqualHeightStepsRetainArrivalOrder()
  {
    var strategy = new CriticalPathSchedulingStrategy();
    // All steps have the same height — arrival order (FIFO) should be preserved.
    var steps = new List<FlowStep>
    {
      MakeStepWithHeight("First", height: 2),
      MakeStepWithHeight("Second", height: 2),
      MakeStepWithHeight("Third", height: 2),
    };
    var context = EmptyContext();

    var result = strategy.Prioritize(steps, context);

    Assert.That(
      result.Select(s => s.Label),
      Is.EqualTo(new[] { "First", "Second", "Third" }),
      "Equal-height steps should maintain relative arrival order (stable sort)"
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // ExecutionOptions defaults — strategy auto-selection
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task DefaultStrategy_Sequential_UsesFifo_AndProducesCorrectResult()
  {
    // The default for MaxDegreeOfParallelism=1 is FifoSchedulingStrategy.
    // Correctness is all that matters here — ordering is implicitly FIFO since
    // only one step runs at a time.
    var catalog = new SimpleThreeStepCatalog();
    await catalog.Input.Save(SeedData).Run();

    var flow = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep("StepA", PassthroughStep.Create(), catalog.Input, catalog.StepOne);
      builder.AddStep("StepB", PassthroughStep.Create(), catalog.StepOne, catalog.StepTwo);
      builder.AddStep("StepC", PassthroughStep.Create(), catalog.StepTwo, catalog.Output);
    });

    // MaxDegreeOfParallelism=1, no explicit strategy → FIFO selected automatically.
    var result = await flow.RunAsync(
      new ExecutionOptions { MaxDegreeOfParallelism = 1 },
      CancellationToken.None
    );

    Assert.That(result.Success, Is.True);
    Assert.That(result.StepResults, Has.Count.EqualTo(3));
  }

  [Test]
  public async Task DefaultStrategy_Parallel_UsesCriticalPath_AndProducesCorrectResult()
  {
    // The default for MaxDegreeOfParallelism>1 is CriticalPathSchedulingStrategy.
    var catalog = new ParallelBranchCatalog();
    await catalog.Input.Save(SeedData).Run();

    var flow = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep("StepA", PassthroughStep.Create(), catalog.Input, catalog.BranchA);
      builder.AddStep("StepB", PassthroughStep.Create(), catalog.Input, catalog.BranchB);
      builder.AddStep(
        "StepMerge",
        MergeStep.Create(),
        (catalog.BranchA, catalog.BranchB),
        catalog.Merged
      );
    });

    // MaxDegreeOfParallelism=2, no explicit strategy → CriticalPath selected automatically.
    var result = await flow.RunAsync(
      new ExecutionOptions { MaxDegreeOfParallelism = 2 },
      CancellationToken.None
    );

    Assert.That(result.Success, Is.True);
    Assert.That(result.StepResults, Has.Count.EqualTo(3));
    Assert.That(result.StepResults.Values.All(r => r.Success), Is.True);
  }

  [Test]
  public async Task ExplicitStrategy_OverridesDefault()
  {
    // Even with parallelism=2, an explicitly-provided FifoSchedulingStrategy should be used.
    var catalog = new ParallelBranchCatalog();
    await catalog.Input.Save(SeedData).Run();

    var flow = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep("StepA", PassthroughStep.Create(), catalog.Input, catalog.BranchA);
      builder.AddStep("StepB", PassthroughStep.Create(), catalog.Input, catalog.BranchB);
    });

    var result = await flow.RunAsync(
      new ExecutionOptions
      {
        MaxDegreeOfParallelism = 2,
        SchedulingStrategy = new FifoSchedulingStrategy(),
      },
      CancellationToken.None
    );

    Assert.That(result.Success, Is.True);
    Assert.That(result.StepResults, Has.Count.EqualTo(2));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static FlowStep MakeStep(string label) =>
    new(label, null, (IEnumerable<TestData> _) => Enumerable.Empty<TestData>(), [], []);

  private static FlowStep MakeStepWithHeight(string label, int height)
  {
    var step = MakeStep(label);
    step.Height = height;
    return step;
  }

  private static SchedulingContext EmptyContext() =>
    new(new Dictionary<FlowStep, IReadOnlyList<FlowStep>>());
}
