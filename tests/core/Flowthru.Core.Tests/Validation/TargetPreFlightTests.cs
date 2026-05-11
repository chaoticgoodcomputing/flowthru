using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;

namespace Flowthru.Core.Tests.Validation;

/// <summary>
/// Tests for target-aware pre-flight semantics ported from the legacy
/// <c>TargetPreFlightTests</c> (gap #3 in the test-coverage gap analysis).
/// </summary>
/// <remarks>
/// <para>
/// The legacy API expressed "inspect the destination" via a dedicated
/// <c>ValidateExternalInputsAsync</c> pass and a per-flow
/// <c>ValidationOptions.Inspect</c> toggle. In the FP rewrite the same
/// behaviour reaches the surface through:
/// </para>
/// <list type="bullet">
/// <item><see cref="InspectionLevel.Target"/> — when set, the pipeline
///   calls <c>InspectTarget()</c> instead of <c>InspectShallow()</c>
///   for each external input.</item>
/// <item><see cref="CatalogItemExtensions.WithMaxInspectionLevel{T}"/> —
///   per-item ceiling lets a catalog author opt an item down to
///   <see cref="InspectionLevel.None"/> (the equivalent of the legacy
///   <c>SkipTargetInspection()</c> escape hatch).</item>
/// <item>Slicing — items become "external" or "internal" depending on
///   whether their producer is part of the slice.</item>
/// </list>
/// <para>
/// In addition to the original five behavioural contracts (failure
/// propagation, error attribution, source short-circuit, can-inspect
/// skip, skip-target escape hatch), this fixture pins the three
/// maintainer-required cases:
/// </para>
/// <list type="number">
/// <item><strong>Memory-only input slices</strong> — a slice whose
///   external inputs are all in-memory passes pre-flight without
///   probing producers that have been sliced out.</item>
/// <item><strong>Slice-aware external/internal reclassification</strong>
///   — data internal to the full DAG (producer in DAG) becomes
///   external once that producer is sliced out.</item>
/// <item><strong>Metadata attribution accuracy</strong> — a step in a
///   sliced or merged DAG keeps its defining flow's <c>FlowLabel</c>,
///   never a phantom "merged" attribution.</item>
/// </list>
/// </remarks>
[TestFixture]
[Category("Validation")]
[Category("PreFlight")]
public class TargetPreFlightTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // (1) Failing target inspection causes invalid result
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Run_WithFailingTarget_AggregatesAsInspectionFailed()
  {
    // Arrange: external input whose InspectTarget returns a failure.
    // Use InspectionLevel.Target so the pipeline dispatches to
    // InspectTarget() on each external input.
    var input = MakeItem("input", new FailingTargetAdapter("input"));
    var output = ItemFactory.Singleton.Memory<int>("output");

    var flow = FlowBuilder.CreateFlow("target-failure", b =>
      b.AddStep<int, int>("step", x => x, input, output)
    );

    // Act
    var result = await PreFlightPipeline
      .Run(flow, inspectionLevel: InspectionLevel.Target)
      .Run();

    // Assert
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;
    Assert.That(inner, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)inner;
    Assert.That(invalid.Errors, Has.Count.EqualTo(1));
    Assert.That(invalid.Errors[0], Is.InstanceOf<PreFlightError.InspectionFailed>());
    var error = (PreFlightError.InspectionFailed)invalid.Errors[0];
    Assert.That(error.ItemId, Is.EqualTo("input"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (2) Healthy source + failing target — only target error surfaces
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Run_HealthySource_FailingTarget_OnlyTargetErrorSurfaces()
  {
    // Arrange: passing item alongside a failing-target item; both probed
    // under InspectionLevel.Target. The passing one contributes no
    // errors; the failing one contributes exactly one.
    var healthy = MakeItem("healthy", new PassingAdapter("healthy"));
    var failing = MakeItem("failing", new FailingTargetAdapter("failing"));
    var outA = ItemFactory.Singleton.Memory<int>("outA");
    var outB = ItemFactory.Singleton.Memory<int>("outB");

    var flow = FlowBuilder.CreateFlow("healthy-and-failing", b =>
    {
      b.AddStep<int, int>("a", x => x, healthy, outA);
      b.AddStep<int, int>("b", x => x, failing, outB);
    });

    // Act
    var result = await PreFlightPipeline
      .Run(flow, inspectionLevel: InspectionLevel.Target)
      .Run();
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    // Assert: exactly one error, attributed to the failing item.
    Assert.That(inner, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)inner;
    Assert.That(invalid.Errors, Has.Count.EqualTo(1));
    var first = (PreFlightError.InspectionFailed)invalid.Errors[0];
    Assert.That(first.ItemId, Is.EqualTo("failing"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (3) CanInspect-equivalent: WithMaxInspectionLevel(None) skips probing.
  // The new API expresses "this item is non-inspectable" via the cap
  // rather than the legacy StorageTraits.CanInspect flag.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Run_ItemCappedAtNone_TargetInspectionIsSkipped()
  {
    // Arrange: adapter that would fail target inspection, capped at None.
    // The cap is the FP-rewrite equivalent of CanInspect = false on the
    // legacy StorageTraits: the pipeline must not call InspectTarget on
    // an item whose effective level is None.
    var failing = MakeItem("failing", new FailingTargetAdapter("failing"))
      .WithMaxInspectionLevel(InspectionLevel.None);
    var output = ItemFactory.Singleton.Memory<int>("output");

    var flow = FlowBuilder.CreateFlow("skip-via-cap", b =>
      b.AddStep<int, int>("step", x => x, failing, output)
    );

    // Act
    var result = await PreFlightPipeline
      .Run(flow, inspectionLevel: InspectionLevel.Target)
      .Run();
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    // Assert: cap honoured, no errors.
    Assert.That(inner.IsValid, Is.True,
      "WithMaxInspectionLevel(None) must skip the adapter probe entirely — the "
      + "FP-rewrite replacement for the legacy CanInspect = false escape hatch.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (4) SkipTargetInspection-equivalent: per-item escape hatch via cap.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Run_SkipTargetInspection_ViaCap_SuppressesFailure()
  {
    // Arrange: opt one of two items out via the per-item cap.
    var failing = MakeItem("failing", new FailingTargetAdapter("failing"))
      .WithMaxInspectionLevel(InspectionLevel.None);
    var healthy = MakeItem("healthy", new PassingAdapter("healthy"));

    var outA = ItemFactory.Singleton.Memory<int>("outA");
    var outB = ItemFactory.Singleton.Memory<int>("outB");

    var flow = FlowBuilder.CreateFlow("escape-hatch", b =>
    {
      b.AddStep<int, int>("opt-out", x => x, failing, outA);
      b.AddStep<int, int>("opt-in", x => x, healthy, outB);
    });

    // Act
    var result = await PreFlightPipeline
      .Run(flow, inspectionLevel: InspectionLevel.Target)
      .Run();
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    // Assert
    Assert.That(inner.IsValid, Is.True,
      "Opting a single item down to InspectionLevel.None suppresses its "
      + "target-inspection failure without affecting other items.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Maintainer (a) — Memory-only input slices.
  // A slice whose external inputs are all in-memory passes pre-flight
  // without attempting filesystem checks on excluded producers.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Slice_MemoryOnlyExternalInputs_PassesPreFlightWithoutProbingExcludedProducers()
  {
    // Arrange a 3-step pipeline:
    //   raw  --producer-->  mid  --consumer-->  final
    //   raw  --side step--> sideOnly
    // Slice to "mainOut": keeps producer + consumer; drops the side
    // step. The side-step's input (raw) is in-memory and would inspect
    // as "missing" without being written; the slice must not probe
    // adapters that aren't on the path to the requested target.
    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var mid = ItemFactory.Singleton.Memory<int>("mid");
    var mainOut = ItemFactory.Singleton.Memory<int>("mainOut");
    var sideOnly = ItemFactory.Singleton.Memory<int>("sideOnly");

    // Save raw so the "consumer" path passes shallow inspection.
    await raw.Save(1).Run();

    var fullFlow = FlowBuilder.CreateFlow("slice-mem-only", b =>
    {
      b.AddStep<int, int>("producer", x => x + 1, raw, mid);
      b.AddStep<int, int>("consumer", x => x * 2, mid, mainOut);
      b.AddStep<int, int>("side", x => x + 1000, raw, sideOnly);
    });

    // Build the slice manually via FlowSliceStrategy so the test can
    // reason about pre-flight on the sliced topology directly.
    var producerByLabel = fullFlow.Steps
      .SelectMany(s => s.Outputs.Select(o => (o.Label, Step: s)))
      .ToDictionary(t => t.Label, t => t.Step);
    var slicedSteps = FlowSlicing.SliceTo(
      fullFlow.Steps,
      producerByLabel,
      new[] { "mainOut" }
    );
    Assert.That(slicedSteps.Select(s => s.Label),
      Is.EquivalentTo(new[] { "producer", "consumer" }),
      "Slice should drop the 'side' step.");

    var slicedFlow = new BuiltFlow("slice-mem-only", slicedSteps, producerByLabel);

    // Act
    var result = await PreFlightPipeline.Run(slicedFlow).Run();
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    // Assert: pre-flight passes — `sideOnly` (would fail because empty)
    // is dropped by the slice and `mid` is internal to the slice (not
    // probed). Only `raw` is external and it was saved.
    Assert.That(inner.IsValid, Is.True,
      "A slice whose external inputs are all readable must pass pre-flight "
      + "without probing adapters outside the slice.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Maintainer (b) — Slice-aware external/internal reclassification.
  // An item that is INTERNAL under a full-DAG run becomes EXTERNAL when
  // that producer is sliced out. Pre-flight must reflect the slice.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Slice_Producer_OutsideSlice_ReclassifiesItsOutputAsExternal()
  {
    // Two-step chain: producer → consumer over `mid`.
    // Under the full DAG, `mid` is INTERNAL (producer is part of the DAG).
    // Under a slice that keeps only `consumer`, `mid` becomes EXTERNAL.
    // The adapter for `mid` MUST be probed in the second case and MUST NOT
    // be probed in the first.
    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var midProbe = new ProbeCountingItem<int>("mid");
    var final = ItemFactory.Singleton.Memory<int>("final");

    await raw.Save(1).Run();

    var fullFlow = FlowBuilder.CreateFlow("reclass-full", b =>
    {
      b.AddStep<int, int>("producer", x => x, raw, midProbe);
      b.AddStep<int, int>("consumer", x => x, midProbe, final);
    });

    // Full DAG: `mid` is INTERNAL — the pipeline must not probe its adapter.
    var fullResult = await PreFlightPipeline.Run(fullFlow).Run();
    Assert.That(((EffResult<Validated<PreFlightError, FlowUnit>>.Success)fullResult).Value.IsValid, Is.True);
    Assert.That(midProbe.InspectShallowCount, Is.EqualTo(0),
      "Under the full DAG, `mid` is internal (producer in slice) — its "
      + "adapter must not be probed at pre-flight time.");

    // Now slice to keep only the consumer step. `mid` is now EXTERNAL
    // — the pipeline MUST probe its adapter.
    var producerByLabel = fullFlow.Steps
      .SelectMany(s => s.Outputs.Select(o => (o.Label, Step: s)))
      .ToDictionary(t => t.Label, t => t.Step);
    var consumerOnly = FlowSlicing.SliceTo(
      fullFlow.Steps.Where(s => s.Label == "consumer").ToList(),
      producerByLabel,
      new[] { "final" }
    );
    var slicedFlow = new BuiltFlow("reclass-slice", consumerOnly, producerByLabel);

    midProbe.Reset();
    var slicedResult = await PreFlightPipeline.Run(slicedFlow).Run();
    Assert.That(((EffResult<Validated<PreFlightError, FlowUnit>>.Success)slicedResult).Value.IsValid, Is.True,
      "midProbe.InspectShallow returns Success — slice should pass pre-flight.");
    Assert.That(midProbe.InspectShallowCount, Is.EqualTo(1),
      "Under the slice that drops `producer`, `mid` is external — its "
      + "adapter must be probed at pre-flight time.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Maintainer (c) — Metadata attribution accuracy.
  // A step in a sliced DAG must remain attributed to the flow that DEFINED
  // it, never to a phantom "merged" flow. Cross-cutting concern: Flow.Merge
  // (composing flows in a FlowthruService) must preserve per-step FlowLabel.
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public void Slice_Preserves_StepDefiningFlowLabel()
  {
    // Single flow with a label distinct from any sentinel. Slicing must
    // preserve `FlowLabel` on each surviving step.
    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var mid = ItemFactory.Singleton.Memory<int>("mid");
    var final = ItemFactory.Singleton.Memory<int>("final");

    var flow = FlowBuilder.CreateFlow("etl-pipeline", b =>
    {
      b.AddStep<int, int>("producer", x => x, raw, mid);
      b.AddStep<int, int>("consumer", x => x, mid, final);
    });

    var producerByLabel = flow.Steps
      .SelectMany(s => s.Outputs.Select(o => (o.Label, Step: s)))
      .ToDictionary(t => t.Label, t => t.Step);
    var slicedSteps = FlowSlicing.SliceTo(
      flow.Steps,
      producerByLabel,
      new[] { "final" }
    );

    foreach (var step in slicedSteps)
    {
      Assert.That(step.FlowLabel, Is.EqualTo("etl-pipeline"),
        $"Sliced step '{step.Label}' must keep its defining flow label "
        + "('etl-pipeline'), never a phantom 'merged' attribution.");
    }
  }

  [Test]
  public void Merge_Preserves_PreMergeFlowLabel_OnEveryStep()
  {
    // Cross-cutting concern (phantom "merged" flow attribution):
    // Merging two flows must not relabel steps. Each step's FlowLabel
    // must still name its pre-merge defining flow — never "merged" or
    // any synthesized container label.
    //
    // This test simulates the merging done by FlowthruService.BuildMergedFlow:
    // it concatenates the steps from two separately-built flows into a
    // third BuiltFlow whose label is "__merged__". The per-step
    // FlowLabel set at AddStep time (captured from the originating
    // FlowBuilder.Label) must survive the merge unchanged.
    var rawA = ItemFactory.Singleton.Memory<int>("rawA");
    var outA = ItemFactory.Singleton.Memory<int>("outA");
    var rawB = ItemFactory.Singleton.Memory<int>("rawB");
    var outB = ItemFactory.Singleton.Memory<int>("outB");

    var flowA = FlowBuilder.CreateFlow("alpha", b =>
      b.AddStep<int, int>("a-step", x => x, rawA, outA)
    );
    var flowB = FlowBuilder.CreateFlow("beta", b =>
      b.AddStep<int, int>("b-step", x => x, rawB, outB)
    );

    // Pre-merge: each step is attributed to its defining flow.
    Assert.That(flowA.Steps[0].FlowLabel, Is.EqualTo("alpha"));
    Assert.That(flowB.Steps[0].FlowLabel, Is.EqualTo("beta"));

    // Merge by union (the same way FlowthruService.BuildMergedFlow does).
    var allSteps = flowA.Steps.Concat(flowB.Steps).ToList();
    var producerByLabel = allSteps
      .SelectMany(s => s.Outputs.Select(o => (o.Label, Step: s)))
      .ToDictionary(t => t.Label, t => t.Step);
    var merged = new BuiltFlow("__merged__", allSteps, producerByLabel);

    // The merged container's label is "__merged__", but each step's
    // FlowLabel must STILL name its defining flow — never the merged
    // container, never any "merged" sentinel.
    var stepByLabel = merged.Steps.ToDictionary(s => s.Label);
    Assert.That(stepByLabel["a-step"].FlowLabel, Is.EqualTo("alpha"),
      "a-step was defined under flow 'alpha'; merge must not rewrite its "
      + "FlowLabel to 'merged' or '__merged__' (phantom-merged-flow bug).");
    Assert.That(stepByLabel["b-step"].FlowLabel, Is.EqualTo("beta"),
      "b-step was defined under flow 'beta'; merge must not rewrite its "
      + "FlowLabel to 'merged' or '__merged__' (phantom-merged-flow bug).");
    Assert.That(stepByLabel["a-step"].FlowLabel, Is.Not.EqualTo("merged"));
    Assert.That(stepByLabel["a-step"].FlowLabel, Is.Not.EqualTo("__merged__"));
    Assert.That(stepByLabel["b-step"].FlowLabel, Is.Not.EqualTo("merged"));
    Assert.That(stepByLabel["b-step"].FlowLabel, Is.Not.EqualTo("__merged__"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers and test doubles
  // ─────────────────────────────────────────────────────────────────────────

  private static IItem<int> MakeItem(string label, IStorageAdapter<int> adapter) =>
    new Item<int>(label, adapter);

  /// <summary>Adapter whose <c>InspectTarget</c> always reports a failure.</summary>
  private sealed class FailingTargetAdapter : IStorageAdapter<int>
  {
    private readonly string _label;

    public FailingTargetAdapter(string label) => _label = label;

    public StorageTraits Traits => new();

    public FlowIO<int> Load() => FlowIO.Pure(0);

    public FlowIO<FlowUnit> Save(int data) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(false);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectDeep() =>
      FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectTarget() =>
      FlowIO.Pure(ValidationResult.Failure(
        _label,
        ValidationErrorType.WriteAccessDenied,
        $"Simulated write-destination failure for '{_label}'"
      ));
  }

  /// <summary>Adapter whose every inspection method reports success.</summary>
  private sealed class PassingAdapter : IStorageAdapter<int>
  {
    private readonly string _label;

    public PassingAdapter(string label) => _label = label;

    public StorageTraits Traits => new();

    public FlowIO<int> Load() => FlowIO.Pure(0);

    public FlowIO<FlowUnit> Save(int data) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(true);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectDeep() =>
      FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectTarget() =>
      FlowIO.Pure(ValidationResult.Success());
  }

  /// <summary>
  /// IItem that records how many times each Inspect* method was called.
  /// Used to verify slice-aware external/internal reclassification —
  /// the pipeline must only probe items that are external to the slice
  /// being inspected.
  /// </summary>
  private sealed class ProbeCountingItem<T> : IItem<T>
  {
    public ProbeCountingItem(string label) { Label = label; }

    public string Label { get; }
    public NodeTraits Traits => new() { CanInspect = true };
    public Type DataType => typeof(T);

    public int InspectShallowCount { get; private set; }
    public int InspectDeepCount { get; private set; }
    public int InspectTargetCount { get; private set; }

    public void Reset()
    {
      InspectShallowCount = 0;
      InspectDeepCount = 0;
      InspectTargetCount = 0;
    }

    public FlowIO<T> Load() => FlowIO.Pure(default(T)!);
    public FlowIO<FlowUnit> Save(T data) => FlowIO.Pure(FlowUnit.Default);
    public FlowIO<bool> Exists() => FlowIO.Pure(true);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100)
    {
      InspectShallowCount++;
      return FlowIO.Pure(ValidationResult.Success());
    }

    public FlowIO<ValidationResult> InspectDeep()
    {
      InspectDeepCount++;
      return FlowIO.Pure(ValidationResult.Success());
    }

    public FlowIO<ValidationResult> InspectTarget()
    {
      InspectTargetCount++;
      return FlowIO.Pure(ValidationResult.Success());
    }

    public FlowIO<object> LoadUntyped() => Load().Map(v => (object)v!);
    public FlowIO<FlowUnit> SaveUntyped(object data) => Save((T)data);
    public FlowIO<ValidationResult> Validate() => InspectShallow();
  }
}
