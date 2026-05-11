using System.Collections.Concurrent;
using System.Diagnostics;
using Flowthru.Cli;
using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Flow;
using Flowthru.Prelude;
using Microsoft.Extensions.Logging;

namespace Flowthru.Cli.Tests;

/// <summary>
/// Verifies <see cref="FlowthruActivityLogger"/> bridges Core's
/// <see cref="FlowthruActivitySource"/> events into
/// <see cref="ILogger"/> log lines.
/// </summary>
[TestFixture]
public class FlowthruActivityLoggerTests
{
  [Test]
  public async Task SubscribedListener_TranslatesStepActivitiesToLogLines()
  {
    var captured = new CapturingLogger();
    using var bridge = new FlowthruActivityLogger(captured);

    var input = ItemFactory.Singleton.Memory<int>("alog-in");
    var output = ItemFactory.Singleton.Memory<int>("alog-out");
    await input.Save(7).Run();

    var flow = FlowBuilder.CreateFlow("alog", b =>
      b.AddStep<int, int>("double", x => x * 2, input, output)
    );

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);

    Assert.That(
      captured.Lines.Any(line => line.Contains("→ double executing")),
      Is.True,
      "ActivityStarted should emit a '→ executing…' log line for the step. Got: "
        + string.Join(" | ", captured.Lines)
    );
    Assert.That(
      captured.Lines.Any(line => line.Contains("✓ double") && line.Contains("ms")),
      Is.True,
      "ActivityStopped should emit a '✓ {label} ({duration} ms)' log line. Got: "
        + string.Join(" | ", captured.Lines)
    );
  }

  [Test]
  public async Task FailedStep_BridgesAsLogWarning()
  {
    var captured = new CapturingLogger();
    using var bridge = new FlowthruActivityLogger(captured);

    var input = ItemFactory.Singleton.Memory<int>("alog-fail-in");
    var output = ItemFactory.Singleton.Memory<int>("alog-fail-out");
    await input.Save(0).Run();

    var flow = FlowBuilder.CreateFlow("boom", b =>
      b.AddStep<int, int>("explode", x => 100 / x, input, output)
    );

    await flow.RunAsync();

    Assert.That(
      captured.Entries.Any(e => e.Level == LogLevel.Warning && e.Message.Contains("✗ explode")),
      Is.True,
      "Failed step should bridge to LogLevel.Warning with '✗ {label}'. Got: "
        + string.Join(" | ", captured.Entries.Select(e => $"[{e.Level}] {e.Message}"))
    );
  }

  /// <summary>
  /// Minimal <see cref="ILogger"/> that captures emitted entries
  /// for assertion. Avoids pulling in an additional test
  /// dependency just for log capture.
  /// </summary>
  private sealed class CapturingLogger : ILogger
  {
    public ConcurrentBag<(LogLevel Level, string Message)> Entries { get; } = new();
    public IEnumerable<string> Lines => Entries.Select(e => e.Message);

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter
    )
    {
      Entries.Add((logLevel, formatter(state, exception)));
    }

    private sealed class NullScope : IDisposable
    {
      public static readonly NullScope Instance = new();
      public void Dispose() { }
    }
  }
}
