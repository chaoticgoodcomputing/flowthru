using System;
using Flowthru.Spark.Services;
using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Spark.Runtime;

/// <summary>
/// Bridges the Spark library's internal <see cref="ILoggerService"/> to the
/// standard Microsoft.Extensions.Logging pipeline so that JVM bridge diagnostics
/// respect the application's configured log levels and sinks.
/// </summary>
internal sealed class MelLoggerService : ILoggerService
{
  private readonly ILoggerFactory _factory;
  private readonly ILogger _logger;

  internal MelLoggerService(ILoggerFactory factory)
    : this(factory, factory.CreateLogger("Flowthru.Spark")) { }

  private MelLoggerService(ILoggerFactory factory, ILogger logger)
  {
    _factory = factory;
    _logger = logger;
  }

  public bool IsDebugEnabled => _logger.IsEnabled(LogLevel.Debug);

  public ILoggerService GetLoggerInstance(Type type)
  {
    try
    {
      return new MelLoggerService(_factory, _factory.CreateLogger(type.FullName ?? type.Name));
    }
    catch (ObjectDisposedException)
    {
      // The ILoggerFactory was disposed before this call — typically during test teardown
      // when GC finalizers on JvmObjectId fire after the DI container has been released.
      // Return the parent instance as a safe no-op fallback so the JvmObjectId static
      // constructor completes cleanly rather than poisoning the type with a
      // TypeInitializationException.
      return this;
    }
  }

  public void LogDebug(string message) => _logger.LogDebug("{Message}", message);

  public void LogDebug(string messageFormat, params object[] messageParameters) =>
    _logger.LogDebug(messageFormat, messageParameters);

  public void LogInfo(string message) => _logger.LogInformation("{Message}", message);

  public void LogInfo(string messageFormat, params object[] messageParameters) =>
    _logger.LogInformation(messageFormat, messageParameters);

  public void LogWarn(string message) => _logger.LogWarning("{Message}", message);

  public void LogWarn(string messageFormat, params object[] messageParameters) =>
    _logger.LogWarning(messageFormat, messageParameters);

  public void LogError(string message) => _logger.LogError("{Message}", message);

  public void LogError(string messageFormat, params object[] messageParameters) =>
    _logger.LogError(messageFormat, messageParameters);

  public void LogFatal(string message) => _logger.LogCritical("{Message}", message);

  public void LogFatal(string messageFormat, params object[] messageParameters) =>
    _logger.LogCritical(messageFormat, messageParameters);

  public void LogException(Exception e) => _logger.LogError(e, "Exception in Spark JVM bridge");
}
