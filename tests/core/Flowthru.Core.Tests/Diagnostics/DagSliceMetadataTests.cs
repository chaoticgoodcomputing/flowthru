using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Diagnostics;

/// <summary>
/// Tests for the slice-metadata surface that downstream metadata
/// providers (JSON, Mermaid, dashboards) consume. On <c>main</c> this
/// lived on <c>DagSliceMetadata.FromStrategy</c> as a per-criterion
/// descriptor; on the FP rewrite the equivalent information lives on
/// <see cref="FlowMetadataContext"/> — <see cref="FlowMetadataContext.RequestedFlowLabel"/>,
/// <see cref="FlowMetadataContext.EffectiveFlow"/>, and
/// <see cref="FlowMetadataContext.ActiveStepLabels"/> — produced by
/// <c>FlowthruService.RunAsync</c> at each invocation.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why this class lives where it does.</strong> The maintainer
/// review note on gap #14 calls this surface "the natural regression
/// net for the outgoing-metadata correctness concern" — both the JSON
/// and Mermaid providers read the same context, so an assertion at this
/// boundary pins both providers' attribution and slice-membership
/// behaviour at once.
/// </para>
/// <para>
/// <strong>Phantom 'merged' flow attribution.</strong> Each test
/// running through a sliced execution asserts that every step's
/// <c>FlowLabel</c> still names its <em>pre-merge</em> defining flow —
/// never the merged container's synthesized label. This is the second
/// pinning site for the cross-cutting concern called out in
/// <c>docs/scratch/test-coverage-gap-analysis.md</c> (the first being
/// <c>StepMetadataIntegrationTests.Merge_PreservesPreMergeFlowLabelOnEveryStep</c>).
/// </para>
/// </remarks>
[TestFixture]
[Category("Diagnostics")]
public class DagSliceMetadataTests
{
  // ── Unsliced (null-strategy equivalent) ────────────────────────────────

  [Test]
  public void FromUnsliced_NoSliceMetadata_RequestedFlowLabelIsNull()
  {
    // Equivalent of main's `FromStrategy(null) returns null` — the FP
    // rewrite encodes "no slice" via the FlowMetadataContext.Unsliced
    // factory, which sets RequestedFlowLabel to null and EffectiveFlow
    // equal to MergedFlow.
    var input = ItemFactory.Singleton.Memory<int>("unsliced-input");
    var output = ItemFactory.Singleton.Memory<int>("unsliced-output");
    var flow = FlowBuilder.CreateFlow("only", b =>
      b.AddStep<int, int>("step", x => x, input, output)
    );

    var ctx = FlowMetadataContext.Unsliced(flow);

    Assert.Multiple(() =>
    {
      Assert.That(ctx.RequestedFlowLabel, Is.Null,
        "Unsliced context has no requested flow label.");
      Assert.That(ctx.EffectiveFlow, Is.SameAs(ctx.MergedFlow),
        "Unsliced context: EffectiveFlow and MergedFlow are the same instance.");
      Assert.That(ctx.ActiveStepLabels, Is.EquivalentTo(new[] { "step" }),
        "Every step in the merged flow is active when no slice is applied.");
    });
  }

  // ── Single-flow slice ──────────────────────────────────────────────────

