using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Run;
using Flowthru.Extensions.Metadata.Diagnostics.Tests.Fixtures;
using Flowthru.Flow;
using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Metadata.Diagnostics.Tests;

[TestFixture]
[Category("Diagnostics")]
public class OutputExistenceProviderTests
{
  [Test]
  public async Task Emit_AllOutputsPresent_ReportMissingOnly_NoWarnings()
  {
    var logger = new RecordingLogger();
    var provider = new OutputExistenceProvider(
      new OutputExistenceOptions { ReportMissingOnly = true }, logger);

    // Memory items with seeded values — Exists() returns true.
    var output = ItemFactory.Singleton.Memory<int>("present");
    await output.Save(7).Run();

    var flow = FlowBuilder.CreateFlow("flow", b =>
    {
      b.AddStep<int>("seed", () => 7, output);
    });
    var ctx = Build(flow, new[] { (StepResult)new StepResult.Succeeded("seed", TimeSpan.FromMilliseconds(1)) });

    await provider.Emit(ctx).Run();

    Assert.That(logger.Entries, Has.None.Matches<(LogLevel Level, string Message)>(
      e => e.Level == LogLevel.Warning),
      "ReportMissingOnly + all-present should produce no warnings.");
  }

  [Test]
  public async Task Emit_MissingOutput_LogsWarning()
  {
    var logger = new RecordingLogger();
    var provider = new OutputExistenceProvider(
      new OutputExistenceOptions { ReportMissingOnly = true }, logger);

    // Memory item never Save'd — Exists() returns false.
    var output = ItemFactory.Singleton.Memory<int>("vanished");
    var flow = FlowBuilder.CreateFlow("flow", b =>
    {
      b.AddStep<int>("seed", () => 7, output);
    });
    var ctx = Build(flow, new[] { (StepResult)new StepResult.Succeeded("seed", TimeSpan.FromMilliseconds(1)) });

    await provider.Emit(ctx).Run();

    Assert.That(logger.Entries, Has.Some.Matches<(LogLevel Level, string Message)>(
      e => e.Level == LogLevel.Warning && e.Message.Contains("vanished")));
  }

  [Test]
  public async Task Emit_FullAudit_LogsBothPresentAndMissing()
  {
    var logger = new RecordingLogger();
    var provider = new OutputExistenceProvider(
      new OutputExistenceOptions { ReportMissingOnly = false }, logger);

    var present = ItemFactory.Singleton.Memory<int>("present");
    var missing = ItemFactory.Singleton.Memory<int>("missing");
    await present.Save(1).Run();

    var flow = FlowBuilder.CreateFlow("flow", b =>
    {
      b.AddStep<int>("a", () => 1, present);
      b.AddStep<int>("b", () => 2, missing);
    });
    var ctx = Build(flow, new[]
    {
      (StepResult)new StepResult.Succeeded("a", TimeSpan.FromMilliseconds(1)),
      new StepResult.Succeeded("b", TimeSpan.FromMilliseconds(1)),
    });

    await provider.Emit(ctx).Run();

    Assert.That(logger.Messages, Has.Some.Contains("present"));
    Assert.That(logger.Messages, Has.Some.Contains("missing"));
    Assert.That(logger.Messages, Has.Some.Contains("✓"));
    Assert.That(logger.Messages, Has.Some.Contains("✗"));
  }

  [Test]
  public async Task Emit_RestrictsToActiveSlice()
  {
    var logger = new RecordingLogger();
    var provider = new OutputExistenceProvider(
      new OutputExistenceOptions { ReportMissingOnly = false }, logger);

    var inactiveOut = ItemFactory.Singleton.Memory<int>("inactive-out");
    var activeOut = ItemFactory.Singleton.Memory<int>("active-out");
    await activeOut.Save(1).Run();

    var flow = FlowBuilder.CreateFlow("merged", b =>
    {
      b.AddStep<int>("inactive-step", () => 0, inactiveOut);
      b.AddStep<int>("active-step", () => 1, activeOut);
    });

    // Slice contains only "active-step".
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
    Assert.That(logger.Messages, Has.None.Contains("inactive-out"),
      "Inactive steps' outputs should not be probed — they didn't run.");
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static FlowRunMetadataContext Build(BuiltFlow flow, StepResult[] results)
  {
    return new FlowRunMetadataContext
    {
      Static = new FlowMetadataContext
      {
        MergedFlow = flow,
        EffectiveFlow = flow,
        ActiveStepLabels = flow.Steps.Select(s => s.Label).ToHashSet(StringComparer.Ordinal),
        RequestedFlowLabel = null,
      },
      Result = new FlowResult(results, TimeSpan.FromMilliseconds(10)),
    };
  }
}
