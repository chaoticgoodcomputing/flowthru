using Flowthru.Core.Tests.Diagnostics;
using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Microsoft.Extensions.Logging;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// Asserts <see cref="ParallelFlowScheduler"/> emits per-step
/// lifecycle logs directly through its injected
/// <see cref="ILogger"/>. Regression coverage for ADR-0006 —
/// replaces the equivalent bridge-rendering tests that were retired
/// with <c>FlowthruActivityLogger</c>.
/// </summary>
[TestFixture]
public class ParallelFlowSchedulerLoggingTests
{
  private static ILogger LoggerFrom(CapturingLoggerProvider provider) =>
    new LoggerFactory(new[] { provider }).CreateLogger("Flowthru");

  [Test]
  public async Task SuccessfulStep_LogsExecutingThenSucceeded()
  {
    var capture = new CapturingLoggerProvider();
    var scheduler = new ParallelFlowScheduler(LoggerFrom(capture));

    var input = ItemFactory.Singleton.Memory<int>("psl-ok-in");
    var output = ItemFactory.Singleton.Memory<int>("psl-ok-out");
    await input.Save(21).Run();

    var flow = FlowBuilder.CreateFlow("psl-ok", b =>
      b.AddStep<int, int>("double", x => x * 2, input, output)
    );

    var result = await scheduler.ExecuteAsync(flow, ExecutionOptions.Default);
    Assert.That(result.IsSuccess, Is.True);

    var entries = capture.Entries.ToList();
    Assert.That(
      entries.Any(e => e.Level == LogLevel.Information && e.Message.Contains("→ double executing")),
      Is.True,
      "Per-step run should emit an Information '→ {Label} executing…' line. Got: "
        + string.Join(" | ", entries.Select(e => $"[{e.Level}] {e.Message}"))
    );
    Assert.That(
      entries.Any(e =>
        e.Level == LogLevel.Information
        && e.Message.Contains("✓ double")
        && e.Message.Contains("ms")),
      Is.True,
      "Per-step completion should emit '✓ {Label} ({Duration} ms)'. Got: "
        + string.Join(" | ", entries.Select(e => $"[{e.Level}] {e.Message}"))
    );
  }

  [Test]
  public async Task FailedStep_LogsWarningWithReason()
  {
    var capture = new CapturingLoggerProvider();
    var scheduler = new ParallelFlowScheduler(LoggerFrom(capture));

    var input = ItemFactory.Singleton.Memory<int>("psl-fail-in");
    var output = ItemFactory.Singleton.Memory<int>("psl-fail-out");
    await input.Save(0).Run();

    var flow = FlowBuilder.CreateFlow("psl-fail", b =>
      b.AddStep<int, int>("explode", x => 100 / x, input, output)
    );

    await scheduler.ExecuteAsync(flow, ExecutionOptions.Default);

    var warnings = capture.Entries
      .Where(e => e.Level == LogLevel.Warning && e.Message.Contains("✗ explode"))
      .ToList();
    Assert.That(warnings, Is.Not.Empty,
      "Failed step should emit a Warning-level '✗ {Label}' line with the failure reason. Got: "
        + string.Join(" | ", capture.Entries.Select(e => $"[{e.Level}] {e.Message}"))
    );
  }

  [Test]
  public async Task ParameterlessCtor_UsesNullLogger_NoThrowsNoSinks()
  {
    // Backwards-compatibility contract: BuiltFlow.RunAsync still works
    // for hosts that haven't migrated to ActivatorUtilities. The
    // parameterless ctor must fall back to NullLogger<T>.Instance,
    // not throw on a missing dependency.
    var scheduler = new ParallelFlowScheduler();

    var input = ItemFactory.Singleton.Memory<int>("psl-null-in");
    var output = ItemFactory.Singleton.Memory<int>("psl-null-out");
    await input.Save(3).Run();

    var flow = FlowBuilder.CreateFlow("psl-null", b =>
      b.AddStep<int, int>("noop", x => x, input, output)
    );

    var result = await scheduler.ExecuteAsync(flow, ExecutionOptions.Default);
    Assert.That(result.IsSuccess, Is.True,
      "Parameterless scheduler ctor must run flows successfully with the NullLogger fallback.");
  }
}
