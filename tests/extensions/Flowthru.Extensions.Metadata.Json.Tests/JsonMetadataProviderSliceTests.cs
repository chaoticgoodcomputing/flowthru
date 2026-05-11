using System.Text.Json;
using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Flow;
using SysIO = System.IO;

namespace Flowthru.Extensions.Metadata.Json.Tests;

/// <summary>
/// Slice-context coverage for <see cref="JsonMetadataProvider"/>.
/// The pre-run JSON manifest is the canonical machine-readable
/// surface — third-party tooling depends on every fact a renderer
/// could need being present, so each property under test here is a
/// load-bearing contract.
/// </summary>
[TestFixture]
[Category("Metadata.Json")]
public class JsonMetadataProviderSliceTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-json-slice-{Guid.NewGuid():N}"
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

  /// <summary>
  /// Build a 4-step linear chain. The "merged" DAG is the full chain;
  /// the "effective" slice is the last step only, with the upstream
  /// steps left inactive in <c>ActiveStepLabels</c> so the slice-aware
  /// assertions have something to test.
  /// </summary>
  private static (BuiltFlow Merged, BuiltFlow Slice, IReadOnlySet<string> ActiveLabels) BuildSliceFixture()
  {
    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var stage1 = ItemFactory.Singleton.Memory<int>("stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("stage2");
    var stage3 = ItemFactory.Singleton.Memory<int>("stage3");
    var stage4 = ItemFactory.Singleton.Memory<int>("stage4");

    var merged = FlowBuilder.CreateFlow("__merged__", b =>
    {
      b.AddStep<int, int>("stage1-step", x => x + 1, raw, stage1);
      b.AddStep<int, int>("stage2-step", x => x + 10, stage1, stage2);
      b.AddStep<int, int>("stage3-step", x => x + 100, stage2, stage3);
      b.AddStep<int, int>("stage4-step", x => x * 2, stage3, stage4);
    });

    var slice = FlowBuilder.CreateFlow("Stage4", b =>
      b.AddStep<int, int>("stage4-step", x => x * 2, stage3, stage4));

    var active = new HashSet<string>(new[] { "stage4-step" }, StringComparer.Ordinal);
    return (merged, slice, active);
  }

  // ── Pre-run, no slice ────────────────────────────────────────────────

  [Test]
  public async Task EmitDag_NoSlice_AllStepsActive()
  {
    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithFilenameTemplate("dag-{FlowName}")
      .Build();

    var (merged, _, _) = BuildSliceFixture();
    await ((IMetadataProvider)provider).Emit(FlowMetadataContext.Unsliced(merged)).Run();

    var written = SysIO.Directory.GetFiles(_root, "*.json").Single();
    using var doc = JsonDocument.Parse(SysIO.File.ReadAllText(written));
    var root = doc.RootElement;

    Assert.That(root.GetProperty("flowName").GetString(), Is.EqualTo("__merged__"));
    Assert.That(root.TryGetProperty("requestedFlowLabel", out _), Is.False,
      "Null RequestedFlowLabel should be omitted by the JSON serialiser.");
    Assert.That(root.GetProperty("steps").GetArrayLength(), Is.EqualTo(4),
      "Merged DAG carries every step.");
    foreach (var step in root.GetProperty("steps").EnumerateArray())
    {
      Assert.That(step.GetProperty("active").GetBoolean(), Is.True,
        $"Without slicing, every step ({step.GetProperty("label").GetString()}) is active.");
    }
    Assert.That(root.GetProperty("activeStepLabels").GetArrayLength(), Is.EqualTo(4));
  }

  // ── Pre-run, narrow slice ────────────────────────────────────────────

  [Test]
  public async Task EmitDag_Sliced_StepsCarryActiveFlag()
  {
    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithFilenameTemplate("dag-{FlowName}")
      .Build();

    var (merged, slice, active) = BuildSliceFixture();
    var ctx = new FlowMetadataContext
    {
      MergedFlow = merged,
      EffectiveFlow = slice,
      ActiveStepLabels = active,
      RequestedFlowLabel = "Stage4",
    };

    await ((IMetadataProvider)provider).Emit(ctx).Run();

    var written = SysIO.Directory.GetFiles(_root, "*.json").Single();
    Assert.That(SysIO.Path.GetFileName(written), Is.EqualTo("dag-Stage4.json"),
      "Filename derives from EffectiveFlow.Label (the slice), not the merged DAG label.");

    using var doc = JsonDocument.Parse(SysIO.File.ReadAllText(written));
    var root = doc.RootElement;

    Assert.That(root.GetProperty("flowName").GetString(), Is.EqualTo("Stage4"));
    Assert.That(root.GetProperty("requestedFlowLabel").GetString(), Is.EqualTo("Stage4"));

    Assert.That(root.GetProperty("steps").GetArrayLength(), Is.EqualTo(4),
      "Merged DAG topology is preserved — third-party tooling can render the full graph.");

    var activeByLabel = root.GetProperty("steps").EnumerateArray()
      .ToDictionary(
        s => s.GetProperty("label").GetString()!,
        s => s.GetProperty("active").GetBoolean()
      );

    Assert.That(activeByLabel["stage4-step"], Is.True, "The slice target is active.");
    Assert.That(activeByLabel["stage1-step"], Is.False,
      "Inactive ancestors are present in the projection but flagged inactive.");
    Assert.That(activeByLabel["stage2-step"], Is.False);
    Assert.That(activeByLabel["stage3-step"], Is.False);

    var activeLabels = root.GetProperty("activeStepLabels").EnumerateArray()
      .Select(e => e.GetString()).ToList();
    Assert.That(activeLabels, Is.EqualTo(new[] { "stage4-step" }));
  }

  // ── Per-step flow-of-origin attribution ──────────────────────────────

  [Test]
  public async Task EmitDag_StepsCarryFlowOfOrigin()
  {
    // Flow-of-origin is whatever flow label was passed to FlowBuilder.CreateFlow
    // for each step; here every step lives in "__merged__" because we built a
    // single merged flow. The integration with multi-flow merging lives in
    // FlowthruService and is exercised by the SpaceflightsEFCore smoke run.
    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .Build();

    var (merged, _, _) = BuildSliceFixture();
    await ((IMetadataProvider)provider).Emit(FlowMetadataContext.Unsliced(merged)).Run();

    var written = SysIO.Directory.GetFiles(_root, "*.json").Single();
    using var doc = JsonDocument.Parse(SysIO.File.ReadAllText(written));

    foreach (var step in doc.RootElement.GetProperty("steps").EnumerateArray())
    {
      Assert.That(step.GetProperty("flowOfOrigin").GetString(), Is.EqualTo("__merged__"),
        "Each step should carry its declaring FlowBuilder's label as flowOfOrigin.");
    }
  }
}
