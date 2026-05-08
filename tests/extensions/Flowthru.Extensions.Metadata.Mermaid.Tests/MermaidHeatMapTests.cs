using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Diagnostics.Mermaid.Internal;
using Flowthru.Flow;
using Flowthru.Validation.Runtime;

namespace Flowthru.Extensions.Metadata.Mermaid.Tests;

/// <summary>
/// Coverage for the Phase 8.0.7 Mermaid heat-map: succeeded steps
/// interpolate between the theme's active colour (fastest) and the
/// heat-map max colour (slowest). Failed and skipped steps keep
/// their dedicated theme colours regardless of duration. Pre-run
/// diagrams (no run result) skip the heat-map entirely.
/// </summary>
[TestFixture]
[Category("Metadata.Mermaid")]
[Category("HeatMap")]
public class MermaidHeatMapTests
{
  // ── InterpolateHex sanity ───────────────────────────────────────────

  [Test]
  public void InterpolateHex_AtZero_ReturnsFromColor() =>
    Assert.That(MermaidDiagramRenderer.InterpolateHex("#2E7D32", "#FF8F00", 0.0),
      Is.EqualTo("#2E7D32"));

  [Test]
  public void InterpolateHex_AtOne_ReturnsToColor() =>
    Assert.That(MermaidDiagramRenderer.InterpolateHex("#2E7D32", "#FF8F00", 1.0),
      Is.EqualTo("#FF8F00"));

  [Test]
  public void InterpolateHex_AtHalf_LandsBetween()
  {
    // 0x2E + (0xFF-0x2E)/2 = 150.5 → banker's rounding lands on 150 (0x96)
    // 0x7D + (0x8F-0x7D)/2 = 134 (0x86)
    // 0x32 + (0x00-0x32)/2 = 25 (0x19)
    Assert.That(MermaidDiagramRenderer.InterpolateHex("#2E7D32", "#FF8F00", 0.5),
      Is.EqualTo("#968619"));
  }

  [Test]
  public void InterpolateHex_MalformedInput_FallsBackToFrom() =>
    Assert.That(MermaidDiagramRenderer.InterpolateHex("not-hex", "#FF8F00", 0.5),
      Is.EqualTo("not-hex"));

  // ── Render-level heat-map behavior ──────────────────────────────────

  [Test]
  public void RenderRun_TwoSucceededStepsWithDifferentDurations_DistinctColors()
  {
    // Two steps: 'fast' (1ms), 'slow' (100ms). The fast one should
    // land near the active baseline; the slow one near the heat-map
    // max. Asserting they're literally different is enough — the
    // exact hex round-trip is covered by InterpolateHex_* above.
    var fastFlow = BuildTwoStepFlowResult(
      fastDuration: TimeSpan.FromMilliseconds(1),
      slowDuration: TimeSpan.FromMilliseconds(100)
    );

    var diagram = MermaidDiagramRenderer.RenderRun(
      fastFlow,
      showFullDag: true,
      direction: MermaidFlowchartDirection.TopToBottom,
      theme: MermaidDiagramRenderer.Theme.Default
    );

    var fastColorMatch = System.Text.RegularExpressions.Regex.Match(
      diagram, @"style fast fill:(#[0-9A-F]{6})");
    var slowColorMatch = System.Text.RegularExpressions.Regex.Match(
      diagram, @"style slow fill:(#[0-9A-F]{6})");

    Assert.That(fastColorMatch.Success, Is.True, "Fast step should be styled.");
    Assert.That(slowColorMatch.Success, Is.True, "Slow step should be styled.");
    Assert.That(fastColorMatch.Groups[1].Value, Is.Not.EqualTo(slowColorMatch.Groups[1].Value),
      "Two steps with very different durations should land at different heat-map points.");
    Assert.That(slowColorMatch.Groups[1].Value, Is.EqualTo("#FF8F00"),
      "The slowest step should pin to the heat-map max colour.");
  }

  [Test]
  public void RenderRun_FailedStep_KeepsFailedColorIgnoringDuration()
  {
    var ctx = new FlowMetadataContext
    {
      MergedFlow = BuildSingleStepFlow("boom"),
      EffectiveFlow = BuildSingleStepFlow("boom"),
      ActiveStepLabels = new HashSet<string>(new[] { "boom" }, StringComparer.Ordinal),
      RequestedFlowLabel = null,
    };
    var result = new FlowResult(new[]
    {
      (StepResult)new StepResult.Failed(
        "boom",
        new RuntimeError.External("test", new InvalidOperationException("nope")),
        TimeSpan.FromMilliseconds(500)
      ),
    }, TimeSpan.FromMilliseconds(500));

    var runCtx = new FlowRunMetadataContext { Static = ctx, Result = result };
    var diagram = MermaidDiagramRenderer.RenderRun(
      runCtx, showFullDag: true,
      direction: MermaidFlowchartDirection.TopToBottom,
      theme: MermaidDiagramRenderer.Theme.Default
    );

    Assert.That(diagram, Does.Contain("style boom fill:#C62828"),
      "Failed steps render with the dedicated FailedStepColor regardless of duration.");
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static FlowRunMetadataContext BuildTwoStepFlowResult(
    TimeSpan fastDuration, TimeSpan slowDuration
  )
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
      (StepResult)new StepResult.Succeeded("fast", fastDuration),
      (StepResult)new StepResult.Succeeded("slow", slowDuration),
    }, fastDuration + slowDuration);
    return new FlowRunMetadataContext { Static = ctx, Result = result };
  }

  private static BuiltFlow BuildSingleStepFlow(string label)
  {
    var output = Flowthru.Data.Catalog.ItemFactory.Singleton.Memory<int>($"{label}-out");
    return FlowBuilder.CreateFlow(label, b =>
    {
      b.AddStep<int>(label, () => 0, output);
    });
  }

  private static BuiltFlow BuildTwoStepFlow()
  {
    var fastOut = Flowthru.Data.Catalog.ItemFactory.Singleton.Memory<int>("fast-out");
    var slowOut = Flowthru.Data.Catalog.ItemFactory.Singleton.Memory<int>("slow-out");
    return FlowBuilder.CreateFlow("two-step", b =>
    {
      b.AddStep<int>("fast", () => 1, fastOut);
      b.AddStep<int>("slow", () => 2, slowOut);
    });
  }
}
