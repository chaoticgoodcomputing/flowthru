using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Mermaid;
using Flowthru.Flow;
using Flowthru.Prelude;
using SysIO = System.IO;

namespace Flowthru.Extensions.Metadata.Mermaid.Tests;

/// <summary>
/// End-to-end exercises for <see cref="MermaidMetadataProvider.Emit"/> —
/// validates that the pre-run DAG diagram and the post-run result
/// diagram are written and contain the expected Mermaid syntax for
/// a real <see cref="BuiltFlow"/>.
/// </summary>
[TestFixture]
[Category("Metadata.Mermaid")]
public class MermaidMetadataProviderEmitTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-mermaid-{Guid.NewGuid():N}"
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
  /// A two-step flow with one external input + one chained intermediate
  /// produces a non-trivial diagram: external item, two steps, two
  /// produced items, four edges.
  /// </summary>
  private static (BuiltFlow Flow, IItem<int> Raw) BuildSampleFlow(string label = "sample-flow")
  {
    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var stage1 = ItemFactory.Singleton.Memory<int>("stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("stage2");

    var flow = FlowBuilder.CreateFlow(label, b =>
    {
      b.AddStep<int, int>("scale",   x => x * 2,  raw,    stage1);
      b.AddStep<int, int>("offset",  x => x + 10, stage1, stage2);
    });
    return (flow, raw);
  }

  // ── Pre-run emission ─────────────────────────────────────────────────

  [Test]
  public async Task EmitDag_WritesMermaidMarkdown()
  {
    var provider = new MermaidMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithFilenameTemplate("dag-{FlowName}")
      .Build();
    var (flow, _) = BuildSampleFlow();

    var emit = await ((IMetadataProvider)provider).Emit(FlowMetadataContext.Unsliced(flow)).Run();
    Assert.That(emit, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var written = SysIO.Directory.GetFiles(_root, "*.md").Single();
    Assert.That(SysIO.Path.GetFileName(written), Is.EqualTo("dag-sample-flow.md"));

    var content = SysIO.File.ReadAllText(written);
    Assert.That(content, Does.StartWith("```mermaid"),
      "File must open with the Mermaid code fence.");
    Assert.That(content, Does.Contain("flowchart TB"),
      "Default direction is top-to-bottom (TB).");
    Assert.That(content, Does.Contain("subgraph sample_flow"),
      "Flow label should drive the subgraph id (with non-id chars sanitized).");
    Assert.That(content, Does.Contain("scale"),
      "Both step labels should appear in the diagram body.");
    Assert.That(content, Does.Contain("offset"));
    Assert.That(content, Does.Contain("stage1 --> offset"),
      "Internal edges should connect produced items to their consumers.");
    Assert.That(content, Does.Contain("raw --> scale"),
      "External-to-step edges should appear below the subgraph.");
  }

  [Test]
  public async Task EmitDag_DirectionLeftToRight_RendersLR()
  {
    var provider = new MermaidMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithFilenameTemplate("dag-{FlowName}")
      .WithDirection(MermaidFlowchartDirection.LeftToRight)
      .Build();

    var (flow, _) = BuildSampleFlow();
    await ((IMetadataProvider)provider).Emit(FlowMetadataContext.Unsliced(flow)).Run();

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "*.md").Single());
    Assert.That(content, Does.Contain("flowchart LR"));
  }

  [Test]
  public async Task EmitDag_OutputDirectoryIsCreatedIfMissing()
  {
    var nested = SysIO.Path.Combine(_root, "nested", "deeper");
    Assert.That(SysIO.Directory.Exists(nested), Is.False, "Precondition: nested dir absent.");

    var provider = new MermaidMetadataProviderBuilder()
      .WithOutputDirectory(nested)
      .Build();

    var (flow, _) = BuildSampleFlow();
    await ((IMetadataProvider)provider).Emit(FlowMetadataContext.Unsliced(flow)).Run();

    Assert.That(SysIO.Directory.Exists(nested), Is.True);
  }

  // ── Post-run emission ────────────────────────────────────────────────

  [Test]
  public async Task EmitRun_ColorsSucceededStepsWithActiveColor()
  {
    var provider = new MermaidMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithRunFilenameTemplate("run-{FlowName}")
      .WithActiveStepColor("#2E7D32")
      .Build();

    var (flow, raw) = BuildSampleFlow();
    await raw.Save(7).Run(); // seed the external input

    var result = await flow.RunAsync();

    var emit = await ((IPostRunMetadataProvider)provider).Emit(new FlowRunMetadataContext { Static = FlowMetadataContext.Unsliced(flow), Result = result }).Run();
    Assert.That(emit, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "run-*.md").Single());
    Assert.That(content, Does.Contain("style scale fill:#2E7D32"),
      "Successful step nodes get the active-step fill colour.");
    Assert.That(content, Does.Contain("style offset fill:#2E7D32"));
  }

  [Test]
  public async Task EmitRun_ColorsFailedStepWithFailedColor()
  {
    var provider = new MermaidMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithRunFilenameTemplate("run-{FlowName}")
      .WithFailedStepColor("#C62828")
      .Build();

    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var stage1 = ItemFactory.Singleton.Memory<int>("stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("stage2");
    await raw.Save(0).Run(); // 100/0 → triggers failure in step 'explode'

    var flow = FlowBuilder.CreateFlow("fail-flow", b =>
    {
      b.AddStep<int, int>("explode",   x => 100 / x,   raw,    stage1);
      b.AddStep<int, int>("downstream", x => x + 1,    stage1, stage2);
    });
    var result = await flow.RunAsync();

    Assert.That(result.IsSuccess, Is.False, "Precondition: the run should have failed.");

    await ((IPostRunMetadataProvider)provider).Emit(new FlowRunMetadataContext { Static = FlowMetadataContext.Unsliced(flow), Result = result }).Run();

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "run-*.md").Single());
    Assert.That(content, Does.Contain("style explode fill:#C62828"),
      "Failed step should be coloured with the failed-step colour.");
  }

  [Test]
  public async Task EmitRun_SkippedStepGetsSkippedColor()
  {
    // After 'explode' fails, 'downstream' is skipped under the default
    // StopOnFirstError policy. The skipped step should render with the
    // skipped-step colour.
    var provider = new MermaidMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithRunFilenameTemplate("run-{FlowName}")
      .WithSkippedStepColor("#757575")
      .Build();

    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var stage1 = ItemFactory.Singleton.Memory<int>("stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("stage2");
    await raw.Save(0).Run();

    var flow = FlowBuilder.CreateFlow("skip-flow", b =>
    {
      b.AddStep<int, int>("explode",    x => 100 / x, raw,    stage1);
      b.AddStep<int, int>("downstream", x => x + 1,   stage1, stage2);
    });
    var result = await flow.RunAsync();

    await ((IPostRunMetadataProvider)provider).Emit(new FlowRunMetadataContext { Static = FlowMetadataContext.Unsliced(flow), Result = result }).Run();

    var content = SysIO.File.ReadAllText(SysIO.Directory.GetFiles(_root, "run-*.md").Single());
    Assert.That(content, Does.Contain("style downstream fill:#757575"),
      "A step that was skipped because an upstream step failed should be coloured "
      + "with the skipped-step colour.");
  }

  // ── Provider identity ────────────────────────────────────────────────

  [Test]
  public void ProviderId_IsStable()
  {
    var provider = new MermaidMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .Build();

    Assert.That(((IMetadataProvider)provider).ProviderId, Is.EqualTo("Flowthru.Mermaid"));
  }
}
