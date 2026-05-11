using Flowthru.Diagnostics;
using Flowthru.Diagnostics.Run;
using Flowthru.Extensions.Metadata.Diagnostics.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Validation.Runtime;

namespace Flowthru.Extensions.Metadata.Diagnostics.Tests;

[TestFixture]
[Category("Diagnostics")]
public class RunSummaryProviderTests
{
  [Test]
  public async Task Emit_AllSucceeded_ReportsSuccessStatus()
  {
    var logger = new RecordingLogger();
    var provider = new RunSummaryProvider(new RunSummaryOptions(), logger);
    var ctx = Build(
      requested: "DataScience",
      effective: "DataScience",
      runDuration: TimeSpan.FromSeconds(2.5),
      results: new[]
      {
        (StepResult)new StepResult.Succeeded("a", TimeSpan.FromMilliseconds(100)),
        new StepResult.Succeeded("b", TimeSpan.FromMilliseconds(2400)),
      }
    );

    await provider.Emit(ctx).Run();

    Assert.That(logger.Messages, Has.Some.Contains("DataScience"));
    Assert.That(logger.Messages, Has.Some.Contains("success"));
    Assert.That(logger.Messages, Has.Some.Contains("2.500s"),
      "Total run duration should appear.");
    Assert.That(logger.Messages, Has.Some.Contains("2 succeeded"));
    Assert.That(logger.Messages, Has.Some.Contains("Slowest:  b"));
  }

  [Test]
  public async Task Emit_WithFailure_ReportsFailureStatus()
  {
    var logger = new RecordingLogger();
    var provider = new RunSummaryProvider(new RunSummaryOptions(), logger);
    var ctx = Build(
      requested: null,
      effective: "__merged__",
      runDuration: TimeSpan.FromSeconds(1),
      results: new[]
      {
        (StepResult)new StepResult.Succeeded("a", TimeSpan.FromMilliseconds(500)),
        new StepResult.Failed(
          "b",
          new RuntimeError.External("test", new InvalidOperationException("boom")),
          TimeSpan.FromMilliseconds(500)),
      }
    );

    await provider.Emit(ctx).Run();

    Assert.That(logger.Messages, Has.Some.Contains("failure"));
    Assert.That(logger.Messages, Has.Some.Contains("1 succeeded, 1 failed"));
  }

  [Test]
  public async Task Emit_FlowNamePrefersRequestedSlice()
  {
    var logger = new RecordingLogger();
    var provider = new RunSummaryProvider(new RunSummaryOptions(), logger);
    var ctx = Build(
      requested: "Reporting",  // user invoked the slice
      effective: "Reporting",
      runDuration: TimeSpan.FromMilliseconds(50),
      results: new[]
      {
        (StepResult)new StepResult.Succeeded("only-step", TimeSpan.FromMilliseconds(50)),
      }
    );
    await provider.Emit(ctx).Run();
    Assert.That(logger.Messages, Has.Some.Contains("Reporting"));
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static FlowRunMetadataContext Build(
    string? requested, string effective, TimeSpan runDuration, StepResult[] results
  )
  {
    var flow = FlowBuilder.CreateFlow(effective, _ => { });
    return new FlowRunMetadataContext
    {
      Static = new FlowMetadataContext
      {
        MergedFlow = flow,
        EffectiveFlow = flow,
        ActiveStepLabels = new HashSet<string>(StringComparer.Ordinal),
        RequestedFlowLabel = requested,
      },
      Result = new FlowResult(results, runDuration),
    };
  }
}
