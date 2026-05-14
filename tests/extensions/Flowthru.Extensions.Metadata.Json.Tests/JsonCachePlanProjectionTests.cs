using System.Text.Json;
using Flowthru.Caching;
using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using SysIO = System.IO;

namespace Flowthru.Extensions.Metadata.Json.Tests;

/// <summary>
/// Coverage for the cache-plan surface in the JSON metadata projection.
/// The pre-run DAG carries a top-level <c>cachePlan</c> object (mode,
/// fresh/stale/uncacheable arrays) and per-step <c>cache</c> objects
/// (status). Post-run step results extend the per-step <c>cache</c>
/// object with the <c>ran</c> flag and optional reason string.
/// </summary>
[TestFixture]
[Category("Metadata.Json")]
[Category("CachePlan")]
public class JsonCachePlanProjectionTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-json-cache-{Guid.NewGuid():N}"
    );
    SysIO.Directory.CreateDirectory(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_root))
    {
      try { SysIO.Directory.Delete(_root, recursive: true); }
      catch { /* best effort */ }
    }
  }

  // ── Pre-run ─────────────────────────────────────────────────────────

  [Test]
  public async Task EmitDag_WithCachePlan_EmitsPlannedModeAndStepStatus()
  {
    var flow = BuildTwoStepFlow();
    var plan = new CachePlan(
      FreshStepLabels: new HashSet<string>(new[] { "alpha" }, StringComparer.Ordinal),
      StaleStepLabels: new HashSet<string>(new[] { "beta" }, StringComparer.Ordinal),
      UncacheableStepLabels: new HashSet<string>(StringComparer.Ordinal),
      NewStepFingerprints: new Dictionary<string, string>(StringComparer.Ordinal),
      NewItemFingerprints: new Dictionary<string, string>(StringComparer.Ordinal)
    );
    var ctx = new FlowMetadataContext
    {
      MergedFlow = flow,
      EffectiveFlow = flow,
      ActiveStepLabels = flow.Steps.Select(s => s.Label).ToHashSet(StringComparer.Ordinal),
      RequestedFlowLabel = null,
      CachePlan = plan,
    };
    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithFilenameTemplate("dag-{FlowName}")
      .Build();

    await ((IMetadataProvider)provider).Emit(ctx).Run();

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "dag-*.json").Single());
    using var doc = JsonDocument.Parse(content);
    var root = doc.RootElement;

    var cachePlan = root.GetProperty("cachePlan");
    Assert.That(cachePlan.GetProperty("mode").GetString(), Is.EqualTo("planned"));
    Assert.That(cachePlan.GetProperty("fresh").EnumerateArray().Select(e => e.GetString()).ToList(),
      Is.EqualTo(new[] { "alpha" }));
    Assert.That(cachePlan.GetProperty("stale").EnumerateArray().Select(e => e.GetString()).ToList(),
      Is.EqualTo(new[] { "beta" }));
    Assert.That(cachePlan.GetProperty("uncacheable").EnumerateArray().Count(), Is.EqualTo(0));

    // Per-step cache classification mirrors the top-level plan.
    var steps = root.GetProperty("steps").EnumerateArray()
      .ToDictionary(s => s.GetProperty("label").GetString()!, s => s);
    Assert.That(steps["alpha"].GetProperty("cache").GetProperty("status").GetString(),
      Is.EqualTo("fresh"));
    Assert.That(steps["beta"].GetProperty("cache").GetProperty("status").GetString(),
      Is.EqualTo("stale"));
    // Pre-flight: `ran` is null (the field exists but JSON ignores nulls,
    // so its absence here means "not yet run").
    Assert.That(steps["alpha"].GetProperty("cache").TryGetProperty("ran", out _), Is.False,
      "Pre-flight cache.ran is null and should be omitted by the WhenWritingNull policy.");
  }

  [Test]
  public async Task EmitDag_NoCachePlan_EmitsDisabledMode()
  {
    var flow = BuildTwoStepFlow();
    var ctx = FlowMetadataContext.Unsliced(flow); // no CachePlan, no Bypass
    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithFilenameTemplate("dag-{FlowName}")
      .Build();

    await ((IMetadataProvider)provider).Emit(ctx).Run();

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "dag-*.json").Single());
    using var doc = JsonDocument.Parse(content);
    var root = doc.RootElement;

    Assert.That(root.GetProperty("cachePlan").GetProperty("mode").GetString(),
      Is.EqualTo("disabled"));
    var steps = root.GetProperty("steps").EnumerateArray()
      .ToDictionary(s => s.GetProperty("label").GetString()!, s => s);
    Assert.That(steps["alpha"].GetProperty("cache").GetProperty("status").GetString(),
      Is.EqualTo("unplanned"));
  }

  [Test]
  public async Task EmitDag_BypassedCacheReads_EmitsBypassedMode()
  {
    var flow = BuildTwoStepFlow();
    var ctx = new FlowMetadataContext
    {
      MergedFlow = flow,
      EffectiveFlow = flow,
      ActiveStepLabels = flow.Steps.Select(s => s.Label).ToHashSet(StringComparer.Ordinal),
      RequestedFlowLabel = null,
      BypassCacheReads = true,
    };
    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithFilenameTemplate("dag-{FlowName}")
      .Build();

    await ((IMetadataProvider)provider).Emit(ctx).Run();

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "dag-*.json").Single());
    using var doc = JsonDocument.Parse(content);
    Assert.That(doc.RootElement.GetProperty("cachePlan").GetProperty("mode").GetString(),
      Is.EqualTo("bypassed"));
  }

  // ── Post-run ────────────────────────────────────────────────────────

  [Test]
  public async Task EmitRun_CachedSucceeded_HasCacheReasonAndRanFalse()
  {
    var flow = BuildTwoStepFlow();
    var plan = new CachePlan(
      FreshStepLabels: new HashSet<string>(new[] { "alpha" }, StringComparer.Ordinal),
      StaleStepLabels: new HashSet<string>(StringComparer.Ordinal),
      UncacheableStepLabels: new HashSet<string>(StringComparer.Ordinal),
      NewStepFingerprints: new Dictionary<string, string>(StringComparer.Ordinal),
      NewItemFingerprints: new Dictionary<string, string>(StringComparer.Ordinal)
    );
    var ctx = new FlowMetadataContext
    {
      MergedFlow = flow,
      EffectiveFlow = flow,
      ActiveStepLabels = flow.Steps.Select(s => s.Label).ToHashSet(StringComparer.Ordinal),
      RequestedFlowLabel = null,
      CachePlan = plan,
    };
    var result = new FlowResult(new[]
    {
      (StepResult)new StepResult.Succeeded("alpha", TimeSpan.Zero) { Reason = "cached" },
      (StepResult)new StepResult.Succeeded("beta", TimeSpan.FromMilliseconds(10)),
    }, TimeSpan.FromMilliseconds(10));
    var runCtx = new FlowRunMetadataContext { Static = ctx, Result = result };

    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithRunFilenameTemplate("run-{FlowName}")
      .Build();
    await ((IPostRunMetadataProvider)provider).Emit(runCtx).Run();

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "run-*.json").Single());
    using var doc = JsonDocument.Parse(content);

    var stepResults = doc.RootElement
      .GetProperty("result")
      .GetProperty("stepResults")
      .EnumerateArray()
      .ToDictionary(s => s.GetProperty("stepLabel").GetString()!, s => s);

    var alphaCache = stepResults["alpha"].GetProperty("cache");
    Assert.That(alphaCache.GetProperty("status").GetString(), Is.EqualTo("fresh"));
    Assert.That(alphaCache.GetProperty("ran").GetBoolean(), Is.False,
      "A cache-hit step did not run.");
    Assert.That(alphaCache.GetProperty("reason").GetString(), Is.EqualTo("cached"));

    var betaCache = stepResults["beta"].GetProperty("cache");
    Assert.That(betaCache.GetProperty("status").GetString(), Is.EqualTo("unplanned"),
      "Step 'beta' is not in any plan bucket — classify as unplanned.");
    Assert.That(betaCache.GetProperty("ran").GetBoolean(), Is.True,
      "A non-cached succeeded step ran.");
  }

  [Test]
  public async Task EmitRun_FailedStep_HasRanTrue()
  {
    var flow = BuildTwoStepFlow();
    var ctx = FlowMetadataContext.Unsliced(flow);
    var result = new FlowResult(new[]
    {
      (StepResult)new StepResult.Failed(
        "alpha",
        new RuntimeError.External("test", new InvalidOperationException("nope")),
        TimeSpan.FromMilliseconds(50)
      ),
    }, TimeSpan.FromMilliseconds(50));
    var runCtx = new FlowRunMetadataContext { Static = ctx, Result = result };

    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithRunFilenameTemplate("run-{FlowName}")
      .Build();
    await ((IPostRunMetadataProvider)provider).Emit(runCtx).Run();

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "run-*.json").Single());
    using var doc = JsonDocument.Parse(content);

    var alphaCache = doc.RootElement
      .GetProperty("result")
      .GetProperty("stepResults")
      .EnumerateArray()
      .Single(s => s.GetProperty("stepLabel").GetString() == "alpha")
      .GetProperty("cache");

    Assert.That(alphaCache.GetProperty("status").GetString(), Is.EqualTo("unplanned"));
    Assert.That(alphaCache.GetProperty("ran").GetBoolean(), Is.True,
      "A failed step did run, even though it failed.");
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static BuiltFlow BuildTwoStepFlow()
  {
    var alphaOut = ItemFactory.Singleton.Memory<int>("alpha-out");
    var betaOut = ItemFactory.Singleton.Memory<int>("beta-out");
    return FlowBuilder.CreateFlow("cache-plan-flow", b =>
    {
      b.AddStep<int>("alpha", () => 1, alphaOut);
      b.AddStep<int>("beta", () => 2, betaOut);
    });
  }
}