  [Test]
  public async Task SingleFlowSlice_SetsRequestedFlowLabelAndNarrowsEffectiveFlow()
  {
    // Equivalent of main's `single Flow allowlist`. On the FP rewrite,
    // RunAsync(flowLabel) slices the merged DAG to the subgraph
    // reachable from that flow's declared outputs.
    var stage1 = ItemFactory.Singleton.Memory<int>("sfs-stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("sfs-stage2");
    var stage3 = ItemFactory.Singleton.Memory<int>("sfs-stage3");
    await stage1.Save(1).Run();

    var captured = new List<FlowMetadataContext>();
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterFlow("Upstream", () => FlowBuilder.CreateFlow("Upstream", p =>
        p.AddStep<int, int>("u-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("Downstream", () => FlowBuilder.CreateFlow("Downstream", p =>
        p.AddStep<int, int>("d-step", x => x * 2, stage2, stage3)
      ));
      b.ConfigureMetadata(m => m.AddProvider(new CapturingContextProvider(captured)));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync("Upstream");
    Assert.That(result.IsSuccess, Is.True);

    var ctx = captured.Single();
    Assert.Multiple(() =>
    {
      Assert.That(ctx.RequestedFlowLabel, Is.EqualTo("Upstream"),
        "Single-flow slice should record the user's requested label.");
      Assert.That(ctx.EffectiveFlow.Label, Is.EqualTo("Upstream"),
        "EffectiveFlow.Label tracks the requested slice key.");
      Assert.That(ctx.ActiveStepLabels, Is.EquivalentTo(new[] { "u-step" }),
        "Slicing to Upstream should expose only its step as active.");
      Assert.That(ctx.MergedFlow.Steps.Select(s => s.Label),
        Is.EquivalentTo(new[] { "u-step", "d-step" }),
        "MergedFlow remains the full union — consumers can render the full graph with the slice highlighted.");
    });
  }

  // ── Multi-flow merged (multi-flow equivalent) ──────────────────────────

  [Test]
  public async Task MultiFlowMergedRun_ExposesEveryRegisteredFlowsSteps()
  {
    // Equivalent of main's `multi-flow allowlist`. On the FP rewrite,
    // RunAsync(null) runs the merged DAG without slicing — every
    // registered flow's steps are active.
    var stage1 = ItemFactory.Singleton.Memory<int>("mfm-stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("mfm-stage2");
    var stage3 = ItemFactory.Singleton.Memory<int>("mfm-stage3");
    await stage1.Save(1).Run();

    var captured = new List<FlowMetadataContext>();
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterFlow("FlowA", () => FlowBuilder.CreateFlow("FlowA", p =>
        p.AddStep<int, int>("a-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("FlowB", () => FlowBuilder.CreateFlow("FlowB", p =>
        p.AddStep<int, int>("b-step", x => x * 2, stage2, stage3)
      ));
      b.ConfigureMetadata(m => m.AddProvider(new CapturingContextProvider(captured)));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync();
    Assert.That(result.IsSuccess, Is.True);

    var ctx = captured.Single();
    Assert.Multiple(() =>
    {
      Assert.That(ctx.RequestedFlowLabel, Is.Null,
        "Merged-DAG run has no requested flow label.");
      Assert.That(ctx.ActiveStepLabels,
        Is.EquivalentTo(new[] { "a-step", "b-step" }),
        "Every registered flow's steps are active under a merged-DAG run.");
    });
  }

  // ── Slice walks backwards over the DAG (from-only / to-only equivalent) ─

  [Test]
  public async Task DownstreamSlice_PullsInUpstreamDependencies()
  {
    // Equivalent of main's `From only` / `To only`. On the FP rewrite,
    // slicing to a flow's outputs walks the DAG backwards through
    // producer edges. Requesting the downstream flow's outputs must
    // include the upstream flow's producing step too, because its
    // output is the downstream flow's input.
    var stage1 = ItemFactory.Singleton.Memory<int>("ds-stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("ds-stage2");
    var stage3 = ItemFactory.Singleton.Memory<int>("ds-stage3");
    await stage1.Save(1).Run();

    var captured = new List<FlowMetadataContext>();
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterFlow("Upstream", () => FlowBuilder.CreateFlow("Upstream", p =>
        p.AddStep<int, int>("u-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("Downstream", () => FlowBuilder.CreateFlow("Downstream", p =>
        p.AddStep<int, int>("d-step", x => x * 2, stage2, stage3)
      ));
      b.ConfigureMetadata(m => m.AddProvider(new CapturingContextProvider(captured)));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync("Downstream");
    Assert.That(result.IsSuccess, Is.True);

    var ctx = captured.Single();
    Assert.That(ctx.ActiveStepLabels,
      Is.EquivalentTo(new[] { "u-step", "d-step" }),
      "Slicing to Downstream must pull in its upstream producer (no 'producer-out-of-slice' surprises).");
  }

  // ── Slice consistency: ActiveStepLabels == EffectiveFlow.Steps labels ──

  [Test]
  public async Task ActiveStepLabels_AlwaysMatchesEffectiveFlowSteps()
  {
    // Membership consistency: the active labels set must always equal
    // the label set of EffectiveFlow.Steps. If these diverge, JSON /
    // Mermaid metadata projections disagree on which nodes are in the
    // slice — the outgoing-metadata-correctness concern from the
    // maintainer notes.
    var stage1 = ItemFactory.Singleton.Memory<int>("asl-stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("asl-stage2");
    var stage3 = ItemFactory.Singleton.Memory<int>("asl-stage3");
    await stage1.Save(1).Run();

    var captured = new List<FlowMetadataContext>();
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterFlow("A", () => FlowBuilder.CreateFlow("A", p =>
        p.AddStep<int, int>("a-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("B", () => FlowBuilder.CreateFlow("B", p =>
        p.AddStep<int, int>("b-step", x => x * 2, stage2, stage3)
      ));
      b.ConfigureMetadata(m => m.AddProvider(new CapturingContextProvider(captured)));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync("A");
    Assert.That(result.IsSuccess, Is.True);

    var ctx = captured.Single();
    var effectiveLabels = ctx.EffectiveFlow.Steps.Select(s => s.Label).ToHashSet(StringComparer.Ordinal);
    Assert.That(ctx.ActiveStepLabels, Is.EquivalentTo(effectiveLabels),
      "ActiveStepLabels must agree with EffectiveFlow.Steps' labels — divergence would scramble JSON / Mermaid slice membership.");
  }

  // ── Slice keeps merged-DAG visibility (full graph remains accessible) ──

  [Test]
  public async Task Slice_KeepsMergedFlowFullEvenWhenEffectiveFlowIsSmaller()
  {
    // Consumers that want to draw the full graph with the active slice
    // highlighted depend on MergedFlow always being the full topology,
    // even under a slice. Verify it doesn't shrink to the slice.
    var stage1 = ItemFactory.Singleton.Memory<int>("kf-stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("kf-stage2");
    var stage3 = ItemFactory.Singleton.Memory<int>("kf-stage3");
    await stage1.Save(1).Run();

    var captured = new List<FlowMetadataContext>();
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterFlow("Upstream", () => FlowBuilder.CreateFlow("Upstream", p =>
        p.AddStep<int, int>("u-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("Downstream", () => FlowBuilder.CreateFlow("Downstream", p =>
        p.AddStep<int, int>("d-step", x => x * 2, stage2, stage3)
      ));
      b.ConfigureMetadata(m => m.AddProvider(new CapturingContextProvider(captured)));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync("Upstream");
    Assert.That(result.IsSuccess, Is.True);

    var ctx = captured.Single();
    Assert.Multiple(() =>
    {
      Assert.That(ctx.MergedFlow.Steps.Select(s => s.Label),
        Is.EquivalentTo(new[] { "u-step", "d-step" }),
        "MergedFlow.Steps must remain the full union regardless of slicing.");
      Assert.That(ctx.EffectiveFlow.Steps.Select(s => s.Label),
        Is.EquivalentTo(new[] { "u-step" }),
        "EffectiveFlow.Steps shrinks to the slice.");
    });
  }

  // ── Slice attribution correctness (phantom 'merged' flow regression) ───

  /// <summary>
  /// CRITICAL REGRESSION TEST. Under a slice, every step in
  /// <see cref="FlowMetadataContext.MergedFlow"/> (active or inactive)
  /// and in <see cref="FlowMetadataContext.EffectiveFlow"/> must still
  /// name its pre-merge declaring flow on <c>FlowLabel</c>. A regression
  /// would silently scramble flow attribution across JSON / Mermaid /
  /// dashboards — particularly because slicing intersects with the
  /// merge step, exactly where the phantom-merged-flow bug surfaced.
  /// </summary>
  [Test]
  public async Task Slice_DoesNotPhantomMergeStepFlowLabels()
  {
    var stage1 = ItemFactory.Singleton.Memory<int>("sa-stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("sa-stage2");
    var stage3 = ItemFactory.Singleton.Memory<int>("sa-stage3");
    await stage1.Save(1).Run();

    var captured = new List<FlowMetadataContext>();
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterFlow("DataEngineering", () => FlowBuilder.CreateFlow("DataEngineering", p =>
        p.AddStep<int, int>("de-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("DataScience", () => FlowBuilder.CreateFlow("DataScience", p =>
        p.AddStep<int, int>("ds-step", x => x * 2, stage2, stage3)
      ));
      b.ConfigureMetadata(m => m.AddProvider(new CapturingContextProvider(captured)));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync("DataScience");
    Assert.That(result.IsSuccess, Is.True);

    var ctx = captured.Single();

    var mergedAttribution = ctx.MergedFlow.Steps
      .ToDictionary(s => s.Label, s => s.FlowLabel, StringComparer.Ordinal);
    var effectiveAttribution = ctx.EffectiveFlow.Steps
      .ToDictionary(s => s.Label, s => s.FlowLabel, StringComparer.Ordinal);

    Assert.Multiple(() =>
    {
      Assert.That(mergedAttribution["de-step"], Is.EqualTo("DataEngineering"),
        "MergedFlow's de-step must still attribute to its pre-merge flow.");
      Assert.That(mergedAttribution["ds-step"], Is.EqualTo("DataScience"),
        "MergedFlow's ds-step must still attribute to its pre-merge flow.");
      Assert.That(effectiveAttribution["de-step"], Is.EqualTo("DataEngineering"),
        "EffectiveFlow's de-step (pulled in by upstream walk) must still attribute to DataEngineering — not 'merged' or 'DataScience'.");
      Assert.That(effectiveAttribution["ds-step"], Is.EqualTo("DataScience"),
        "EffectiveFlow's ds-step must still attribute to DataScience.");

      // Sentinel checks for the phantom-merged-flow bug, in both the
      // merged and the effective (sliced) views.
      Assert.That(mergedAttribution.Values, Has.None.EqualTo("__merged__"),
        "MergedFlow must never attribute any step to '__merged__' — that's the merged-DAG's own label, not a step's flow of origin.");
      Assert.That(effectiveAttribution.Values, Has.None.EqualTo("__merged__"),
        "EffectiveFlow must never attribute any step to '__merged__' — slicing must not synthesize merged attribution.");
      Assert.That(mergedAttribution.Values, Has.None.EqualTo("merged"),
        "MergedFlow must never attribute any step to 'merged' — phantom-merged-flow regression sentinel.");
      Assert.That(effectiveAttribution.Values, Has.None.EqualTo("merged"),
        "EffectiveFlow must never attribute any step to 'merged' — phantom-merged-flow regression sentinel.");
    });
  }

  // ── ActiveStepLabels accuracy under a composed (multi-criterion) slice ─

  [Test]
  public async Task ComposedSlice_RequestedAndUpstreamFlowsBothActive()
  {
    // Equivalent of main's `composed slice` (multi-criterion). On the
    // FP rewrite the "composition" is the natural slice walking
    // upstream from the requested flow's outputs across cross-flow
    // edges — so requesting flow B activates steps from both A and B
    // when A's outputs feed B's inputs. The slice descriptor downstream
    // tooling reads is the same FlowMetadataContext fields.
    var stage1 = ItemFactory.Singleton.Memory<int>("cs-stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("cs-stage2");
    var stage3 = ItemFactory.Singleton.Memory<int>("cs-stage3");
    var stage4 = ItemFactory.Singleton.Memory<int>("cs-stage4");
    await stage1.Save(1).Run();

    var captured = new List<FlowMetadataContext>();
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      // Three-flow topology: A → B → C.
      b.RegisterFlow("A", () => FlowBuilder.CreateFlow("A", p =>
        p.AddStep<int, int>("a-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("B", () => FlowBuilder.CreateFlow("B", p =>
        p.AddStep<int, int>("b-step", x => x * 2, stage2, stage3)
      ));
      b.RegisterFlow("C", () => FlowBuilder.CreateFlow("C", p =>
        p.AddStep<int, int>("c-step", x => x - 3, stage3, stage4)
      ));
      b.ConfigureMetadata(m => m.AddProvider(new CapturingContextProvider(captured)));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    // Request the middle flow B. The slice must include A (upstream
    // producer) and B itself — but not C (downstream consumer).
    var result = await flowthru.RunAsync("B");
    Assert.That(result.IsSuccess, Is.True);

    var ctx = captured.Single();
    Assert.Multiple(() =>
    {
      Assert.That(ctx.RequestedFlowLabel, Is.EqualTo("B"));
      Assert.That(ctx.ActiveStepLabels,
        Is.EquivalentTo(new[] { "a-step", "b-step" }),
        "Composed slice (B + its upstream walk) should activate A's and B's steps but not C's.");
      Assert.That(ctx.MergedFlow.Steps.Select(s => s.Label),
        Is.EquivalentTo(new[] { "a-step", "b-step", "c-step" }),
        "MergedFlow still carries the full three-flow topology, so consumers can render the full graph with the slice highlighted.");
    });
  }
}

/// <summary>
/// Metadata provider that snapshots every <see cref="FlowMetadataContext"/>
/// it receives. Used by the DagSliceMetadata tests to inspect the
/// outgoing-metadata surface JSON / Mermaid providers would read.
/// </summary>
internal sealed class CapturingContextProvider : IMetadataProvider
{
  private readonly List<FlowMetadataContext> _capture;

  public CapturingContextProvider(List<FlowMetadataContext> capture)
  {
    _capture = capture ?? throw new ArgumentNullException(nameof(capture));
  }

  public string ProviderId => "capturing-context";

  public FlowIO<FlowUnit> Emit(FlowMetadataContext ctx)
  {
    _capture.Add(ctx);
    return FlowIO.Pure(FlowUnit.Default);
  }
}
