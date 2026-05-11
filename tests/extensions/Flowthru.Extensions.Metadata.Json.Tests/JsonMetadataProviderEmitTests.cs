using System.Text.Json;
using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Json;
using Flowthru.Flow;
using Flowthru.Prelude;
using SysIO = System.IO;

namespace Flowthru.Extensions.Metadata.Json.Tests;

/// <summary>
/// End-to-end exercises for <see cref="JsonMetadataProvider.Emit"/> —
/// validates that the pre-run DAG file and the post-run result file
/// are written atomically and contain the expected projection of a
/// real <see cref="BuiltFlow"/>.
/// </summary>
[TestFixture]
[Category("Metadata.Json")]
public class JsonMetadataProviderEmitTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-json-meta-{Guid.NewGuid():N}"
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
  /// Build a tiny BuiltFlow with a single step writing one in-memory
  /// item — the smallest non-trivial DAG that yields an interesting
  /// projection (one step, one item, one edge).
  /// </summary>
  private static BuiltFlow BuildSampleFlow(string label = "sample-flow")
  {
    var output = ItemFactory.Singleton.Memory<int>("computed");
    return FlowBuilder.CreateFlow(label, b =>
    {
      b.AddStep<int>("compute", () => 42, output);
    });
  }

  // ── Pre-run emission ─────────────────────────────────────────────────

  [Test]
  public async Task EmitDag_WritesFileWithExpectedProjection()
  {
    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithFilenameTemplate("dag-{FlowName}")
      .Build();
    var flow = BuildSampleFlow();

    var result = await ((IMetadataProvider)provider).Emit(FlowMetadataContext.Unsliced(flow)).Run();
    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var written = SysIO.Directory.GetFiles(_root, "*.json").Single();
    Assert.That(SysIO.Path.GetFileName(written), Is.EqualTo("dag-sample-flow.json"));

    using var document = JsonDocument.Parse(SysIO.File.ReadAllText(written));
    var root = document.RootElement;
    Assert.That(root.GetProperty("flowName").GetString(), Is.EqualTo("sample-flow"));
    Assert.That(root.GetProperty("steps").GetArrayLength(), Is.EqualTo(1));
    Assert.That(root.GetProperty("steps")[0].GetProperty("label").GetString(), Is.EqualTo("compute"));
    Assert.That(root.GetProperty("catalogItems").GetArrayLength(), Is.EqualTo(1));
    Assert.That(root.GetProperty("edges").GetArrayLength(), Is.EqualTo(1),
      "One step + one output ⇒ exactly one output edge.");
  }

  [Test]
  public async Task EmitDag_OutputDirectoryIsCreatedIfMissing()
  {
    var nested = SysIO.Path.Combine(_root, "nested", "deeper");
    Assert.That(SysIO.Directory.Exists(nested), Is.False, "Precondition: nested dir absent.");

    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(nested)
      .Build();

    await ((IMetadataProvider)provider).Emit(FlowMetadataContext.Unsliced(BuildSampleFlow())).Run();

    Assert.That(SysIO.Directory.Exists(nested), Is.True,
      "Provider should create the output directory on first emit.");
  }

  [Test]
  public async Task EmitDag_CompactFormat_ProducesNonIndentedJson()
  {
    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithFilenameTemplate("compact-{FlowName}")
      .UseCompactFormat()
      .Build();

    await ((IMetadataProvider)provider).Emit(FlowMetadataContext.Unsliced(BuildSampleFlow())).Run();

    var written = SysIO.Directory.GetFiles(_root, "compact-*.json").Single();
    var json = SysIO.File.ReadAllText(written);
    Assert.That(json, Does.Not.Contain("\n  "),
      "Compact format should have no indented child properties.");
  }

  [Test]
  public async Task EmitDag_TimestampedFilename_IncludesTimestamp()
  {
    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithFilenameTemplate("dag-{FlowName}-{Timestamp}")
      .WithTimestamp()
      .Build();

    await ((IMetadataProvider)provider).Emit(FlowMetadataContext.Unsliced(BuildSampleFlow())).Run();

    var files = SysIO.Directory.GetFiles(_root, "dag-sample-flow-*.json");
    Assert.That(files, Has.Length.EqualTo(1));
    var name = SysIO.Path.GetFileName(files[0]);
    Assert.That(name, Does.Match(@"dag-sample-flow-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}\.json"),
      $"Timestamped filename should include the formatted timestamp. Got: {name}");
  }

  // ── Post-run emission ────────────────────────────────────────────────

  [Test]
  public async Task EmitRun_WritesFileWithDagAndResultProjections()
  {
    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .WithRunFilenameTemplate("run-{FlowName}")
      .Build();

    var flow = BuildSampleFlow();
    var runResult = await flow.RunAsync();

    var emitResult = await ((IPostRunMetadataProvider)provider).Emit(new FlowRunMetadataContext
    {
      Static = FlowMetadataContext.Unsliced(flow),
      Result = runResult,
    }).Run();
    Assert.That(emitResult, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var written = SysIO.Directory.GetFiles(_root, "run-*.json").Single();
    using var document = JsonDocument.Parse(SysIO.File.ReadAllText(written));
    var root = document.RootElement;

    Assert.That(root.GetProperty("dag").GetProperty("flowName").GetString(),
      Is.EqualTo("sample-flow"));
    Assert.That(root.GetProperty("result").GetProperty("success").GetBoolean(), Is.True);
    Assert.That(root.GetProperty("result").GetProperty("stepResults").GetArrayLength(),
      Is.EqualTo(1));
    Assert.That(
      root.GetProperty("result").GetProperty("stepResults")[0].GetProperty("status").GetString(),
      Is.EqualTo("succeeded")
    );
  }

  // ── Provider identity ────────────────────────────────────────────────

  [Test]
  public void ProviderId_IsStable()
  {
    var provider = new JsonMetadataProviderBuilder()
      .WithOutputDirectory(_root)
      .Build();

    Assert.That(((IMetadataProvider)provider).ProviderId, Is.EqualTo("Flowthru.Json"),
      "ProviderId is the dispatcher key the host uses to label this provider.");
  }
}
