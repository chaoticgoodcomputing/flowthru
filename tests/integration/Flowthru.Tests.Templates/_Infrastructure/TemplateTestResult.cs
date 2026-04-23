namespace Flowthru.Tests.Templates.Infrastructure;

/// <summary>
/// Result of running a template-generated project test.
/// </summary>
public sealed record TemplateTestResult
{
  /// <summary>
  /// Gets the template project that was tested.
  /// </summary>
  public required TemplateProject Project { get; init; }

  /// <summary>
  /// Gets whether the test succeeded.
  /// </summary>
  public required bool Success { get; init; }

  /// <summary>
  /// Gets the exit code from the dotnet run command.
  /// </summary>
  public required int ExitCode { get; init; }

  /// <summary>
  /// Gets the captured standard output.
  /// </summary>
  public string? StandardOutput { get; init; }

  /// <summary>
  /// Gets the captured standard error.
  /// </summary>
  public string? StandardError { get; init; }

  /// <summary>
  /// Gets the total duration of the test (generation + restore + execution).
  /// </summary>
  public required TimeSpan Duration { get; init; }

  /// <summary>
  /// Gets any exception that occurred during test execution.
  /// </summary>
  public Exception? Exception { get; init; }

  /// <summary>
  /// Gets a diagnostic message describing the failure, if any.
  /// </summary>
  public string? DiagnosticMessage { get; init; }
}
