using Flowthru.Caching;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Caching;

/// <summary>
/// Cascade rules, eligibility, schema-mismatch, and composite-identity
/// behaviour of <see cref="CachePlanBuilder"/>. Tests construct
/// <see cref="Step{TIn, TOut}"/> instances directly so they can vary
/// <c>codeVersion</c> and <c>ServiceDependencies</c> independently of
/// the source-generator path; items are
/// <see cref="FakeFingerprintItem{T}"/>s that let each test pin both
/// the fingerprint value and the <c>Exists()</c> verdict.
/// </summary>
/// <remarks>
/// Phase 8 changes:
///   * <c>CacheManifest</c> now has separate <c>Steps</c> and <c>Items</c>
///     maps (schema version 2). Fresh-path tests must seed both.
///   * Step composites compose input <em>item</em> fingerprints (not
///     parent step composites), so chain tests pin the new shape.
/// </remarks>
[TestFixture]
public class CachePlanBuilderTests
{
  private static readonly DateTimeOffset T = new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);

  [Test]
  public async Task EmptyFlow_ProducesEmptyPlan()
  {
    var flow = new BuiltFlow("empty", Array.Empty<IStepNode>(), new Dictionary<string, IStepNode>());
    var plan = await CachePlanBuilder.BuildAsync(flow, CacheManifest.Empty);

    Assert.That(plan.FreshStepLabels, Is.Empty);
    Assert.That(plan.StaleStepLabels, Is.Empty);
    Assert.That(plan.UncacheableStepLabels, Is.Empty);
    Assert.That(plan.NewStepFingerprints, Is.Empty);
    Assert.That(plan.NewItemFingerprints, Is.Empty);
  }

  [Test]
  public async Task SingleStep_MatchingManifestAndOutputsExist_IsFresh()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in-1", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out-1", exists: true);
    var step = MakeStep("transform", "code-v1", input, output);
    var flow = BuildFlow(step);

    var composite = CachePlanBuilder.ComposeStepFingerprint(
      "code-v1", new[] { ("in", "fp-in-1") });

    var manifest = Manifest(
      steps: new[] { ("transform", composite) },
      items: new[] { ("in", "fp-in-1") });

    var plan = await CachePlanBuilder.BuildAsync(flow, manifest);

    Assert.That(plan.FreshStepLabels, Is.EquivalentTo(new[] { "transform" }));
    Assert.That(plan.StaleStepLabels, Is.Empty);
    Assert.That(plan.UncacheableStepLabels, Is.Empty);
    Assert.That(plan.NewStepFingerprints["transform"], Is.EqualTo(composite));
    Assert.That(plan.NewItemFingerprints["in"], Is.EqualTo("fp-in-1"));
    Assert.That(plan.NewItemFingerprints["out"], Is.EqualTo("fp-out-1"),
      "Fresh-path output fingerprints are probed at pre-flight so the post-run upsert "
      + "records intermediate item identities alongside their producer's composite.");
  }

  [Test]
  public async Task SingleStep_MatchingManifestButOutputMissing_IsStale()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: false);
    var step = MakeStep("transform", "code-v1", input, output);
    var flow = BuildFlow(step);

    var composite = CachePlanBuilder.ComposeStepFingerprint(
      "code-v1", new[] { ("in", "fp-in") });
    var manifest = Manifest(
      steps: new[] { ("transform", composite) },
      items: new[] { ("in", "fp-in") });

    var plan = await CachePlanBuilder.BuildAsync(flow, manifest);

    Assert.That(plan.StaleStepLabels, Is.EquivalentTo(new[] { "transform" }),
      "Output missing on disk forces stale even when both composite and input "
      + "fingerprints match the manifest — we can't reuse data that isn't there.");
  }

  [Test]
  public async Task SingleStep_NoManifestEntry_IsStale()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep("transform", "code-v1", input, output);
    var flow = BuildFlow(step);

    var plan = await CachePlanBuilder.BuildAsync(flow, CacheManifest.Empty);

    Assert.That(plan.StaleStepLabels, Is.EquivalentTo(new[] { "transform" }),
      "First run: nothing recorded, so the step must run.");
  }

  [Test]
  public async Task SingleStep_NonMatchingStepComposite_IsStale()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep("transform", "code-v1", input, output);
    var flow = BuildFlow(step);

    // Input matches its manifest entry; only the step composite is wrong.
    var manifest = Manifest(
      steps: new[] { ("transform", "stale-composite") },
      items: new[] { ("in", "fp-in") });

    var plan = await CachePlanBuilder.BuildAsync(flow, manifest);

    Assert.That(plan.StaleStepLabels, Is.EquivalentTo(new[] { "transform" }));
  }

  [Test]
  public async Task SingleStep_NonMatchingInputFingerprint_CascadesStepStale()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in-new", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep("transform", "code-v1", input, output);
    var flow = BuildFlow(step);

    // Step composite happens to match, but the input's leaf fingerprint
    // doesn't — Phase 8 cascade rule forces the consumer stale before
    // the step composite is ever consulted.
    var composite = CachePlanBuilder.ComposeStepFingerprint(
      "code-v1", new[] { ("in", "fp-in-new") });
    var manifest = Manifest(
      steps: new[] { ("transform", composite) },
      items: new[] { ("in", "fp-in-old") });

    var plan = await CachePlanBuilder.BuildAsync(flow, manifest);

    Assert.That(plan.StaleStepLabels, Is.EquivalentTo(new[] { "transform" }),
      "An external input's leaf fingerprint mismatch makes the consuming step stale "
      + "via the cascade rule, independent of any step composite agreement.");
  }

  [Test]
  public async Task StepWithServiceDependencies_IsUncacheable()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep(
      label: "transform",
      codeVersion: "code-v1",
      input: input,
      output: output,
      serviceDependencies: new[] { ServiceRef.Of<object>() });
    var flow = BuildFlow(step);

    var plan = await CachePlanBuilder.BuildAsync(flow, CacheManifest.Empty);

    Assert.That(plan.UncacheableStepLabels, Is.EquivalentTo(new[] { "transform" }));
  }

  [Test]
  public async Task StepWithNullCodeVersion_IsUncacheable()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep("transform", codeVersion: null, input, output);
    var flow = BuildFlow(step);

    var plan = await CachePlanBuilder.BuildAsync(flow, CacheManifest.Empty);

    Assert.That(plan.UncacheableStepLabels, Is.EquivalentTo(new[] { "transform" }),
      "Without CodeVersion the framework cannot detect code changes — fail-safe is to "
      + "treat the step as uncacheable.");
  }

  [Test]
  public async Task StepWithUnfingerprintableInput_IsUncacheable()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: null, exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep("transform", "code-v1", input, output);
    var flow = BuildFlow(step);

    var plan = await CachePlanBuilder.BuildAsync(flow, CacheManifest.Empty);

    Assert.That(plan.UncacheableStepLabels, Is.EquivalentTo(new[] { "transform" }),
      "Inputs that don't implement ISupportsFingerprint (TryGetFingerprint returns null) "
      + "make their consuming step uncacheable.");
  }

  [Test]
  public async Task Cascade_StaleParentForcesChildStale()
  {
    var seedInput = new FakeFingerprintItem<int>("seed", fingerprint: "fp-seed", exists: true);
    var mid = new FakeFingerprintItem<int>("mid", fingerprint: "fp-mid", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);

    var stepA = MakeStep("A", "code-A-v1", seedInput, mid);
    var stepB = MakeStep("B", "code-B-v1", mid, output);
    var flow = BuildFlow(stepA, stepB);

    var plan = await CachePlanBuilder.BuildAsync(flow, CacheManifest.Empty);

    Assert.That(plan.StaleStepLabels, Is.EquivalentTo(new[] { "A", "B" }),
      "Cascade rule: any input produced by a non-fresh parent forces the consumer stale.");
    Assert.That(plan.FreshStepLabels, Is.Empty);
  }

  [Test]
  public async Task Cascade_UncacheableParentForcesChildUncacheable()
  {
    var seedInput = new FakeFingerprintItem<int>("seed", fingerprint: "fp-seed", exists: true);
    var mid = new FakeFingerprintItem<int>("mid", fingerprint: "fp-mid", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);

    var stepA = MakeStep("A", codeVersion: null, seedInput, mid);
    var stepB = MakeStep("B", "code-B-v1", mid, output);
    var flow = BuildFlow(stepA, stepB);

    var plan = await CachePlanBuilder.BuildAsync(flow, CacheManifest.Empty);

    Assert.That(plan.UncacheableStepLabels, Is.EquivalentTo(new[] { "A", "B" }),
      "Uncacheable parents cascade as uncacheable — B can never be safely cached because "
      + "we cannot identify A's outputs.");
  }

  [Test]
  public async Task TwoStepChain_BothFresh_DownstreamCompositeFoldsInputItemFingerprint()
  {
    var seedInput = new FakeFingerprintItem<int>("seed", fingerprint: "fp-seed", exists: true);
    var mid = new FakeFingerprintItem<int>("mid", fingerprint: "fp-mid", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);

    var stepA = MakeStep("A", "code-A-v1", seedInput, mid);
    var stepB = MakeStep("B", "code-B-v1", mid, output);
    var flow = BuildFlow(stepA, stepB);

    // Phase 8: composites fold in input <em>item</em> fingerprints,
    // not parent step composites. The mid item supplies its own
    // fingerprint to B's composite.
    var compositeA = CachePlanBuilder.ComposeStepFingerprint(
      "code-A-v1", new[] { ("seed", "fp-seed") });
    var compositeB = CachePlanBuilder.ComposeStepFingerprint(
      "code-B-v1", new[] { ("mid", "fp-mid") });

    var manifest = Manifest(
      steps: new[] { ("A", compositeA), ("B", compositeB) },
      items: new[] { ("seed", "fp-seed"), ("mid", "fp-mid") });

    var plan = await CachePlanBuilder.BuildAsync(flow, manifest);

    Assert.That(plan.FreshStepLabels, Is.EquivalentTo(new[] { "A", "B" }));
    Assert.That(plan.NewStepFingerprints["A"], Is.EqualTo(compositeA));
    Assert.That(plan.NewStepFingerprints["B"], Is.EqualTo(compositeB));
  }

  [Test]
  public async Task SchemaMismatchedManifest_TreatsEveryEntryAsAbsent()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep("transform", "code-v1", input, output);
    var flow = BuildFlow(step);

    // The manifest's saved values match what we'd compute now…
    var composite = CachePlanBuilder.ComposeStepFingerprint(
      "code-v1", new[] { ("in", "fp-in") });
    var staleSchemaManifest = new CacheManifest(
      CacheManifestSchema.CurrentVersion - 1,
      new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal)
      {
        ["transform"] = new NodeFingerprint(composite, T),
      },
      new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal)
      {
        ["in"] = new NodeFingerprint("fp-in", T),
      });

    var plan = await CachePlanBuilder.BuildAsync(flow, staleSchemaManifest);

    Assert.That(plan.StaleStepLabels, Is.EquivalentTo(new[] { "transform" }),
      "…but the schema-version mismatch invalidates every entry — first run after a "
      + "framework bump re-records every cacheable step.");
  }

  [Test]
  public async Task IndependentBranches_DoNotCascade()
  {
    var inA = new FakeFingerprintItem<int>("in-a", fingerprint: "fp-a", exists: true);
    var outA = new FakeFingerprintItem<int>("out-a", fingerprint: "fp-out-a", exists: true);
    var inB = new FakeFingerprintItem<int>("in-b", fingerprint: "fp-b", exists: true);
    var outB = new FakeFingerprintItem<int>("out-b", fingerprint: "fp-out-b", exists: true);

    var stepA = MakeStep("A", "code-A-v1", inA, outA);
    var stepB = MakeStep("B", "code-B-v1", inB, outB);
    var flow = BuildFlow(stepA, stepB);

    // Manifest has B's correct composite + B's input recorded; A has nothing.
    var compositeB = CachePlanBuilder.ComposeStepFingerprint(
      "code-B-v1", new[] { ("in-b", "fp-b") });
    var manifest = Manifest(
      steps: new[] { ("B", compositeB) },
      items: new[] { ("in-b", "fp-b") });

    var plan = await CachePlanBuilder.BuildAsync(flow, manifest);

    Assert.That(plan.FreshStepLabels, Is.EquivalentTo(new[] { "B" }));
    Assert.That(plan.StaleStepLabels, Is.EquivalentTo(new[] { "A" }));
  }

  // ── Uncacheable reason capture (regression: MagicAtlas Bug 3) ─────────

  [Test]
  public async Task UncacheableReason_NoCodeVersion_IsCapturedPerStep()
  {
    // Regression: pre-Bug-3, a step landing in UncacheableStepLabels carried
    // no machine-readable reason, so a 7-step cascade was indistinguishable
    // from a 7-step bag of unrelated misses. The plan now exposes
    // UncacheableReasons keyed by step label.
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep("transform", codeVersion: null, input, output);
    var plan = await CachePlanBuilder.BuildAsync(BuildFlow(step), CacheManifest.Empty);

    Assert.That(plan.UncacheableReasons, Does.ContainKey("transform"));
    Assert.That(plan.UncacheableReasons["transform"],
      Is.TypeOf<StepUncacheableReason.NoCodeVersion>());
  }

  [Test]
  public async Task UncacheableReason_ServiceDependencies_CarriesCount()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep(
      label: "transform", codeVersion: "code-v1",
      input: input, output: output,
      serviceDependencies: new[] { ServiceRef.Of<object>(), ServiceRef.Of<string>() });

    var plan = await CachePlanBuilder.BuildAsync(BuildFlow(step), CacheManifest.Empty);

    var reason = plan.UncacheableReasons["transform"];
    Assert.That(reason, Is.TypeOf<StepUncacheableReason.HasServiceDependencies>());
    Assert.That(((StepUncacheableReason.HasServiceDependencies)reason).Count, Is.EqualTo(2));
  }

  // ── ObservationOnly carve-out (ADR-0010) ────────────────────────────

  [Test]
  public async Task ObservationOnlyDeps_DoNotMarkStepUncacheable()
  {
    // ADR-0010: ServiceRef.ObservationOnly variants (e.g., ILogger)
    // are skipped when computing cache eligibility — observation
    // surfaces don't affect step output values, so their presence
    // can't invalidate a cached result.
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep(
      label: "transform", codeVersion: "code-v1",
      input: input, output: output,
      serviceDependencies: new ServiceRef[]
      {
        new ServiceRef.ObservationOnly(typeof(Microsoft.Extensions.Logging.ILogger)),
      });

    var composite = CachePlanBuilder.ComposeStepFingerprint(
      "code-v1", new[] { ("in", "fp-in") });
    var manifest = Manifest(
      steps: new[] { ("transform", composite) },
      items: new[] { ("in", "fp-in") });

    var plan = await CachePlanBuilder.BuildAsync(BuildFlow(step), manifest);

    Assert.That(plan.FreshStepLabels, Is.EquivalentTo(new[] { "transform" }),
      "A step with only observation-only deps must remain eligible for caching when "
      + "its inputs and code version match the manifest.");
    Assert.That(plan.UncacheableStepLabels, Is.Empty,
      "Observation-only deps must not push the step into the uncacheable bucket.");
  }

  [Test]
  public async Task ObservationOnlyPlusRegularDep_StillUncacheable_CountExcludesObservation()
  {
    // The carve-out is for observation-only refs specifically; a step
    // that also declares a regular service dep is still uncacheable.
    // The reason's Count surfaces only the cache-affecting deps so
    // the developer-facing message stays meaningful.
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep(
      label: "transform", codeVersion: "code-v1",
      input: input, output: output,
      serviceDependencies: new ServiceRef[]
      {
        new ServiceRef.ObservationOnly(typeof(Microsoft.Extensions.Logging.ILogger)),
        ServiceRef.Of<object>(),
      });

    var plan = await CachePlanBuilder.BuildAsync(BuildFlow(step), CacheManifest.Empty);

    Assert.That(plan.UncacheableStepLabels, Is.EquivalentTo(new[] { "transform" }));
    var reason = plan.UncacheableReasons["transform"];
    Assert.That(reason, Is.TypeOf<StepUncacheableReason.HasServiceDependencies>());
    Assert.That(((StepUncacheableReason.HasServiceDependencies)reason).Count, Is.EqualTo(1),
      "Count surfaces cache-affecting deps only — the ObservationOnly logger is excluded.");
  }

  [Test]
  public async Task ObservationOnlyParent_DoesNotCascadeUncacheabilityToChildren()
  {
    // The original motivation for ADR-0010 was the cascade: an
    // ILogger-declaring parent step uncacheabilised every downstream
    // consumer too. With the carve-out the parent is cache-eligible,
    // so the cascade rule has nothing to propagate.
    var seedInput = new FakeFingerprintItem<int>("seed", fingerprint: "fp-seed", exists: true);
    var mid = new FakeFingerprintItem<int>("mid", fingerprint: "fp-mid", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var stepA = MakeStep(
      label: "A", codeVersion: "code-A-v1",
      input: seedInput, output: mid,
      serviceDependencies: new ServiceRef[]
      {
        new ServiceRef.ObservationOnly(typeof(Microsoft.Extensions.Logging.ILogger)),
      });
    var stepB = MakeStep("B", "code-B-v1", mid, output);

    var compositeA = CachePlanBuilder.ComposeStepFingerprint(
      "code-A-v1", new[] { ("seed", "fp-seed") });
    var compositeB = CachePlanBuilder.ComposeStepFingerprint(
      "code-B-v1", new[] { ("mid", "fp-mid") });
    var manifest = Manifest(
      steps: new[] { ("A", compositeA), ("B", compositeB) },
      items: new[] { ("seed", "fp-seed"), ("mid", "fp-mid") });

    var plan = await CachePlanBuilder.BuildAsync(BuildFlow(stepA, stepB), manifest);

    Assert.That(plan.UncacheableStepLabels, Is.Empty,
      "Observation-only deps on the parent must not cascade into the child.");
    Assert.That(plan.FreshStepLabels, Is.EquivalentTo(new[] { "A", "B" }),
      "Both steps eligible and matching the manifest → both fresh.");
  }

  [Test]
  public async Task UncacheableReason_UnfingerprintableInput_NamesItemLabel()
  {
    // Mirrors MagicAtlas's exact debugging pain: a `.Memory()` adapter
    // upstream (no fingerprint capability) silently cascaded to every
    // consumer. The reason must point at the offending item by label so
    // the developer doesn't have to bisect by removing nodes one at a time.
    var input = new FakeFingerprintItem<int>("memory_input", fingerprint: null, exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep("transform", "code-v1", input, output);

    var plan = await CachePlanBuilder.BuildAsync(BuildFlow(step), CacheManifest.Empty);

    var reason = plan.UncacheableReasons["transform"];
    Assert.That(reason, Is.TypeOf<StepUncacheableReason.UnfingerprintableInput>());
    Assert.That(((StepUncacheableReason.UnfingerprintableInput)reason).ItemLabel,
      Is.EqualTo("memory_input"));
  }

  [Test]
  public async Task UncacheableReason_Cascade_NamesParentStepLabel()
  {
    // A cascade case names the immediate parent step so developers can
    // walk the chain backward in one hop instead of bisecting.
    var seedInput = new FakeFingerprintItem<int>("seed", fingerprint: "fp-seed", exists: true);
    var mid = new FakeFingerprintItem<int>("mid", fingerprint: "fp-mid", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var stepA = MakeStep("A", codeVersion: null, seedInput, mid);
    var stepB = MakeStep("B", "code-B-v1", mid, output);

    var plan = await CachePlanBuilder.BuildAsync(BuildFlow(stepA, stepB), CacheManifest.Empty);

    Assert.That(plan.UncacheableReasons["A"], Is.TypeOf<StepUncacheableReason.NoCodeVersion>(),
      "Root cause keeps its specific reason (NoCodeVersion), not Cascade.");
    var bReason = plan.UncacheableReasons["B"];
    Assert.That(bReason, Is.TypeOf<StepUncacheableReason.CascadeFromStep>());
    Assert.That(((StepUncacheableReason.CascadeFromStep)bReason).ParentStepLabel,
      Is.EqualTo("A"),
      "Cascaded child should name its immediate uncacheable parent so the trail "
      + "from a leaf back to the root cause is one hop per step.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers
  // ─────────────────────────────────────────────────────────────────────────

  private static Step<int, int> MakeStep(
    string label,
    string? codeVersion,
    IItem<int> input,
    IItem<int> output,
    IReadOnlyList<ServiceRef>? serviceDependencies = null
  ) => new Step<int, int>(
    label: label,
    transform: x => FlowIO.Pure(x),
    inputs: new IItem[] { input },
    outputs: new IItem[] { output },
    loadInputs: () => FlowIO.Pure(0),
    saveOutputs: _ => FlowIO.Pure(FlowUnit.Default),
    codeVersion: codeVersion,
    serviceDependencies: serviceDependencies
  );

  private static BuiltFlow BuildFlow(params IStepNode[] steps) =>
    FlowBuilder.CreateFlow("test", b =>
    {
      foreach (var step in steps) b.Add(step);
    });

  private static CacheManifest Manifest(
    (string Label, string Value)[]? steps = null,
    (string Label, string Value)[]? items = null
  )
  {
    var stepDict = new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal);
    foreach (var (label, value) in steps ?? Array.Empty<(string, string)>())
    {
      stepDict[label] = new NodeFingerprint(value, T);
    }
    var itemDict = new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal);
    foreach (var (label, value) in items ?? Array.Empty<(string, string)>())
    {
      itemDict[label] = new NodeFingerprint(value, T);
    }
    return new CacheManifest(CacheManifestSchema.CurrentVersion, stepDict, itemDict);
  }

  /// <summary>
  /// Test stub: an <see cref="IItem{T}"/> that exposes a configurable
  /// <see cref="TryGetFingerprint"/> result (null → unfingerprintable)
  /// and a configurable <see cref="Exists"/> verdict. Load and Save
  /// fail because the cache-plan walk should never invoke them.
  /// </summary>
  private sealed class FakeFingerprintItem<T> : IItem<T>
  {
    private readonly string? _fingerprint;
    private readonly bool _exists;

    public FakeFingerprintItem(string label, string? fingerprint, bool exists)
    {
      Label = label;
      _fingerprint = fingerprint;
      _exists = exists;
    }

    public string Label { get; }
    public NodeTraits Traits => new();
    public Type DataType => typeof(T);

    public FlowIO<T> Load() => FlowIO.Fail<T>(
      new RuntimeError.External("fake-load", new NotSupportedException()));
    public FlowIO<FlowUnit> Save(T data) => FlowIO.Pure(FlowUnit.Default);
    public FlowIO<bool> Exists() => FlowIO.Pure(_exists);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
      FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());

    public FlowIO<object> LoadUntyped() => Load().Map(v => (object)v!);
    public FlowIO<FlowUnit> SaveUntyped(object data) => Save((T)data);
    public FlowIO<ValidationResult> Validate() => InspectShallow();

    public FlowIO<string>? TryGetFingerprint() =>
      _fingerprint is null ? null : FlowIO.Pure(_fingerprint);
  }
}
