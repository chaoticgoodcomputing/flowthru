using Flowthru.Caching;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Diagnostics.Mermaid.Internal;
using Flowthru.Flow;

namespace Flowthru.Extensions.Metadata.Mermaid.Tests;

/// <summary>
/// Coverage for cache-plan rendering on Mermaid diagrams (Phase 8.0.8).
/// Pre-run diagrams colour steps the plan marks Fresh as blue; post-run
/// diagrams colour Succeeded steps whose Reason is "cached" the same
/// way. Other states (in-slice + will-execute, out-of-slice, ran,
/// failed) keep their existing visual treatment.
/// </summary>
[TestFixture]
[Category("Metadata.Mermaid")]
[Category("CachePlan")]
public class MermaidCachePlanTests
{
  private static readonly string Blue = "#1976D2";

  // ── Pre-run ─────────────────────────────────────────────────────────

  [Test]
  public void RenderDag_FreshStepInPlan_RendersBlue()
  {
    var flow = BuildTwoStepFlow();
    var plan = new CachePlan(
      FreshStepLabels: new HashSet<string>(new[] { "cached_step" }, StringComparer.Ordinal),
      StaleStepLabels: new HashSet<string>(StringComparer.Ordinal),
      UncacheableStepLabels: new HashSet<string>(StringComparer.Ordinal),
      NewStepFingerprints: new Dictionary<string, string>(StringComparer.Ordinal),
      NewItemFingerprints: new Dictionary<string, string>(StringComparer.Ordinal),
      UncacheableReasons: new Dictionary<string, StepUncacheableReason>(StringComparer.Ordinal)
    );
    var ctx = new FlowMetadataContext
    {
      MergedFlow = flow,
      EffectiveFlow = flow,
      ActiveStepLabels = flow.Steps.Select(s => s.Label).ToHashSet(StringComparer.Ordinal),
      RequestedFlowLabel = null,
      CachePlan = plan,
    };

    var diagram = MermaidDiagramRenderer.RenderDag(
      ctx, showFullDag: true,
      direction: MermaidFlowchartDirection.TopToBottom,
      theme: MermaidDiagramRenderer.Theme.Default
    );

    Assert.That(diagram, Does.Contain($"style cached_step fill:{Blue},color:#FFFFFF"),
      "A step in CachePlan.FreshStepLabels should render with the cached-step blue fill.");
  }

  [Test]
  public void RenderDag_StaleStepInPlan_RendersUnstyled()
  {
    // Will-execute steps should render with default Mermaid styling
    // (no explicit fill) — neutral, not blue.
    var flow = BuildTwoStepFlow();
    var plan = new CachePlan(
      FreshStepLabels: new HashSet<string>(StringComparer.Ordinal),
      StaleStepLabels: new HashSet<string>(new[] { "cached_step", "will_run" }, StringComparer.Ordinal),
      UncacheableStepLabels: new HashSet<string>(StringComparer.Ordinal),
      NewStepFingerprints: new Dictionary<string, string>(StringComparer.Ordinal),
      NewItemFingerprints: new Dictionary<string, string>(StringComparer.Ordinal),
      UncacheableReasons: new Dictionary<string, StepUncacheableReason>(StringComparer.Ordinal)
    );
    var ctx = new FlowMetadataContext
    {
      MergedFlow = flow,
      EffectiveFlow = flow,
      ActiveStepLabels = flow.Steps.Select(s => s.Label).ToHashSet(StringComparer.Ordinal),
      RequestedFlowLabel = null,
      CachePlan = plan,
    };

    var diagram = MermaidDiagramRenderer.RenderDag(
      ctx, showFullDag: true,
      direction: MermaidFlowchartDirection.TopToBottom,
      theme: MermaidDiagramRenderer.Theme.Default
    );

    Assert.That(diagram, Does.Not.Contain("style cached_step fill:"),
      "A stale step should not receive an explicit fill style.");
    Assert.That(diagram, Does.Not.Contain("style will_run fill:"),
      "A stale step should not receive an explicit fill style.");
  }

