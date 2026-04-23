using Microsoft.Extensions.Logging;

namespace Flowthru.Tests.Fixtures;

/// <summary>
/// A minimal <see cref="ILogger"/> that accumulates log entries for test assertions.
/// </summary>
public class RecordingLogger : ILogger
{
  private readonly List<LogEntry> _entries = [];

  /// <summary>All entries logged so far.</summary>
  public IReadOnlyList<LogEntry> Entries => _entries;

  /// <summary>
  /// Concatenated rendered messages, for simple string-contains assertions.
  /// </summary>
  public IEnumerable<string> Messages => _entries.Select(e => e.Message);

  public IDisposable? BeginScope<TState>(TState state)
    where TState : notnull => NullScope.Instance;

  public bool IsEnabled(LogLevel logLevel) => true;

  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter
  )
  {
    _entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
  }

  private sealed class NullScope : IDisposable
  {
    public static readonly NullScope Instance = new();

    public void Dispose() { }
  }
}

/// <summary>A single captured log entry.</summary>
public record LogEntry(LogLevel Level, string Message, Exception? Exception);
