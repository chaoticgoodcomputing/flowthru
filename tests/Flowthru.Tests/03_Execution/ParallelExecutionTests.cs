using System.Collections.Concurrent;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestSteps;

namespace Flowthru.Tests.Execution;

/// <summary>
/// Tests verifying the task-graph scheduler's parallel dispatch behaviour.
/// </summary>
/// <remarks>
/// <para>
/// Concurrency is verified structurally (overlapping execution windows) rather than
/// purely by elapsed time — wall-clock assertions are inherently flaky on loaded CI
/// machines. Each test that needs to prove steps ran concurrently compares the
/// start/end timestamps recorded by <see cref="RecordingStep"/> and asserts that at
/// least two windows overlap.
/// </para>
/// <para>
/// The core DAG used across most tests is a diamond:
/// <code>
///   (external) Input ──→ StepA (BranchA) ──┐
///                    └──→ StepB (BranchB) ──┴──→ StepMerge (Merged)
/// </code>
/// StepA and StepB have no dependency on each other, so they are eligible for
/// concurrent dispatch whenever <c>MaxDegreeOfParallelism &gt; 1</c>.
/// </para>
/// </remarks>
[TestFixture]
[Category("Execution")]
[Category("Parallel")]
public class ParallelExecutionTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

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

  private static readonly TimeSpan BranchDelay = TimeSpan.FromMilliseconds(300);

  /// <summary>
  /// Returns true when two execution windows overlap in time.
  /// </summary>
  private static bool Overlaps(
    (string Label, DateTime Start, DateTime End) a,
    (string Label, DateTime Start, DateTime End) b
  ) => a.Start < b.End && b.Start < a.End;

  // ─────────────────────────────────────────────────────────────────────────
  // Correctness — parallel produces the same results as sequential
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RunAsync_WithParallelism_ProducesCorrectResults()
  {
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

    var result = await flow.RunAsync(
      new ExecutionOptions { MaxDegreeOfParallelism = 2 },
      CancellationToken.None
    );

    Assert.That(result.Success, Is.True);
    var merged = await catalog.Merged.Load().Run();
    Assert.That(
      merged.Count(),
      Is.EqualTo(SeedData.Count() * 2),
      "Merge should contain both branches"
    );
    Assert.That(result.StepResults, Has.Count.EqualTo(3));
    Assert.That(result.StepResults.Values.All(r => r.Success), Is.True);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Concurrency — independent steps actually overlap when parallelism > 1
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RunAsync_WithParallelism_IndependentStepsOverlapInTime()
  {
    var catalog = new ParallelBranchCatalog();
    await catalog.Input.Save(SeedData).Run();

    var log = new ConcurrentBag<(string Label, DateTime Start, DateTime End)>();

    var flow = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        "StepA",
        RecordingStep.Create(log, "StepA", BranchDelay),
        catalog.Input,
        catalog.BranchA
      );
      builder.AddStep(
        "StepB",
        RecordingStep.Create(log, "StepB", BranchDelay),
        catalog.Input,
        catalog.BranchB
      );
    });

    var result = await flow.RunAsync(
      new ExecutionOptions { MaxDegreeOfParallelism = 2 },
      CancellationToken.None
    );

    Assert.That(result.Success, Is.True, "Flow should succeed");
    Assert.That(log, Has.Count.EqualTo(2), "Both steps should have recorded");

    var entries = log.ToList();
    var stepA = entries.First(e => e.Label == "StepA");
    var stepB = entries.First(e => e.Label == "StepB");

    Assert.That(
      Overlaps(stepA, stepB),
      Is.True,
      $"StepA [{stepA.Start:mm:ss.fff}–{stepA.End:mm:ss.fff}] and "
        + $"StepB [{stepB.Start:mm:ss.fff}–{stepB.End:mm:ss.fff}] should overlap"
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Sequential constraint — MaxDegreeOfParallelism = 1 prevents overlap
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RunAsync_WithParallelismOne_IndependentStepsDoNotOverlap()
  {
    var catalog = new ParallelBranchCatalog();
    await catalog.Input.Save(SeedData).Run();

    var log = new ConcurrentBag<(string Label, DateTime Start, DateTime End)>();

    var flow = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        "StepA",
        RecordingStep.Create(log, "StepA", BranchDelay),
        catalog.Input,
        catalog.BranchA
      );
      builder.AddStep(
        "StepB",
        RecordingStep.Create(log, "StepB", BranchDelay),
        catalog.Input,
        catalog.BranchB
      );
    });

    var result = await flow.RunAsync(
      new ExecutionOptions { MaxDegreeOfParallelism = 1 },
      CancellationToken.None
    );

    Assert.That(result.Success, Is.True, "Flow should succeed");
    var entries = log.ToList();
    var stepA = entries.First(e => e.Label == "StepA");
    var stepB = entries.First(e => e.Label == "StepB");

    Assert.That(
      Overlaps(stepA, stepB),
      Is.False,
      $"With MaxDegreeOfParallelism=1, StepA [{stepA.Start:mm:ss.fff}–{stepA.End:mm:ss.fff}] and "
        + $"StepB [{stepB.Start:mm:ss.fff}–{stepB.End:mm:ss.fff}] must not overlap"
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // StopOnFirstError = true (default) — halts on the first failure
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RunAsync_StopOnFirstErrorTrue_ReturnsFailureResult()
  {
    var catalog = new ParallelBranchCatalog();
    await catalog.Input.Save(SeedData).Run();

    var flow = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        "FailingA",
        FailingStep.Create("branch A failed"),
        catalog.Input,
        catalog.BranchA
      );
      builder.AddStep("StepB", PassthroughStep.Create(), catalog.Input, catalog.BranchB);
    });

    var result = await flow.RunAsync(
      new ExecutionOptions { MaxDegreeOfParallelism = 2, StopOnFirstError = true },
      CancellationToken.None
    );

    Assert.That(result.Success, Is.False, "Flow should fail");
    Assert.That(result.Exception, Is.Not.Null);
    Assert.That(result.StepResults["FailingA"].Success, Is.False);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // StopOnFirstError = false — independent branches complete; dependents skipped
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RunAsync_StopOnFirstErrorFalse_IndependentBranchCompletes()
  {
    var catalog = new ParallelBranchCatalog();
    await catalog.Input.Save(SeedData).Run();

    var flow = FlowBuilder.CreateFlow(builder =>
    {
      // StepA fails — BranchA will not be populated.
      builder.AddStep(
        "FailingA",
        FailingStep.Create("branch A failed"),
        catalog.Input,
        catalog.BranchA
      );
      // StepB is independent and should still run and succeed.
      builder.AddStep("StepB", PassthroughStep.Create(), catalog.Input, catalog.BranchB);
      // StepMerge depends on BranchA (failed) — should be skipped.
      builder.AddStep(
        "StepMerge",
        MergeStep.Create(),
        (catalog.BranchA, catalog.BranchB),
        catalog.Merged
      );
    });

    var result = await flow.RunAsync(
      new ExecutionOptions { MaxDegreeOfParallelism = 2, StopOnFirstError = false },
      CancellationToken.None
    );

    Assert.That(result.Success, Is.False, "Flow should still report failure");
    Assert.That(result.StepResults["FailingA"].Success, Is.False, "FailingA should be failed");
    Assert.That(result.StepResults["StepB"].Success, Is.True, "Independent StepB should have run");
    // StepMerge is a downstream dependent of FailingA — it should have been skipped.
    Assert.That(
      result.StepResults,
      Does.Not.ContainKey("StepMerge"),
      "StepMerge should be skipped"
    );

    var branchB = await catalog.BranchB.Load().Run();
    Assert.That(branchB.Count(), Is.EqualTo(SeedData.Count()), "BranchB should have been written");
  }

  [Test]
  public async Task RunAsync_StopOnFirstErrorFalse_DownstreamOfFailedStepIsSkipped()
  {
    // Linear chain: A → B → C. A fails. B and C should be skipped.
    var catalog = new SimpleThreeStepCatalog();
    await catalog.Input.Save(SeedData).Run();

    var flow = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep("StepA", FailingStep.Create("A failed"), catalog.Input, catalog.StepOne);
      builder.AddStep("StepB", PassthroughStep.Create(), catalog.StepOne, catalog.StepTwo);
      builder.AddStep("StepC", PassthroughStep.Create(), catalog.StepTwo, catalog.Output);
    });

    var result = await flow.RunAsync(
      new ExecutionOptions { MaxDegreeOfParallelism = 1, StopOnFirstError = false },
      CancellationToken.None
    );

    Assert.That(result.Success, Is.False);
    Assert.That(result.StepResults["StepA"].Success, Is.False);
    Assert.That(
      result.StepResults,
      Does.Not.ContainKey("StepB"),
      "StepB (downstream) should be skipped"
    );
    Assert.That(
      result.StepResults,
      Does.Not.ContainKey("StepC"),
      "StepC (downstream) should be skipped"
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Dependency ordering — downstream steps never start before their producers
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RunAsync_WithParallelism_DownstreamStepStartsAfterProducerCompletes()
  {
    // Linear chain: StepA (delayed) → StepB. Even with high parallelism, StepB must
    // not start until StepA has written its output.
    var catalog = new SimpleThreeStepCatalog();
    await catalog.Input.Save(SeedData).Run();

    var log = new ConcurrentBag<(string Label, DateTime Start, DateTime End)>();

    var flow = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep(
        "StepA",
        RecordingStep.Create(log, "StepA", BranchDelay),
        catalog.Input,
        catalog.StepOne
      );
      builder.AddStep(
        "StepB",
        RecordingStep.Create(log, "StepB", TimeSpan.Zero),
        catalog.StepOne,
        catalog.Output
      );
    });

    var result = await flow.RunAsync(
      new ExecutionOptions { MaxDegreeOfParallelism = 4 },
      CancellationToken.None
    );

    Assert.That(result.Success, Is.True);

    var entries = log.ToDictionary(e => e.Label);
    Assert.That(
      entries["StepB"].Start >= entries["StepA"].End,
      Is.True,
      $"StepB must not start [{entries["StepB"].Start:mm:ss.fff}] "
        + $"before StepA finishes [{entries["StepA"].End:mm:ss.fff}]"
    );
  }
}