  [Test]
  public void RenderDag_NoCachePlan_AllActiveStepsRenderUnstyled()
  {
    var flow = BuildTwoStepFlow();
    var ctx = new FlowMetadataContext
    {
      MergedFlow = flow,
      EffectiveFlow = flow,
      ActiveStepLabels = flow.Steps.Select(s => s.Label).ToHashSet(StringComparer.Ordinal),
      RequestedFlowLabel = null,
      // CachePlan deliberately omitted — caching disabled or bypassed
    };

    var diagram = MermaidDiagramRenderer.RenderDag(
      ctx, showFullDag: true,
      direction: MermaidFlowchartDirection.TopToBottom,
      theme: MermaidDiagramRenderer.Theme.Default
    );

    Assert.That(diagram, Does.Not.Contain($"fill:{Blue}"),
      "Without a cache plan, no step should be coloured blue.");
  }

  // ── Post-run ────────────────────────────────────────────────────────

  [Test]
  public void RenderRun_CachedStepResult_RendersBlue()
  {
    var flow = BuildTwoStepFlow();
    var ctx = new FlowMetadataContext
    {
      MergedFlow = flow,
      EffectiveFlow = flow,
      ActiveStepLabels = flow.Steps.Select(s => s.Label).ToHashSet(StringComparer.Ordinal),
      RequestedFlowLabel = null,
    };
    var result = new FlowResult(new[]
    {
      // Scheduler emits Reason="cached" for short-circuited fresh steps.
      (StepResult)new StepResult.Succeeded("cached_step", TimeSpan.Zero) { Reason = "cached" },
      (StepResult)new StepResult.Succeeded("will_run", TimeSpan.FromMilliseconds(10)),
    }, TimeSpan.FromMilliseconds(10));

    var runCtx = new FlowRunMetadataContext { Static = ctx, Result = result };
    var diagram = MermaidDiagramRenderer.RenderRun(
      runCtx, showFullDag: true,
      direction: MermaidFlowchartDirection.TopToBottom,
      theme: MermaidDiagramRenderer.Theme.Default
    );

    Assert.That(diagram, Does.Contain($"style cached_step fill:{Blue}"),
      "A succeeded step with Reason=\"cached\" should render with the cached-step blue fill.");
  }

  [Test]
  public void RenderRun_OnlyOneNonCachedStep_RendersNeutralNotRed()
  {
    // When only one step runs (the others were cached), the curve is
    // degenerate. Render the lone ran step at the green baseline so it
    // doesn't deceptively read as red.
    var flow = BuildTwoStepFlow();
    var ctx = new FlowMetadataContext
    {
      MergedFlow = flow,
      EffectiveFlow = flow,
      ActiveStepLabels = flow.Steps.Select(s => s.Label).ToHashSet(StringComparer.Ordinal),
      RequestedFlowLabel = null,
    };
    var result = new FlowResult(new[]
    {
      (StepResult)new StepResult.Succeeded("cached_step", TimeSpan.Zero) { Reason = "cached" },
      (StepResult)new StepResult.Succeeded("will_run", TimeSpan.FromMilliseconds(500)),
    }, TimeSpan.FromMilliseconds(500));

    var runCtx = new FlowRunMetadataContext { Static = ctx, Result = result };
    var diagram = MermaidDiagramRenderer.RenderRun(
      runCtx, showFullDag: true,
      direction: MermaidFlowchartDirection.TopToBottom,
      theme: MermaidDiagramRenderer.Theme.Default
    );

    // The lone ran step should fall back to the green active baseline,
    // not the red heat-map endpoint.
    Assert.That(diagram, Does.Contain("style will_run fill:#2E7D32"),
      "A single non-cached ran step should render at the green baseline, not on the red end of the curve.");
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static BuiltFlow BuildTwoStepFlow()
  {
    var cachedOut = Flowthru.Data.Catalog.ItemFactory.Singleton.Memory<int>("cached-out");
    var ranOut = Flowthru.Data.Catalog.ItemFactory.Singleton.Memory<int>("ran-out");
    return FlowBuilder.CreateFlow("plan-flow", b =>
    {
      b.AddStep<int>("cached_step", () => 1, cachedOut);
      b.AddStep<int>("will_run", () => 2, ranOut);
    });
  }
}
