namespace Flowthru.Flows;

/// <summary>
/// Represents the result of a Flow execution.
/// </summary>
/// <remarks>
/// <para>
/// This class provides comprehensive execution information including success status,
/// timing, individual step results, and error details.
/// </para>
/// <para>
/// <strong>Usage Pattern:</strong>
/// </para>
/// <code>
/// var result = await flow.RunAsync();
///
/// if (result.Success)
/// {
///     Console.WriteLine($"Flow completed in {result.ExecutionTime.TotalSeconds:F2}s");
///     foreach (var stepResult in result.StepResults.Values)
///     {
///         Console.WriteLine($"  {stepResult.StepName}: {stepResult.ExecutionTime.TotalSeconds:F2}s");
///     }
/// }
/// else
/// {
///     Console.WriteLine($"Flow failed: {result.Exception?.Message}");
/// }
/// </code>
/// </remarks>
public class FlowResult
{
  /// <summary>
  /// The name of the Flow that was executed.
  /// </summary>
  public string? FlowName { get; init; }

  /// <summary>
  /// Indicates whether the Flow executed successfully.
  /// </summary>
  public bool Success { get; init; }

  /// <summary>
  /// Indicates whether this was a dry run (pre-flight checks only).
  /// </summary>
  public bool IsDryRun { get; init; }

  /// <summary>
  /// Total execution time for the entire flow.
  /// </summary>
  public TimeSpan ExecutionTime { get; init; }

  /// <summary>
  /// Results for individual steps, keyed by step name.
  /// </summary>
  /// <remarks>
  /// Dictionary keys are the step names as specified in the Flow definition.
  /// Values contain execution details for each step.
  /// </remarks>
  public Dictionary<string, StepResult> StepResults { get; init; } = new();

  /// <summary>
  /// Exception that caused Flow failure, if any.
  /// </summary>
  /// <remarks>
  /// Null if Success is true. Contains the first exception that caused
  /// Flow execution to halt if Success is false.
  /// </remarks>
  public Exception? Exception { get; init; }

  /// <summary>
  /// Creates a successful Flow result.
  /// </summary>
  public static FlowResult CreateSuccess(
    TimeSpan executionTime,
    Dictionary<string, StepResult> stepResults,
    string? flowName = null
  )
  {
    return new FlowResult
    {
      Success = true,
      IsDryRun = false,
      ExecutionTime = executionTime,
      StepResults = stepResults,
      FlowName = flowName,
    };
  }

  /// <summary>
  /// Creates a failed Flow result.
  /// </summary>
  public static FlowResult CreateFailure(
    TimeSpan executionTime,
    Exception exception,
    Dictionary<string, StepResult>? stepResults = null,
    string? flowName = null
  )
  {
    return new FlowResult
    {
      Success = false,
      IsDryRun = false,
      ExecutionTime = executionTime,
      Exception = exception,
      StepResults = stepResults ?? new(),
      FlowName = flowName,
    };
  }

  /// <summary>
  /// Creates a successful dry run result.
  /// </summary>
  /// <param name="preFlightDuration">Time spent on pre-flight checks</param>
  /// <param name="stepCount">Total number of steps in the flow</param>
  /// <param name="layerCount">Number of execution layers</param>
  /// <param name="validatedInputCount">Number of external inputs validated</param>
  /// <param name="flowName">Name of the flow</param>
  /// <returns>A successful dry run result</returns>
  public static FlowResult CreateDryRunSuccess(
    TimeSpan preFlightDuration,
    int stepCount,
    int layerCount,
    int validatedInputCount,
    string? flowName = null
  )
  {
    return new FlowResult
    {
      Success = true,
      IsDryRun = true,
      ExecutionTime = preFlightDuration,
      StepResults = new Dictionary<string, StepResult>(),
      FlowName = flowName,
    };
  }
}

/// <summary>
/// Represents the execution result of a single Flow step.
/// </summary>
public class StepResult
{
  /// <summary>
  /// The name of the step that was executed.
  /// </summary>
  public required string StepName { get; init; }

  /// <summary>
  /// Indicates whether the step executed successfully.
  /// </summary>
  public bool Success { get; init; }

  /// <summary>
  /// Execution time for this specific step.
  /// </summary>
  public TimeSpan ExecutionTime { get; init; }

  /// <summary>
  /// Exception that occurred during step execution, if any.
  /// </summary>
  /// <remarks>
  /// Null if Success is true. Contains the exception that caused
  /// the step to fail if Success is false.
  /// </remarks>
  public Exception? Exception { get; init; }

  /// <summary>
  /// Number of input items processed by this step.
  /// </summary>
  /// <remarks>
  /// For multi-input steps, this represents the total count across
  /// all input catalog entries.
  /// </remarks>
  public int InputCount { get; init; }

  /// <summary>
  /// Number of output items produced by this step.
  /// </summary>
  /// <remarks>
  /// For multi-output steps, this represents the total count across
  /// all output catalog entries.
  /// </remarks>
  public int OutputCount { get; init; }

  /// <summary>
  /// Creates a successful step result.
  /// </summary>
  public static StepResult CreateSuccess(
    string stepName,
    TimeSpan executionTime,
    int inputCount,
    int outputCount
  )
  {
    return new StepResult
    {
      StepName = stepName,
      Success = true,
      ExecutionTime = executionTime,
      InputCount = inputCount,
      OutputCount = outputCount,
    };
  }

  /// <summary>
  /// Creates a failed step result.
  /// </summary>
  public static StepResult CreateFailure(
    string stepName,
    TimeSpan executionTime,
    Exception exception,
    int inputCount = 0
  )
  {
    return new StepResult
    {
      StepName = stepName,
      Success = false,
      ExecutionTime = executionTime,
      Exception = exception,
      InputCount = inputCount,
      OutputCount = 0,
    };
  }
}
