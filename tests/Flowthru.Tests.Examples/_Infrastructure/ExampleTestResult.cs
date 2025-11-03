namespace Flowthru.Tests.Examples.Infrastructure;

/// <summary>
/// Represents the result of running an example project.
/// </summary>
public sealed class ExampleTestResult
{
  /// <summary>
  /// Gets the example project that was executed.
  /// </summary>
  public required ExampleProject Example { get; init; }

  /// <summary>
  /// Gets the exit code returned by the example's Main method.
  /// </summary>
  public required int ExitCode { get; init; }

  /// <summary>
  /// Gets a value indicating whether the example executed successfully (exit code 0).
  /// </summary>
  public bool Success => ExitCode == 0;

  /// <summary>
  /// Gets any exception that occurred during execution, if applicable.
  /// </summary>
  public Exception? Exception { get; init; }

  /// <summary>
  /// Gets the duration of the example execution.
  /// </summary>
  public TimeSpan Duration { get; init; }

  /// <summary>
  /// Gets the category of failure, if any.
  /// </summary>
  public FailureCategory Category { get; init; } = FailureCategory.None;

  /// <summary>
  /// Gets a value indicating whether this is an infrastructure failure
  /// (test framework issue) vs an application failure (example needs setup).
  /// </summary>
  public bool IsInfrastructureFailure => Category == FailureCategory.Infrastructure;

  /// <summary>
  /// Gets a diagnostic message describing the issue, if any.
  /// </summary>
  public string? DiagnosticMessage { get; init; }

  /// <summary>
  /// Gets the captured console output from the example execution.
  /// </summary>
  public string? CapturedOutput { get; init; }
}

/// <summary>
/// Categorizes the type of failure that occurred during example execution.
/// </summary>
public enum FailureCategory
{
  /// <summary>
  /// No failure - example executed successfully.
  /// </summary>
  None,

  /// <summary>
  /// Infrastructure failure - test framework couldn't execute the example
  /// (e.g., missing entry point, reflection errors, framework bugs).
  /// </summary>
  Infrastructure,

  /// <summary>
  /// Application failure - example ran but failed due to missing data,
  /// configuration, or expected runtime errors.
  /// </summary>
  Application,
}
