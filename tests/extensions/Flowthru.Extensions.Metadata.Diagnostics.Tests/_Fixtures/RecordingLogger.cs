using Microsoft.Extensions.Logging;

namespace Flowthru.Meta.Diagnostics.Tests.Fixtures;

internal sealed class RecordingLogger : ILogger
{
  private readonly List<(LogLevel Level, string Message)> _entries = new();

  public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

  public IEnumerable<string> Messages => _entries.Select(e => e.Message);

  public IDisposable? BeginScope<TState>(TState state)
    where TState : notnull => null;

  public bool IsEnabled(LogLevel logLevel) => true;

  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter
  )
  {
    _entries.Add((logLevel, formatter(state, exception)));
  }
}
