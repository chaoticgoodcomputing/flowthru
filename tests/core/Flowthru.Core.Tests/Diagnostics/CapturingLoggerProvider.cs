using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Flowthru.Core.Tests.Diagnostics;

/// <summary>
/// Minimal <see cref="ILoggerProvider"/> that records every emitted
/// entry into a thread-safe bag for test assertions. Replaces the
/// retired <c>FlowthruActivityLogger</c> bridge test capture path —
/// the engine now logs directly via <see cref="ILogger{TSelf}"/>, so
/// tests register this provider via <c>AddLogging</c> and read
/// <see cref="Entries"/> after the run completes.
/// </summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
  public ConcurrentBag<LogEntry> Entries { get; } = new();

  public ILogger CreateLogger(string categoryName) => new CategoryLogger(categoryName, Entries);

  public void Dispose() { }

  public IEnumerable<LogEntry> EntriesForCategory(string categoryName) =>
    Entries.Where(e => e.Category == categoryName);

  public IEnumerable<LogEntry> EntriesForCategoryEndingWith(string categorySuffix) =>
    Entries.Where(e => e.Category.EndsWith(categorySuffix, StringComparison.Ordinal));

  private sealed class CategoryLogger : ILogger
  {
    private readonly string _category;
    private readonly ConcurrentBag<LogEntry> _sink;

    public CategoryLogger(string category, ConcurrentBag<LogEntry> sink)
    {
      _category = category;
      _sink = sink;
    }

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
      _sink.Add(new LogEntry(_category, logLevel, formatter(state, exception)));
    }

    private sealed class NullScope : IDisposable
    {
      public static readonly NullScope Instance = new();
      public void Dispose() { }
    }
  }
}

/// <summary>Single captured log entry — category, level, formatted message.</summary>
internal readonly record struct LogEntry(string Category, LogLevel Level, string Message);
