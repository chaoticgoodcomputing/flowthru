using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Flow;
using SysIO = System.IO;

namespace Flowthru.Extensions.Metadata.Mermaid.Tests;

/// <summary>
/// Slice-context coverage for <see cref="MermaidMetadataProvider"/>.
/// Verifies <c>WithShowFullDag</c> behaviour: <c>true</c> renders the
/// merged DAG with inactive nodes muted; <c>false</c> filters inactive
/// nodes out entirely.
/// </summary>
[TestFixture]
[Category("Metadata.Mermaid")]
public class MermaidMetadataProviderSliceTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-mermaid-slice-{Guid.NewGuid():N}"
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
  /// Build a 4-step linear chain. The merged DAG is the full chain;
  /// the slice is the last step only ("stage4-step"), with the
  /// upstream three steps inactive in <c>ActiveStepLabels</c>.
  /// </summary>
  private static FlowMetadataContext BuildSliceContext()
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

    return new FlowMetadataContext
    {
      MergedFlow = merged,
      EffectiveFlow = slice,
      ActiveStepLabels = new HashSet<string>(new[] { "stage4-step" }, StringComparer.Ordinal),
      RequestedFlowLabel = "Stage4",
    };
  }

  // ── ShowFullDag = true (default): merged with inactive muted ─────────

  [Test]
  public async Task EmitDag_ShowFullDagDefaultsTrue_RendersMergedDagWithMutedInactive()
  {
    var provider = new MermaidMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithFilenameTemplate("dag-{FlowName}")
      .Build();

    await ((IMetadataProvider)provider).Emit(BuildSliceContext()).Run();

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "*.md").Single());
    Assert.That(content, Does.Contain("stage1-step"),
      "showFullDag default keeps inactive steps in the diagram.");
    Assert.That(content, Does.Contain("stage2_step"),
      "Subgraph labels sanitise dashes to underscores; node ids do too.");
    Assert.That(content, Does.Contain("stage4-step"));

    Assert.That(content, Does.Contain("style stage1_step fill:#E0E0E0"),
      "Inactive steps render with the muted inactive-step colour.");
    Assert.That(content, Does.Contain("style stage2_step fill:#E0E0E0"));
    Assert.That(content, Does.Contain("style stage3_step fill:#E0E0E0"));
    Assert.That(content, Does.Not.Contain("style stage4_step fill:#E0E0E0"),
      "Active steps don't get the muted style.");
  }

  [Test]
  public async Task EmitDag_ShowFullDagTrue_EdgesIntoInactiveStepsAreDashed()
  {
    var provider = new MermaidMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithShowFullDag(true)
      .Build();

    await ((IMetadataProvider)provider).Emit(BuildSliceContext()).Run();

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "*.md").Single());

    Assert.That(content, Does.Contain("raw -.-> stage1_step"),
      "Edges incident to inactive steps render with the dashed (-.->) Mermaid arrow.");
    Assert.That(content, Does.Contain("stage3 --> stage4_step"),
      "Edges into active steps stay solid.");
  }

  // ── ShowFullDag = false: filter inactive entirely ────────────────────

  [Test]
  public async Task EmitDag_ShowFullDagFalse_FiltersInactiveNodes()
  {
    var provider = new MermaidMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithShowFullDag(false)
      .Build();

    await ((IMetadataProvider)provider).Emit(BuildSliceContext()).Run();

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "*.md").Single());

    Assert.That(content, Does.Contain("stage4_step"),
      "The active step renders.");
    Assert.That(content, Does.Not.Contain("stage1_step"),
      "Inactive steps are filtered out entirely when showFullDag=false.");
    Assert.That(content, Does.Not.Contain("stage2_step"));
    Assert.That(content, Does.Not.Contain("stage3_step"));

    Assert.That(content, Does.Contain("subgraph Stage4"),
      "The active flow's subgraph still wraps the slice.");
  }

  // ── Subgraph partitioning by FlowLabel ───────────────────────────────

  [Test]
  public async Task EmitDag_PartitionsStepsBySubgraphPerFlowOfOrigin()
  {
    // Build a fresh context where every step is active so the subgraph
    // partitioning is the only behaviour under test.
    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var stage1 = ItemFactory.Singleton.Memory<int>("stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("stage2");
    var flow = FlowBuilder.CreateFlow("MyFlow", b =>
    {
      b.AddStep<int, int>("step-a", x => x + 1, raw, stage1);
      b.AddStep<int, int>("step-b", x => x + 10, stage1, stage2);
    });

    var provider = new MermaidMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .Build();

    await ((IMetadataProvider)provider).Emit(FlowMetadataContext.Unsliced(flow)).Run();

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "*.md").Single());
    Assert.That(content, Does.Contain("subgraph MyFlow"),
      "Single registered flow → single subgraph keyed by FlowBuilder label.");
    Assert.That(content, Does.Contain("stage1 --> step_b"),
      "Internal flow edges render solidly when the consumer step is active.");
  }
}
