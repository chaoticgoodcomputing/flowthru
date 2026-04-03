using Flowthru.Flows;
using Flowthru.Tests.Fixtures.TestCatalogs;
using Flowthru.Tests.Fixtures.TestSteps;

namespace Flowthru.Tests.Execution;

/// <summary>
/// Tests for the unified slicing API: From / To / Only / Flows.
/// </summary>
/// <remarks>
/// Each test builds a fresh flow to avoid the dependency-trimming side-effect that
/// occurs when SliceSteps mutates step.Dependencies in place.
/// <para>
/// The base DAG used across most tests is a linear 4-step chain:
/// <code>
///   StepA: input_a    → processed_a
///   StepB: processed_a → processed_b
///   StepC: processed_b → merged
///   StepD: merged      → final
/// </code>
/// </para>
/// </remarks>
[TestFixture]
[Category("Execution")]
[Category("Slicing")]
public class SlicingTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static Flow BuildLinearFlow()
  {
    var catalog = new ComplexMultiLayerCatalog();
    return FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep("StepA", PassthroughStep.Create(), catalog.InputA, catalog.ProcessedA);
      builder.AddStep("StepB", PassthroughStep.Create(), catalog.ProcessedA, catalog.ProcessedB);
      builder.AddStep("StepC", PassthroughStep.Create(), catalog.ProcessedB, catalog.Merged);
      builder.AddStep("StepD", PassthroughStep.Create(), catalog.Merged, catalog.Final);
    });
  }

  /// <summary>
  /// Returns sorted step labels from the sliced view, falling back to all steps when no
  /// slice was applied.
  /// </summary>
  private static IReadOnlyList<string> GetSlicedLabels(Flow flow)
  {
    var steps = flow.GetSlicedSteps() ?? flow.StepsList;
    return steps.Select(s => s.Label).Order().ToList();
  }

  // ─────────────────────────────────────────────────────────────────────────
  // From — step label
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void From_StepLabel_IncludesStartingStepAndAllDownstream()
  {
    var flow = BuildLinearFlow();

    flow.Build(new FlowSliceStrategy { From = new HashSet<string> { "StepB" } });

    Assert.That(GetSlicedLabels(flow), Is.EquivalentTo(new[] { "StepB", "StepC", "StepD" }));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // From — catalog item label
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void From_CatalogItemLabel_ResolvesToConsumerStepAndDownstream()
  {
    // "processed_a" is an output of StepA and an input consumed by StepB.
    // From resolves "processed_a" → consumer StepB → expands downstream.
    var flow = BuildLinearFlow();

    flow.Build(new FlowSliceStrategy { From = new HashSet<string> { "processed_a" } });

    Assert.That(GetSlicedLabels(flow), Is.EquivalentTo(new[] { "StepB", "StepC", "StepD" }));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // To — step label
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void To_StepLabel_IncludesEndingStepAndAllUpstream()
  {
    var flow = BuildLinearFlow();

    flow.Build(new FlowSliceStrategy { To = new HashSet<string> { "StepC" } });

    Assert.That(GetSlicedLabels(flow), Is.EquivalentTo(new[] { "StepA", "StepB", "StepC" }));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // To — catalog item label
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void To_CatalogItemLabel_ResolvesToProducerStepAndUpstream()
  {
    // "merged" is produced by StepC. To resolves "merged" → producer StepC → expands upstream.
    var flow = BuildLinearFlow();

    flow.Build(new FlowSliceStrategy { To = new HashSet<string> { "merged" } });

    Assert.That(GetSlicedLabels(flow), Is.EquivalentTo(new[] { "StepA", "StepB", "StepC" }));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Only — step label
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Only_StepLabel_IncludesStepAndMinimalUpstreamDependencies()
  {
    var flow = BuildLinearFlow();

    flow.Build(new FlowSliceStrategy { Only = new HashSet<string> { "StepC" } });

    // StepC depends on StepB which depends on StepA — all three needed.
    Assert.That(GetSlicedLabels(flow), Is.EquivalentTo(new[] { "StepA", "StepB", "StepC" }));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Only — catalog item label
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Only_CatalogItemLabel_ResolvesToProducerStepAndMinimalUpstream()
  {
    // "processed_b" is produced by StepB. Only resolves to StepB → upstream is StepA only.
    var flow = BuildLinearFlow();

    flow.Build(new FlowSliceStrategy { Only = new HashSet<string> { "processed_b" } });

    Assert.That(GetSlicedLabels(flow), Is.EquivalentTo(new[] { "StepA", "StepB" }));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Flows — merged DAG filter
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Flows_FiltersByFlowNamePrefix_InMergedDag()
  {
    var catalog = new ComplexMultiLayerCatalog();

    var flowAlpha = FlowBuilder.CreateFlow(b =>
    {
      b.AddStep("StepA", PassthroughStep.Create(), catalog.InputA, catalog.ProcessedA);
      b.AddStep("StepB", PassthroughStep.Create(), catalog.ProcessedA, catalog.ProcessedB);
    });

    var flowBeta = FlowBuilder.CreateFlow(b =>
    {
      b.AddStep("StepC", PassthroughStep.Create(), catalog.ProcessedB, catalog.Merged);
      b.AddStep("StepD", PassthroughStep.Create(), catalog.Merged, catalog.Final);
    });

    var merged = Flow.Merge(
      new Dictionary<string, Flow> { { "FlowAlpha", flowAlpha }, { "FlowBeta", flowBeta } }
    );

    merged.Build(new FlowSliceStrategy { Flows = new HashSet<string> { "FlowAlpha" } });

    Assert.That(
      GetSlicedLabels(merged),
      Is.EquivalentTo(new[] { "FlowAlpha.StepA", "FlowAlpha.StepB" })
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Composition — From + To intersection
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void From_And_To_ProduceIntersectionSegment()
  {
    // From StepB expands downstream: { StepB, StepC, StepD }
    // To StepC expands upstream:     { StepA, StepB, StepC }
    // Intersection:                  { StepB, StepC }
    var flow = BuildLinearFlow();

    flow.Build(
      new FlowSliceStrategy
      {
        From = new HashSet<string> { "StepB" },
        To = new HashSet<string> { "StepC" },
      }
    );

    Assert.That(GetSlicedLabels(flow), Is.EquivalentTo(new[] { "StepB", "StepC" }));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // No slicing
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void NoSlice_GetSlicedStepsReturnsNull()
  {
    var flow = BuildLinearFlow();

    flow.Build();

    Assert.That(flow.GetSlicedSteps(), Is.Null);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Error — unknown label
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void From_UnknownLabel_ThrowsInvalidOperationException()
  {
    var flow = BuildLinearFlow();

    Assert.Throws<InvalidOperationException>(
      () => flow.Build(new FlowSliceStrategy { From = new HashSet<string> { "does_not_exist" } })
    );
  }

  [Test]
  public void To_UnknownLabel_ThrowsInvalidOperationException()
  {
    var flow = BuildLinearFlow();

    Assert.Throws<InvalidOperationException>(
      () => flow.Build(new FlowSliceStrategy { To = new HashSet<string> { "does_not_exist" } })
    );
  }

  [Test]
  public void Only_UnknownLabel_ThrowsInvalidOperationException()
  {
    var flow = BuildLinearFlow();

    Assert.Throws<InvalidOperationException>(
      () => flow.Build(new FlowSliceStrategy { Only = new HashSet<string> { "does_not_exist" } })
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Error — catalog item with no producer (external / seed input)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void To_CatalogItemWithNoProducer_ThrowsInvalidOperationException()
  {
    // "input_a" is an external input — no step in the flow produces it.
    var flow = BuildLinearFlow();

    Assert.Throws<InvalidOperationException>(
      () => flow.Build(new FlowSliceStrategy { To = new HashSet<string> { "input_a" } })
    );
  }

  [Test]
  public void Only_CatalogItemWithNoProducer_ThrowsInvalidOperationException()
  {
    var flow = BuildLinearFlow();

    Assert.Throws<InvalidOperationException>(
      () => flow.Build(new FlowSliceStrategy { Only = new HashSet<string> { "input_a" } })
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Error — Flows filter matches nothing
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Flows_MatchesNoSteps_ThrowsInvalidOperationException()
  {
    var flow = BuildLinearFlow();

    Assert.Throws<InvalidOperationException>(
      () => flow.Build(new FlowSliceStrategy { Flows = new HashSet<string> { "NonExistentFlow" } })
    );
  }
}
