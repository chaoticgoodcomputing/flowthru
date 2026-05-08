using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Run;
using Flowthru.Extensions.Metadata.Diagnostics.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Metadata.Diagnostics.Tests;

[TestFixture]
[Category("Diagnostics")]
public class StepTimingProviderTests
{
  [Test]
  public async Task Emit_NoLogger_NoSideEffect()
  {
    var provider = new StepTimingProvider(); // no logger
    var ctx = ContextWith(new[]
    {
      (StepResult)new StepResult.Succeeded("a", TimeSpan.FromMilliseconds(10)),
    });

    var result = await provider.Emit(ctx).Run();
    Assert.That(result, Is.InstanceOf<EffResult<FlowUnit>.Success>());
  }

  [Test]
  public async Task Emit_TopSlowest_ReportsSlowestFirst()
  {
    var logger = new RecordingLogger();
    var provider = new StepTimingProvider(
      new StepTimingOptions { TopSlowest = 2 }, logger);

    var ctx = ContextWith(new[]
    {
      (StepResult)new StepResult.Succeeded("fast", TimeSpan.FromMilliseconds(1)),
      new StepResult.Succeeded("medium", TimeSpan.FromMilliseconds(10)),
      new StepResult.Succeeded("slow", TimeSpan.FromMilliseconds(100)),
    });

    await provider.Emit(ctx).Run();
    var lines = logger.Messages.ToList();

    Assert.That(lines, Has.Some.Contains("slow"));
    Assert.That(lines, Has.Some.Contains("medium"));
    // Top 2 only — the fastest should not appear in the slowest list.
    var slowestSection = string.Join("\n", lines.Where(m => m.Contains("→") || m.StartsWith("  ")));
    var slowIdx = slowestSection.IndexOf("slow");
    var medIdx = slowestSection.IndexOf("medium");
    Assert.That(slowIdx, Is.GreaterThanOrEqualTo(0));
    Assert.That(medIdx, Is.GreaterThanOrEqualTo(0));
    Assert.That(slowIdx, Is.LessThan(medIdx),
      "Slowest step should be reported before the medium one.");
  }

  [Test]
  public async Task Emit_OverThreshold_FlagsAsWarning()
  {
    var logger = new RecordingLogger();
    var provider = new StepTimingProvider(
      new StepTimingOptions
      {
        TopSlowest = 0,
        SlowThreshold = TimeSpan.FromMilliseconds(50),
      }, logger);

    var ctx = ContextWith(new[]
    {
      (StepResult)new StepResult.Succeeded("ok",   TimeSpan.FromMilliseconds(10)),
      new StepResult.Succeeded("slow", TimeSpan.FromMilliseconds(75)),
    });
    await provider.Emit(ctx).Run();

    Assert.That(logger.Entries, Has.Some.Matches<(LogLevel Level, string Message)>(
      e => e.Level == LogLevel.Warning && e.Message.Contains("slow")
        && e.Message.Contains("exceeded threshold")));
    Assert.That(logger.Entries, Has.None.Matches<(LogLevel Level, string Message)>(
      e => e.Level == LogLevel.Warning && e.Message.Contains("ok ")),
      "Steps under the threshold should not warn.");
  }

  [Test]
  public async Task Emit_Disabled_NoOutput()
  {
    var logger = new RecordingLogger();
    var provider = new StepTimingProvider(
      new StepTimingOptions { Enabled = false }, logger);
    var ctx = ContextWith(new[]
    {
      (StepResult)new StepResult.Succeeded("a", TimeSpan.FromMilliseconds(10)),
    });
    await provider.Emit(ctx).Run();

    Assert.That(logger.Entries, Is.Empty);
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static FlowRunMetadataContext ContextWith(StepResult[] results)
  {
    var raw = Flowthru.Data.Catalog.ItemFactory.Singleton.Memory<int>("raw");
    var flow = FlowBuilder.CreateFlow("test", _ => { });
    return new FlowRunMetadataContext
    {
      Static = FlowMetadataContext.Unsliced(flow),
      Result = new FlowResult(results, TimeSpan.FromMilliseconds(200)),
    };
  }
}
