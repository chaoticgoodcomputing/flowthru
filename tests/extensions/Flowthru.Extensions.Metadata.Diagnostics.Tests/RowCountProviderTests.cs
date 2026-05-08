using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Run;
using Flowthru.Extensions.Metadata.Diagnostics.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Prelude;

namespace Flowthru.Extensions.Metadata.Diagnostics.Tests;

[TestFixture]
[Category("Diagnostics")]
public class RowCountProviderTests
{
  [Test]
  public async Task Emit_ItemWithoutEfficientCount_ReportsQuestionMark()
  {
    var logger = new RecordingLogger();
    var provider = new RowCountProvider(new RowCountOptions(), logger);

    // Singleton.Memory uses MemoryStorageAdapter which doesn't
    // implement IHasEfficientCount.
    var output = ItemFactory.Singleton.Memory<int>("opaque");
    var flow = FlowBuilder.CreateFlow("flow", b =>
    {
      b.AddStep<int>("seed", () => 1, output);
    });

    await provider.Emit(Build(flow, "seed")).Run();

    Assert.That(logger.Messages, Has.Some.Contains("opaque"));
    Assert.That(logger.Messages, Has.Some.Contains("?"));
  }

  [Test]
  public async Task Emit_DirectoryItem_HasEfficientCount_ReportsActualCount()
  {
    // DirectoryStorageAdapter implements IHasEfficientCount via
    // file-listing length. Build a directory with N files.
    var dir = Path.Combine(Path.GetTempPath(), $"flowthru-rowcount-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, "a.json"), "{}");
    File.WriteAllText(Path.Combine(dir, "b.json"), "{}");
    File.WriteAllText(Path.Combine(dir, "c.json"), "{}");
    try
    {
      var logger = new RecordingLogger();
      var provider = new RowCountProvider(new RowCountOptions(), logger);

      var item = ItemFactory.Directory.JsonDocuments<RowCountSchema>("dir-item", dir);
      var flow = FlowBuilder.CreateFlow("flow", b =>
      {
        b.AddStep<Directory<RowCountSchema>>(
          "seed", () => Directory<RowCountSchema>.Empty, item);
      });

      await provider.Emit(Build(flow, "seed")).Run();

      Assert.That(logger.Messages, Has.Some.Matches<string>(
        m => m.Contains("dir-item") && m.Contains("3")));
    }
    finally
    {
      try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
    }
  }

  [Test]
  public async Task Emit_Disabled_NoOutput()
  {
    var logger = new RecordingLogger();
    var provider = new RowCountProvider(
      new RowCountOptions { Enabled = false }, logger);

    var output = ItemFactory.Singleton.Memory<int>("x");
    var flow = FlowBuilder.CreateFlow("flow", b =>
    {
      b.AddStep<int>("seed", () => 1, output);
    });
    await provider.Emit(Build(flow, "seed")).Run();
    Assert.That(logger.Entries, Is.Empty);
  }

  [Test]
  public async Task Emit_OnlyReportsActiveSliceSteps()
  {
    var logger = new RecordingLogger();
    var provider = new RowCountProvider(new RowCountOptions(), logger);

    var inactiveOut = ItemFactory.Singleton.Memory<int>("inactive-out");
    var activeOut = ItemFactory.Singleton.Memory<int>("active-out");

    var flow = FlowBuilder.CreateFlow("merged", b =>
    {
      b.AddStep<int>("inactive-step", () => 0, inactiveOut);
      b.AddStep<int>("active-step", () => 1, activeOut);
    });
    var ctx = new FlowRunMetadataContext
    {
      Static = new FlowMetadataContext
      {
        MergedFlow = flow,
        EffectiveFlow = flow,
        ActiveStepLabels = new HashSet<string>(new[] { "active-step" }, StringComparer.Ordinal),
        RequestedFlowLabel = null,
      },
      Result = new FlowResult(new[]
      {
        (StepResult)new StepResult.Succeeded("active-step", TimeSpan.FromMilliseconds(1)),
      }, TimeSpan.FromMilliseconds(1)),
    };

    await provider.Emit(ctx).Run();

    Assert.That(logger.Messages, Has.Some.Contains("active-out"));
    Assert.That(logger.Messages, Has.None.Contains("inactive-out"));
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static FlowRunMetadataContext Build(BuiltFlow flow, string activeStep)
  {
    return new FlowRunMetadataContext
    {
      Static = new FlowMetadataContext
      {
        MergedFlow = flow,
        EffectiveFlow = flow,
        ActiveStepLabels = new HashSet<string>(new[] { activeStep }, StringComparer.Ordinal),
        RequestedFlowLabel = null,
      },
      Result = new FlowResult(new[]
      {
        (StepResult)new StepResult.Succeeded(activeStep, TimeSpan.FromMilliseconds(1)),
      }, TimeSpan.FromMilliseconds(10)),
    };
  }
}
