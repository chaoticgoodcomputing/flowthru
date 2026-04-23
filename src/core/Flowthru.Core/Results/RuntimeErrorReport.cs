using System.Reflection;
using System.Runtime.InteropServices;
using Flowthru.Core.Flows;

namespace Flowthru.Core.Results;

/// <summary>
/// Classifies a runtime failure as either an external/environmental error
/// or a possible framework bug.
/// </summary>
public enum ErrorClassification
{
  /// <summary>
  /// The failure appears to be caused by external factors (network, OOM, cancellation, I/O).
  /// </summary>
  ExternalError,

  /// <summary>
  /// The failure does not match any known external cause and may indicate a Flowthru bug.
  /// </summary>
  PossibleFrameworkBug,
}

/// <summary>
/// Captures the context of a runtime pipeline failure for error reporting.
/// </summary>
public class RuntimeErrorReport
{
  /// <summary>
  /// The Flowthru library version that produced this report.
  /// </summary>
  public required string FlowthruVersion { get; init; }

  /// <summary>
  /// The .NET runtime version (e.g. "8.0.5").
  /// </summary>
  public required string RuntimeVersion { get; init; }

  /// <summary>
  /// The operating system description.
  /// </summary>
  public required string OperatingSystem { get; init; }

  /// <summary>
  /// Name of the flow that failed.
  /// </summary>
  public string? FlowName { get; init; }

  /// <summary>
  /// Name of the step that failed, if the failure is step-scoped.
  /// </summary>
  public string? FailedStepName { get; init; }

  /// <summary>
  /// The exception that caused the failure.
  /// </summary>
  public required Exception Exception { get; init; }

  /// <summary>
  /// Heuristic classification of the failure.
  /// </summary>
  public required ErrorClassification Classification { get; init; }

  /// <summary>
  /// Names of steps that completed successfully before the failure.
  /// </summary>
  public IReadOnlyList<string> CompletedSteps { get; init; } = [];

  /// <summary>
  /// Creates a <see cref="RuntimeErrorReport"/> from a failed <see cref="FlowResult"/>.
  /// </summary>
  public static RuntimeErrorReport FromFlowResult(FlowResult result)
  {
    var failedStep = result.StepResults.Values.FirstOrDefault(s => !s.Success);
    var exception = failedStep?.Exception ?? result.Exception!;

    return new RuntimeErrorReport
    {
      FlowthruVersion = GetFlowthruVersion(),
      RuntimeVersion = RuntimeInformation.FrameworkDescription,
      OperatingSystem = RuntimeInformation.OSDescription,
      FlowName = result.FlowName,
      FailedStepName = failedStep?.StepName,
      Exception = exception,
      Classification = RuntimeErrorClassifier.Classify(exception),
      CompletedSteps = result
        .StepResults.Values.Where(s => s.Success)
        .Select(s => s.StepName)
        .ToList(),
    };
  }

  private static string GetFlowthruVersion()
  {
    var assembly = typeof(RuntimeErrorReport).Assembly;
    return assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion
      ?? assembly.GetName().Version?.ToString()
      ?? "unknown";
  }
}
