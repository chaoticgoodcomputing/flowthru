using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Metadata.Diagnostics.Tests.Fixtures;

/// <summary>
/// Test ILogger that records every Log call into an in-memory list.
/// Tests assert against the captured messages — diagnostic providers
/// emit human-readable lines, not structured events, so a string
/// match is the cleanest assertion shape.
/// </summary>
public sealed class RecordingLogger : ILogger
{
  private readonly List<(LogLevel Level, string Message)> _entries = new();

  public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;
  public IEnumerable<string> Messages => _entries.Select(e => e.Message);

  public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
  public bool IsEnabled(LogLevel logLevel) => true;

  public void Log<TState>(
    LogLevel logLevel, EventId eventId, TState state,
    Exception? exception, Func<TState, Exception?, string> formatter
  ) => _entries.Add((logLevel, formatter(state, exception)));
}
