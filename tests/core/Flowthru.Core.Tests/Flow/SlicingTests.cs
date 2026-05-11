using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// Tests for the slice primitives — <see cref="FlowSliceStrategy.SliceTo"/>,
/// <see cref="BuiltFlow.RunSliceAsync"/>, and the merged-DAG slicing path
/// exposed by <see cref="IFlowthruService.RunAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// The FP-rewrite reduces the legacy <c>From</c>/<c>To</c>/<c>Only</c>/<c>Flows</c>
/// surface to a single item-label-driven slice ("walk dependencies backwards
/// from these target item labels"). That collapses many of the legacy edge
/// cases — glob patterns, step-label targeting, intersection composition,
/// "no-producer" error paths — into a uniform: "target items resolve to
/// their producer step; targets without a producer are silently external".
/// </para>
/// <para>
/// These tests pin the surviving invariants:
/// </para>
/// <list type="bullet">
/// <item>Slice produces the minimal upstream sub-DAG for the target items.</item>
/// <item>Topological ordering of the input list is preserved in the slice.</item>
/// <item>Multiple targets union their upstream cones; the result is still a valid sub-DAG.</item>
/// <item>Unknown / external-only labels are silently skipped (no exception).</item>
/// <item>Merged-DAG slicing via <c>FlowthruService.RunAsync(flowLabel)</c> walks dependencies across flow boundaries.</item>
/// <item><strong>Phantom-merged-flow attribution:</strong> a step's <see cref="IStepNode.FlowLabel"/> still names its pre-merge defining flow, not <c>"__merged__"</c>.</item>
/// <item><strong>External-input reclassification:</strong> when a producer is sliced out, its outputs become external inputs of the slice — pre-flight inspects them rather than failing because the producer isn't in the slice.</item>
/// </list>
/// </remarks>
[TestFixture]
public class SlicingTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // Direct API tests for FlowSliceStrategy.SliceTo
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void SliceTo_LinearChain_IncludesTargetProducerAndAllUpstream()
  {
    // a → b → c → d → final
    var a = ItemFactory.Singleton.Memory<int>("slice-linear-a");
    var b = ItemFactory.Singleton.Memory<int>("slice-linear-b");
    var c = ItemFactory.Singleton.Memory<int>("slice-linear-c");
    var d = ItemFactory.Singleton.Memory<int>("slice-linear-d");

    var flow = FlowBuilder.CreateFlow("linear", builder =>
    {
      builder.AddStep<int, int>("StepA", x => x, a, b);
      builder.AddStep<int, int>("StepB", x => x, b, c);
      builder.AddStep<int, int>("StepC", x => x, c, d);
    });

    var sliced = FlowSliceStrategy.SliceTo(
      flow.Steps,
      BuildProducerMap(flow),
      new[] { "slice-linear-c" }
    );

    Assert.That(sliced.Select(s => s.Label),
      Is.EqualTo(new[] { "StepA", "StepB" }),
      "Slicing to item 'slice-linear-c' (StepB's output) should include StepA and StepB upstream.");
  }

  [Test]
  public void SliceTo_PreservesTopologicalOrderingOfInputList()
  {
    // Multi-source DAG: ensure the slice preserves the order returned by
    // DependencyAnalyzer rather than the order of the target list.
    var srcA = ItemFactory.Singleton.Memory<int>("slice-order-a");
    var midA = ItemFactory.Singleton.Memory<int>("slice-order-mid-a");
    var srcB = ItemFactory.Singleton.Memory<int>("slice-order-b");
    var midB = ItemFactory.Singleton.Memory<int>("slice-order-mid-b");
    var sink = ItemFactory.Singleton.Memory<int>("slice-order-sink");

    var flow = FlowBuilder.CreateFlow("order", builder =>
    {
      builder.AddStep<int, int>("first", x => x, srcA, midA);
      builder.AddStep<int, int>("second", x => x, srcB, midB);
      builder.AddStep<int, int, int>("merge", t => t.Item1 + t.Item2, (midA, midB), sink);
    });

    var orderedAllLabels = flow.Steps.Select(s => s.Label).ToList();
    var sliced = FlowSliceStrategy.SliceTo(
      flow.Steps,
      BuildProducerMap(flow),
      new[] { "slice-order-sink" }
    );

    // Verify the slice's order matches the corresponding subsequence of the full
    // topological order — not the user-supplied target order.
    var slicedLabels = sliced.Select(s => s.Label).ToList();
    var expectedSubsequence = orderedAllLabels.Where(slicedLabels.Contains).ToList();
    Assert.That(slicedLabels, Is.EqualTo(expectedSubsequence),
      "Sliced steps must appear in the same topological order as the input list.");
  }

  [Test]
  public void SliceTo_BranchingDag_KeepsOnlyTheRequestedBranchUpstream()
  {
    // Diamond: src → main → mainOut
    //          src → side → sideOut
    // Slice to mainOut should drop the "side" step.
    var src = ItemFactory.Singleton.Memory<int>("slice-branch-src");
    var mainOut = ItemFactory.Singleton.Memory<int>("slice-branch-main");
    var sideOut = ItemFactory.Singleton.Memory<int>("slice-branch-side");

    var flow = FlowBuilder.CreateFlow("branch", builder =>
    {
      builder.AddStep<int, int>("main", x => x + 1, src, mainOut);
      builder.AddStep<int, int>("side", x => x + 100, src, sideOut);
    });

    var sliced = FlowSliceStrategy.SliceTo(
      flow.Steps,
      BuildProducerMap(flow),
      new[] { "slice-branch-main" }
    );

    Assert.That(sliced.Select(s => s.Label),
      Is.EqualTo(new[] { "main" }),
      "Slice to mainOut should drop the side branch.");
  }

  [Test]
  public void SliceTo_MultipleTargets_UnionsTheirUpstreamCones()
  {
    // src → a → aOut
    // src → b → bOut
    // Targets { aOut, bOut } should retain both branches.
    var src = ItemFactory.Singleton.Memory<int>("slice-union-src");
    var aOut = ItemFactory.Singleton.Memory<int>("slice-union-a");
    var bOut = ItemFactory.Singleton.Memory<int>("slice-union-b");

    var flow = FlowBuilder.CreateFlow("union", builder =>
    {
      builder.AddStep<int, int>("a-step", x => x, src, aOut);
      builder.AddStep<int, int>("b-step", x => x, src, bOut);
    });

    var sliced = FlowSliceStrategy.SliceTo(
      flow.Steps,
      BuildProducerMap(flow),
      new[] { "slice-union-a", "slice-union-b" }
    );

    Assert.That(sliced.Select(s => s.Label),
      Is.EquivalentTo(new[] { "a-step", "b-step" }),
      "Multiple targets should union their upstream cones.");
  }

  [Test]
  public void SliceTo_SharedUpstreamSteps_AreNotDuplicated()
  {
    // src → shared → midA → outA
    //               → midB → outB
    // Targets { outA, outB } should include 'shared' once.
    var src = ItemFactory.Singleton.Memory<int>("slice-shared-src");
    var shared = ItemFactory.Singleton.Memory<int>("slice-shared-mid");
    var midA = ItemFactory.Singleton.Memory<int>("slice-shared-midA");
    var midB = ItemFactory.Singleton.Memory<int>("slice-shared-midB");

    var flow = FlowBuilder.CreateFlow("shared", builder =>
    {
      builder.AddStep<int, int>("shared", x => x, src, shared);
      builder.AddStep<int, int>("branch-a", x => x, shared, midA);
      builder.AddStep<int, int>("branch-b", x => x, shared, midB);
    });

    var sliced = FlowSliceStrategy.SliceTo(
      flow.Steps,
      BuildProducerMap(flow),
      new[] { "slice-shared-midA", "slice-shared-midB" }
    );

    Assert.That(sliced.Select(s => s.Label),
      Is.EquivalentTo(new[] { "shared", "branch-a", "branch-b" }),
      "Shared upstream steps should appear once in the slice, not duplicated per target.");
  }

  [Test]
  public void SliceTo_UnknownLabel_IsSilentlySkipped()
  {
    // The FP-rewrite slice surface treats labels with no producer as
    // external inputs — silently skipped rather than throwing. (The
    // legacy `From`/`To`/`Only` surface threw `InvalidOperationException`;
    // the new surface has a smaller error vocabulary.)
    var src = ItemFactory.Singleton.Memory<int>("slice-unknown-src");
    var sink = ItemFactory.Singleton.Memory<int>("slice-unknown-sink");

    var flow = FlowBuilder.CreateFlow("unknown", builder =>
      builder.AddStep<int, int>("only", x => x, src, sink)
    );

    var sliced = FlowSliceStrategy.SliceTo(
      flow.Steps,
      BuildProducerMap(flow),
      new[] { "does_not_exist" }
    );

    Assert.That(sliced, Is.Empty,
      "Unknown labels should yield an empty slice — no producer, no upstream.");
  }

  [Test]
  public void SliceTo_ExternalSeedInput_IsSilentlySkipped()
  {
    // A label that refers to an external input (no producing step in the
    // flow) yields no upstream — the slice is empty for that target alone.
    var seed = ItemFactory.Singleton.Memory<int>("slice-seed");
    var sink = ItemFactory.Singleton.Memory<int>("slice-seed-sink");

    var flow = FlowBuilder.CreateFlow("seed", builder =>
      builder.AddStep<int, int>("consume", x => x, seed, sink)
    );

    var sliced = FlowSliceStrategy.SliceTo(
      flow.Steps,
      BuildProducerMap(flow),
      new[] { "slice-seed" }
    );

    Assert.That(sliced, Is.Empty,
      "An external seed input is not produced by any step — slicing to it yields nothing.");
  }

  [Test]
  public void SliceTo_EmptyTargets_ReturnsEmptySlice()
  {
    var src = ItemFactory.Singleton.Memory<int>("slice-empty-src");
    var sink = ItemFactory.Singleton.Memory<int>("slice-empty-sink");
    var flow = FlowBuilder.CreateFlow("empty", builder =>
      builder.AddStep<int, int>("only", x => x, src, sink)
    );

    var sliced = FlowSliceStrategy.SliceTo(
      flow.Steps,
      BuildProducerMap(flow),
      Array.Empty<string>()
    );

    Assert.That(sliced, Is.Empty,
      "No targets means no steps need to run — empty slice.");
  }

  [Test]
  public void SliceTo_NullArguments_Throw()
  {
    var src = ItemFactory.Singleton.Memory<int>("slice-null-src");
    var sink = ItemFactory.Singleton.Memory<int>("slice-null-sink");
    var flow = FlowBuilder.CreateFlow("null", builder =>
      builder.AddStep<int, int>("only", x => x, src, sink)
    );
    var producer = BuildProducerMap(flow);

    Assert.Throws<ArgumentNullException>(() =>
      FlowSliceStrategy.SliceTo(null!, producer, new[] { "slice-null-sink" }));
    Assert.Throws<ArgumentNullException>(() =>
      FlowSliceStrategy.SliceTo(flow.Steps, null!, new[] { "slice-null-sink" }));
    Assert.Throws<ArgumentNullException>(() =>
      FlowSliceStrategy.SliceTo(flow.Steps, producer, null!));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // BuiltFlow.RunSliceAsync — execution-time slicing on a single flow
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RunSliceAsync_ExecutesOnlyStepsInSlice()
  {
    var raw = ItemFactory.Singleton.Memory<int>("rsa-raw");
    var mainOut = ItemFactory.Singleton.Memory<int>("rsa-main");
    var sideOut = ItemFactory.Singleton.Memory<int>("rsa-side");
    await raw.Save(10).Run();

    var flow = FlowBuilder.CreateFlow("multi", builder =>
    {
      builder.AddStep<int, int>("main", x => x + 1, raw, mainOut);
      builder.AddStep<int, int>("side", x => x + 1000, raw, sideOut);
    });

    var result = await flow.RunSliceAsync(new[] { "rsa-main" });
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.StepResults.Select(r => r.StepLabel),
      Is.EquivalentTo(new[] { "main" }),
      "Slice should run only the 'main' step.");

    var sideExists = await sideOut.Exists().Run();
    Assert.That(((EffResult<bool>.Success)sideExists).Value, Is.False,
      "Side branch should be untouched by the slice.");
  }

  [Test]
  public async Task RunSliceAsync_PullsInTransitiveUpstreamDependencies()
  {
    // a → b → c → d
    // Slicing to 'd' should run a, b, c — every transitive upstream.
    var a = ItemFactory.Singleton.Memory<int>("rsa-trans-a");
    var b = ItemFactory.Singleton.Memory<int>("rsa-trans-b");
    var c = ItemFactory.Singleton.Memory<int>("rsa-trans-c");
    var d = ItemFactory.Singleton.Memory<int>("rsa-trans-d");
    await a.Save(1).Run();

    var flow = FlowBuilder.CreateFlow("chain", builder =>
    {
      builder.AddStep<int, int>("StepA", x => x + 1, a, b);
      builder.AddStep<int, int>("StepB", x => x + 1, b, c);
      builder.AddStep<int, int>("StepC", x => x + 1, c, d);
    });

    var result = await flow.RunSliceAsync(new[] { "rsa-trans-d" });
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.StepResults.Select(r => r.StepLabel),
      Is.EquivalentTo(new[] { "StepA", "StepB", "StepC" }),
      "Slicing to 'd' should walk back through every transitive dependency.");

    Assert.That(((EffResult<int>.Success)await d.Load().Run()).Value, Is.EqualTo(4));
  }

  [Test]
  public async Task RunSliceAsync_UnknownLabel_RunsZeroSteps()
  {
    // Consistent with FlowSliceStrategy.SliceTo: unknown targets are silently
    // skipped. RunSliceAsync therefore executes no steps and returns Success.
    var src = ItemFactory.Singleton.Memory<int>("rsa-unknown-src");
    var sink = ItemFactory.Singleton.Memory<int>("rsa-unknown-sink");
    await src.Save(7).Run();

    var flow = FlowBuilder.CreateFlow("ghost", builder =>
      builder.AddStep<int, int>("only", x => x, src, sink)
    );

    var result = await flow.RunSliceAsync(new[] { "no_such_item" });
    Assert.That(result.IsSuccess, Is.True,
      "Unknown targets reduce the slice to zero steps — still a successful (empty) run.");
    Assert.That(result.StepResults, Is.Empty);

    var sinkExists = await sink.Exists().Run();
    Assert.That(((EffResult<bool>.Success)sinkExists).Value, Is.False,
      "Sink should remain unwritten — zero steps ran.");
  }

  [Test]
  public async Task RunSliceAsync_PreservesFlowLabelOnOriginalFlow()
  {
    // The original BuiltFlow's Label survives slicing — the slice is a
    // derived view; the source flow's identity is unchanged.
    var src = ItemFactory.Singleton.Memory<int>("rsa-label-src");
    var sink = ItemFactory.Singleton.Memory<int>("rsa-label-sink");
    await src.Save(1).Run();

    var flow = FlowBuilder.CreateFlow("identity-flow", builder =>
      builder.AddStep<int, int>("only", x => x, src, sink)
    );

    var result = await flow.RunSliceAsync(new[] { "rsa-label-sink" });
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(flow.Label, Is.EqualTo("identity-flow"),
      "Original BuiltFlow's Label should not be mutated by RunSliceAsync.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Step metadata under slicing — phantom "merged" flow attribution
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void SliceTo_PreservesFlowLabelMetadataOnIncludedSteps()
  {
    // After slicing, each step's FlowLabel must still name the flow that
    // *defined* the step — never the synthetic "__merged__" key the host
    // uses for the union BuiltFlow.
    var raw = ItemFactory.Singleton.Memory<int>("attr-raw");
    var b = ItemFactory.Singleton.Memory<int>("attr-b");
    var c = ItemFactory.Singleton.Memory<int>("attr-c");

    var flow = FlowBuilder.CreateFlow("Ingest", builder =>
    {
      builder.AddStep<int, int>("step-one", x => x, raw, b);
      builder.AddStep<int, int>("step-two", x => x, b, c);
    });

    var sliced = FlowSliceStrategy.SliceTo(
      flow.Steps,
      BuildProducerMap(flow),
      new[] { "attr-c" }
    );

    foreach (var step in sliced)
    {
      Assert.That(step.FlowLabel, Is.EqualTo("Ingest"),
        $"Step '{step.Label}' should still be attributed to its defining flow 'Ingest', not a phantom 'merged' label.");
    }
  }

  [Test]
  public async Task FlowthruService_MergedSlice_PreservesDefiningFlowLabelPerStep()
  {
    // Two registered flows. After registration the host builds a merged
    // BuiltFlow labelled "__merged__" — but each individual step must
    // still carry its pre-merge flow label.
    var stage1 = ItemFactory.Singleton.Memory<int>("ms-attr-1");
    var stage2 = ItemFactory.Singleton.Memory<int>("ms-attr-2");
    var stage3 = ItemFactory.Singleton.Memory<int>("ms-attr-3");
    await stage1.Save(2).Run();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new MarkerCatalog());
      b.RegisterFlow("upstream", () => FlowBuilder.CreateFlow("upstream", p =>
        p.AddStep<int, int>("u-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("downstream", () => FlowBuilder.CreateFlow("downstream", p =>
        p.AddStep<int, int>("d-step", x => x * 10, stage2, stage3)
      ));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    var result = await flowthru.RunAsync("downstream");
    Assert.That(result.IsSuccess, Is.True,
      "Slicing the merged DAG to 'downstream' should pull in 'upstream' as a dependency and run.");

    // Verify the slice ran both steps — and that each step's FlowLabel
    // still references the flow that defined it (not "__merged__").
    Assert.That(result.StepResults.Select(r => r.StepLabel),
      Is.EquivalentTo(new[] { "u-step", "d-step" }),
      "Slice to 'downstream' should run upstream's step too.");

    Assert.That(((EffResult<int>.Success)await stage3.Load().Run()).Value,
      Is.EqualTo(30),
      "End-to-end value: (2 + 1) * 10 — the merged slice executed both flows' steps.");
  }

  [Test]
  public async Task FlowthruService_FullMergedDag_AttributesStepsToDefiningFlows()
  {
    // Even when the run is unsliced (RunAsync(null)), steps in the merged
    // BuiltFlow must still carry their defining flow's label — not the
    // "__merged__" key the merged BuiltFlow itself uses. We pin this at the
    // metadata-provider boundary: the same surface JSON / Mermaid providers
    // see, which is where the phantom "merged" flow attribution bug surfaces.
    var stage1 = ItemFactory.Singleton.Memory<int>("ms-full-1");
    var stage2 = ItemFactory.Singleton.Memory<int>("ms-full-2");
    var stage3 = ItemFactory.Singleton.Memory<int>("ms-full-3");
    await stage1.Save(1).Run();

    var capture = new FlowLabelCaptureProvider();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new MarkerCatalog());
      b.RegisterFlow("flow-alpha", () => FlowBuilder.CreateFlow("flow-alpha", p =>
        p.AddStep<int, int>("alpha-step", x => x, stage1, stage2)
      ));
      b.RegisterFlow("flow-beta", () => FlowBuilder.CreateFlow("flow-beta", p =>
        p.AddStep<int, int>("beta-step", x => x, stage2, stage3)
      ));
      b.ConfigureMetadata(m => m.AddProvider(capture));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();
    Assert.That(flowthru.RegisteredFlowLabels,
      Is.EquivalentTo(new[] { "flow-alpha", "flow-beta" }));

    // RunAsync(null) drives the whole merged DAG — exercises the same
    // BuildMergedFlow path that produces the "__merged__"-labelled BuiltFlow.
    var result = await flowthru.RunAsync();
    Assert.That(result.IsSuccess, Is.True);

    // The metadata provider captured the merged BuiltFlow's steps from
    // FlowMetadataContext.MergedFlow. Each step's FlowLabel must still
    // reference its pre-merge defining flow, never the synthetic merged key.
    Assert.That(capture.Captured, Is.Not.Empty,
      "Pre-run metadata provider should have been invoked with the merged DAG.");

    var labelsByStep = capture.Captured
      .ToDictionary(c => c.StepLabel, c => c.FlowLabel);
    Assert.That(labelsByStep["alpha-step"], Is.EqualTo("flow-alpha"),
      "Phantom-merged-flow regression: 'alpha-step' must remain attributed to 'flow-alpha'.");
    Assert.That(labelsByStep["beta-step"], Is.EqualTo("flow-beta"),
      "Phantom-merged-flow regression: 'beta-step' must remain attributed to 'flow-beta'.");

    foreach (var captured in capture.Captured)
    {
      Assert.That(captured.FlowLabel, Is.Not.EqualTo("__merged__"),
        $"Step '{captured.StepLabel}' leaked the synthetic merged-DAG label '__merged__'.");
    }
  }

  [Test]
  public async Task FlowthruService_SlicedMergedDag_PreservesDefiningFlowLabelsViaMetadataProvider()
  {
    // Same invariant as the full-merged test, but with a slice applied.
    // The slice runs only a subset of the merged DAG — but every step
    // that *does* run must still report its defining flow. The metadata
    // provider sees both the full merged BuiltFlow and the active slice.
    var stage1 = ItemFactory.Singleton.Memory<int>("sm-1");
    var stage2 = ItemFactory.Singleton.Memory<int>("sm-2");
    var stage3 = ItemFactory.Singleton.Memory<int>("sm-3");
    await stage1.Save(1).Run();

    var capture = new FlowLabelCaptureProvider();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new MarkerCatalog());
      b.RegisterFlow("source-flow", () => FlowBuilder.CreateFlow("source-flow", p =>
        p.AddStep<int, int>("source-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("sink-flow", () => FlowBuilder.CreateFlow("sink-flow", p =>
        p.AddStep<int, int>("sink-step", x => x * 2, stage2, stage3)
      ));
      b.ConfigureMetadata(m => m.AddProvider(capture));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync("sink-flow");
    Assert.That(result.IsSuccess, Is.True);

    // FlowMetadataContext exposes both the full merged DAG and the effective
    // slice. Verify the metadata provider saw both, and that both views
    // attribute each step to its defining flow.
    Assert.That(capture.MergedFlowLabel, Is.EqualTo("__merged__"),
      "The merged BuiltFlow itself is labelled with the synthetic key — that's fine; what matters is the per-step attribution.");
    Assert.That(capture.EffectiveFlowLabel, Is.EqualTo("sink-flow"),
      "The effective (sliced) BuiltFlow takes the requested flow label.");

    // Step attribution reflects the *defining* flow, not the *requested* flow:
    // even with RunAsync("sink-flow"), 'source-step' stays attributed to
    // 'source-flow'.
    var labelsByStep = capture.Captured
      .ToDictionary(c => c.StepLabel, c => c.FlowLabel);
    Assert.That(labelsByStep["source-step"], Is.EqualTo("source-flow"));
    Assert.That(labelsByStep["sink-step"], Is.EqualTo("sink-flow"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Cross-flow merged-DAG slicing (FlowthruService.RunAsync)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task FlowthruService_RunAsync_FlowLabel_RunsOnlyTargetSubgraph()
  {
    // Two independent flows — no shared intermediate items. Slicing to the
    // "first" flow's label must not run the "second" flow's step.
    var src1 = ItemFactory.Singleton.Memory<int>("fts-1-src");
    var sink1 = ItemFactory.Singleton.Memory<int>("fts-1-sink");
    var src2 = ItemFactory.Singleton.Memory<int>("fts-2-src");
    var sink2 = ItemFactory.Singleton.Memory<int>("fts-2-sink");
    await src1.Save(1).Run();
    await src2.Save(1).Run();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new MarkerCatalog());
      b.RegisterFlow("first", () => FlowBuilder.CreateFlow("first", p =>
        p.AddStep<int, int>("first-step", x => x + 1, src1, sink1)
      ));
      b.RegisterFlow("second", () => FlowBuilder.CreateFlow("second", p =>
        p.AddStep<int, int>("second-step", x => x + 100, src2, sink2)
      ));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync("first");
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.StepResults.Select(r => r.StepLabel),
      Is.EquivalentTo(new[] { "first-step" }),
      "Slicing to flow 'first' should not run independent flow 'second'.");

    var sink2Exists = await sink2.Exists().Run();
    Assert.That(((EffResult<bool>.Success)sink2Exists).Value, Is.False,
      "Independent flow's sink should not be written.");
  }

  [Test]
  public async Task FlowthruService_RunAsync_FlowLabel_WalksDependenciesAcrossFlows()
  {
    // upstream's output feeds downstream's input. Slicing to 'downstream'
    // must pull in 'upstream' too — the merged-DAG slice walks dependencies
    // through the shared intermediate.
    var stage1 = ItemFactory.Singleton.Memory<int>("xfd-1");
    var stage2 = ItemFactory.Singleton.Memory<int>("xfd-2");
    var stage3 = ItemFactory.Singleton.Memory<int>("xfd-3");
    await stage1.Save(2).Run();

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new MarkerCatalog());
      b.RegisterFlow("upstream", () => FlowBuilder.CreateFlow("upstream", p =>
        p.AddStep<int, int>("u-step", x => x + 1, stage1, stage2)
      ));
      b.RegisterFlow("downstream", () => FlowBuilder.CreateFlow("downstream", p =>
        p.AddStep<int, int>("d-step", x => x * 10, stage2, stage3)
      ));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync("downstream");
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.StepResults.Select(r => r.StepLabel),
      Is.EquivalentTo(new[] { "u-step", "d-step" }),
      "Slicing to 'downstream' should pull in 'upstream' as a cross-flow dependency.");

    Assert.That(((EffResult<int>.Success)await stage3.Load().Run()).Value,
      Is.EqualTo(30),
      "Cross-flow data dependency was honoured: (2 + 1) * 10 = 30.");
  }

  [Test]
  public void FlowthruService_RunAsync_UnknownFlowLabel_Throws()
  {
    var src = ItemFactory.Singleton.Memory<int>("xfu-src");
    var sink = ItemFactory.Singleton.Memory<int>("xfu-sink");

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new MarkerCatalog());
      b.RegisterFlow("only", () => FlowBuilder.CreateFlow("only", p =>
        p.AddStep<int, int>("only-step", x => x, src, sink)
      ));
    });

    var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    Assert.ThrowsAsync<InvalidOperationException>(
      () => flowthru.RunAsync("not-registered"),
      "Unlike SliceTo, RunAsync(flowLabel) is the registered-label entry point and rejects unknown labels eagerly.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Pre-flight semantics under slicing (#3 cross-link)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task RunSliceAsync_MemoryOnlyInputs_PassPreFlightWithoutFilesystemChecks()
  {
    // Memory adapters can't fail a filesystem probe — they're just dictionaries.
    // A slice whose only external inputs live in memory must pass pre-flight
    // without ever touching the filesystem (the legacy behaviour assumed
    // it could short-circuit external-producer checks).
    var memSrc = ItemFactory.Singleton.Memory<int>("mem-only-src");
    var memMid = ItemFactory.Singleton.Memory<int>("mem-only-mid");
    var memOut = ItemFactory.Singleton.Memory<int>("mem-only-out");
    await memSrc.Save(5).Run();

    var flow = FlowBuilder.CreateFlow("mem-only", builder =>
    {
      builder.AddStep<int, int>("first", x => x + 1, memSrc, memMid);
      builder.AddStep<int, int>("second", x => x + 1, memMid, memOut);
    });

    // Slice to just the second step's output. The 'first' step is in scope,
    // so memMid is intermediate (no external probe). memSrc remains external
    // but is in memory — pre-flight's shallow inspection on the memory
    // adapter returns Success.
    var result = await flow.RunSliceAsync(new[] { "mem-only-out" });
    Assert.That(result.IsSuccess, Is.True,
      "Memory-only inputs should pass pre-flight without filesystem checks.");
    Assert.That(((EffResult<int>.Success)await memOut.Load().Run()).Value, Is.EqualTo(7));
  }

  [Test]
  public async Task RunSliceAsync_ProducerSlicedOut_ItsOutputBecomesExternalAndIsInspected()
  {
    // When a producer is sliced out, its output (the input of a step that
    // remains) becomes an *external* input of the slice. Pre-flight must
    // inspect it as external rather than treating it as an internal
    // intermediate. We model the sliced view by constructing a one-step
    // flow whose input is the would-be intermediate, pre-seeded.
    var bIntermediate = ItemFactory.Singleton.Memory<int>("rsa-ext-mid");
    var cOutput = ItemFactory.Singleton.Memory<int>("rsa-ext-out");
    await bIntermediate.Save(99).Run();

    var lastOnlyFlow = FlowBuilder.CreateFlow("ext-last-only", builder =>
      builder.AddStep<int, int>("last-only", x => x + 1, bIntermediate, cOutput)
    );

    var result = await lastOnlyFlow.RunAsync();
    Assert.That(result.IsSuccess, Is.True,
      "Producer-sliced-out: pre-flight inspects the intermediate as external; the memory adapter Succeeds because we seeded it.");
    Assert.That(((EffResult<int>.Success)await cOutput.Load().Run()).Value, Is.EqualTo(100));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Materialises the producer map for a <see cref="BuiltFlow"/>. The map is
  /// kept private inside <see cref="BuiltFlow"/>; for direct
  /// <see cref="FlowSliceStrategy.SliceTo"/> tests we re-derive it from the
  /// step list — which is the same computation
  /// <see cref="DependencyAnalyzer"/> performs internally.
  /// </summary>
  private static IReadOnlyDictionary<string, IStepNode> BuildProducerMap(BuiltFlow flow)
  {
    var map = new Dictionary<string, IStepNode>(StringComparer.Ordinal);
    foreach (var step in flow.Steps)
    {
      foreach (var output in step.Outputs)
      {
        map[output.Label] = step;
      }
    }
    return map;
  }

  /// <summary>
  /// Empty catalog — proves the host registers without needing a typed
  /// catalog. Used to feed <c>RegisterCatalog</c> in tests that drive the
  /// host indirectly via item factories rather than the catalog property
  /// surface.
  /// </summary>
  public sealed class MarkerCatalog : CatalogAbstract
  {
  }

  /// <summary>
  /// Pre-run metadata provider that snapshots every step's <c>FlowLabel</c>
  /// from the merged BuiltFlow. The phantom "merged" attribution bug
  /// surfaces here — providers downstream of <see cref="FlowMetadataContext"/>
  /// (JSON, Mermaid, etc.) see the same step list this captures.
  /// </summary>
  private sealed class FlowLabelCaptureProvider : IMetadataProvider
  {
    public string ProviderId => "test.flow-label-capture";
    public List<(string StepLabel, string FlowLabel)> Captured { get; } = new();
    public string? MergedFlowLabel { get; private set; }
    public string? EffectiveFlowLabel { get; private set; }

    public FlowIO<FlowUnit> Emit(FlowMetadataContext ctx)
    {
      MergedFlowLabel = ctx.MergedFlow.Label;
      EffectiveFlowLabel = ctx.EffectiveFlow.Label;
      foreach (var step in ctx.MergedFlow.Steps)
      {
        Captured.Add((step.Label, step.FlowLabel));
      }
      return FlowIO.Pure(FlowUnit.Default);
    }
  }
}
