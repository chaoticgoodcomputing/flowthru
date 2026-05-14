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
    Assert.That(plan.NewFingerprints, Is.Empty);
  }

  [Test]
  public async Task SingleStep_MatchingManifestAndOutputsExist_IsFresh()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in-1", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out-1", exists: true);
    var step = MakeStep("transform", "code-v1", input, output);
    var flow = BuildFlow(step);

    // Expected composite for the single-input flow.
    var composite = CachePlanBuilder.ComposeStepFingerprint(
      "code-v1", new[] { ("in", "fp-in-1") });

    var manifest = new CacheManifest(
      CacheManifestSchema.CurrentVersion,
      new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal)
      {
        ["transform"] = new NodeFingerprint(composite, T),
      });

    var plan = await CachePlanBuilder.BuildAsync(flow, manifest);

    Assert.That(plan.FreshStepLabels, Is.EquivalentTo(new[] { "transform" }));
    Assert.That(plan.StaleStepLabels, Is.Empty);
    Assert.That(plan.UncacheableStepLabels, Is.Empty);
    Assert.That(plan.NewFingerprints["transform"], Is.EqualTo(composite));
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
    var manifest = ManifestWith(("transform", composite));

    var plan = await CachePlanBuilder.BuildAsync(flow, manifest);

    Assert.That(plan.StaleStepLabels, Is.EquivalentTo(new[] { "transform" }),
      "Output missing on disk forces stale even when the composite hash matches — "
      + "we can't reuse data that isn't there.");
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
  public async Task SingleStep_NonMatchingManifestEntry_IsStale()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep("transform", "code-v1", input, output);
    var flow = BuildFlow(step);

    // Different composite was recorded — could be a different CodeVersion,
    // a different input fingerprint, or a schema bump that filtered through.
    var manifest = ManifestWith(("transform", "stale-composite"));

    var plan = await CachePlanBuilder.BuildAsync(flow, manifest);

    Assert.That(plan.StaleStepLabels, Is.EquivalentTo(new[] { "transform" }));
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
    // A produces M, B consumes M and produces O.
    // A is stale (no manifest entry); B inherits stale via M.
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

    // A is uncacheable (null CodeVersion). B should also be uncacheable —
    // not just stale — because A's outputs have no reliable identity.
    var stepA = MakeStep("A", codeVersion: null, seedInput, mid);
    var stepB = MakeStep("B", "code-B-v1", mid, output);
    var flow = BuildFlow(stepA, stepB);

    var plan = await CachePlanBuilder.BuildAsync(flow, CacheManifest.Empty);

    Assert.That(plan.UncacheableStepLabels, Is.EquivalentTo(new[] { "A", "B" }),
      "Uncacheable parents cascade as uncacheable — B can never be safely cached because "
      + "we cannot identify A's outputs.");
  }

  [Test]
  public async Task TwoStepChain_BothFresh_DownstreamCompositeIncludesUpstream()
  {
    var seedInput = new FakeFingerprintItem<int>("seed", fingerprint: "fp-seed", exists: true);
    var mid = new FakeFingerprintItem<int>("mid", fingerprint: "fp-mid", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);

    var stepA = MakeStep("A", "code-A-v1", seedInput, mid);
    var stepB = MakeStep("B", "code-B-v1", mid, output);
    var flow = BuildFlow(stepA, stepB);

    // Compute the expected composites.
    var compositeA = CachePlanBuilder.ComposeStepFingerprint(
      "code-A-v1", new[] { ("seed", "fp-seed") });
    var compositeB = CachePlanBuilder.ComposeStepFingerprint(
      "code-B-v1", new[] { ("mid", compositeA) });

    var manifest = ManifestWith(("A", compositeA), ("B", compositeB));

    var plan = await CachePlanBuilder.BuildAsync(flow, manifest);

    Assert.That(plan.FreshStepLabels, Is.EquivalentTo(new[] { "A", "B" }));
    Assert.That(plan.NewFingerprints["A"], Is.EqualTo(compositeA));
    Assert.That(plan.NewFingerprints["B"], Is.EqualTo(compositeB),
      "B's composite must fold in A's composite (not A's input fingerprint) — that's "
      + "the rollup that makes intermediate-item identity meaningful.");
  }

  [Test]
  public async Task SchemaMismatchedManifest_TreatsEveryEntryAsAbsent()
  {
    var input = new FakeFingerprintItem<int>("in", fingerprint: "fp-in", exists: true);
    var output = new FakeFingerprintItem<int>("out", fingerprint: "fp-out", exists: true);
    var step = MakeStep("transform", "code-v1", input, output);
    var flow = BuildFlow(step);

    // The manifest's saved value matches what we'd compute now…
    var composite = CachePlanBuilder.ComposeStepFingerprint(
      "code-v1", new[] { ("in", "fp-in") });
    var staleSchemaManifest = new CacheManifest(
      CacheManifestSchema.CurrentVersion - 1,
      new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal)
      {
        ["transform"] = new NodeFingerprint(composite, T),
      });

    var plan = await CachePlanBuilder.BuildAsync(flow, staleSchemaManifest);

    Assert.That(plan.StaleStepLabels, Is.EquivalentTo(new[] { "transform" }),
      "…but the schema-version mismatch invalidates every entry — first run after a "
      + "framework bump re-records every cacheable step.");
  }

  [Test]
  public async Task IndependentBranches_DoNotCascade()
  {
    // Two unrelated steps: A reads its own input + produces its own output;
    // B reads its own input + produces its own output. Staleness on A
    // must NOT bleed into B.
    var inA = new FakeFingerprintItem<int>("in-a", fingerprint: "fp-a", exists: true);
    var outA = new FakeFingerprintItem<int>("out-a", fingerprint: "fp-out-a", exists: true);
    var inB = new FakeFingerprintItem<int>("in-b", fingerprint: "fp-b", exists: true);
    var outB = new FakeFingerprintItem<int>("out-b", fingerprint: "fp-out-b", exists: true);

    var stepA = MakeStep("A", "code-A-v1", inA, outA);
    var stepB = MakeStep("B", "code-B-v1", inB, outB);
    var flow = BuildFlow(stepA, stepB);

    // Manifest has B's correct composite but no A entry — only A should be stale.
    var compositeB = CachePlanBuilder.ComposeStepFingerprint(
      "code-B-v1", new[] { ("in-b", "fp-b") });
    var manifest = ManifestWith(("B", compositeB));

    var plan = await CachePlanBuilder.BuildAsync(flow, manifest);

    Assert.That(plan.FreshStepLabels, Is.EquivalentTo(new[] { "B" }));
    Assert.That(plan.StaleStepLabels, Is.EquivalentTo(new[] { "A" }));
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

  private static CacheManifest ManifestWith(params (string Label, string Value)[] entries)
  {
    var dict = new Dictionary<string, NodeFingerprint>(StringComparer.Ordinal);
    foreach (var (label, value) in entries)
    {
      dict[label] = new NodeFingerprint(value, T);
    }
    return new CacheManifest(CacheManifestSchema.CurrentVersion, dict);
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
