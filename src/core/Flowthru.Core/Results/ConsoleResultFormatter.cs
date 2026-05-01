using Flowthru.Core.Flows;
using Microsoft.Extensions.Logging;

namespace Flowthru.Core.Results;

/// <summary>
/// Formats pipeline results as human-readable console output.
/// </summary>
/// <remarks>
/// This is the default formatter used by the CLI.
/// Produces colorful, detailed output suitable for interactive terminal sessions.
/// </remarks>
public class ConsoleResultFormatter : IFlowResultFormatter
{
  /// <inheritdoc />
  public void Format(FlowResult result, ILogger logger)
  {
    if (result.Success)
    {
      FormatSuccess(result, logger);
    }
    else
    {
      FormatFailure(result, logger);
    }
  }

  private void FormatSuccess(FlowResult result, ILogger logger)
  {
    logger.LogInformation("================================================================");
    logger.LogInformation("Pipeline: {FlowName}", result.FlowName ?? "Unknown");
    logger.LogInformation("Status: ✓ SUCCESS");
    logger.LogInformation("Duration: {Duration:F2}s", result.ExecutionTime.TotalSeconds);
    logger.LogInformation("================================================================");
    logger.LogInformation("");

    if (result.StepResults.Count > 0)
    {
      logger.LogInformation("Steps Executed ({Count}):", result.StepResults.Count);

      foreach (var (nodeName, nodeResult) in result.StepResults)
      {
        if (nodeResult.Success)
        {
          logger.LogInformation(
            "  ✓ {StepName,-40} {Duration,6:F2}s",
            nodeResult.StepName,
            nodeResult.ExecutionTime.TotalSeconds
          );
        }
        else
        {
          // This shouldn't happen in a successful pipeline, but handle it anyway
          logger.LogWarning(
            "  ✗ {StepName,-40} {Duration,6:F2}s   FAILED",
            nodeResult.StepName,
            nodeResult.ExecutionTime.TotalSeconds
          );
        }
      }

      logger.LogInformation("");
    }

    logger.LogInformation("================================================================");
  }

  private void FormatFailure(FlowResult result, ILogger logger)
  {
    logger.LogError("================================================================");
    logger.LogError("Pipeline: {FlowName}", result.FlowName ?? "Unknown");
    logger.LogError("Status: ✗ FAILED");
    logger.LogError("Duration: {Duration:F2}s", result.ExecutionTime.TotalSeconds);
    logger.LogError("================================================================");
    logger.LogError("");

    // Show which nodes succeeded before failure
    var succeededSteps = result.StepResults.Values.Where(n => n.Success).ToList();
    var failedStep = result.StepResults.Values.FirstOrDefault(n => !n.Success);

    if (succeededSteps.Any())
    {
      logger.LogInformation("Steps Completed Before Failure ({Count}):", succeededSteps.Count);
      foreach (var nodeResult in succeededSteps)
      {
        logger.LogInformation(
          "  ✓ {StepName,-40} {Duration,6:F2}s",
          nodeResult.StepName,
          nodeResult.ExecutionTime.TotalSeconds
        );
      }
      logger.LogError("");
    }

    // Show failed node
    if (failedStep != null)
    {
      logger.LogError("Failed Step:");
      logger.LogError("  ✗ {StepName}", failedStep.StepName);
      logger.LogError("  Duration: {Duration:F2}s", failedStep.ExecutionTime.TotalSeconds);

      if (failedStep.Exception != null)
      {
        logger.LogError("  Error: {ErrorMessage}", failedStep.Exception.Message);
        logger.LogError("  Stack Trace:");

        // Format stack trace with indentation
        var stackLines = failedStep.Exception.StackTrace?.Split('\n') ?? Array.Empty<string>();
        foreach (var line in stackLines.Take(10)) // Limit to first 10 lines
        {
          logger.LogError("    {StackLine}", line.TrimEnd());
        }

        if (stackLines.Length > 10)
        {
          logger.LogError("    ... ({MoreLines} more lines)", stackLines.Length - 10);
        }
      }
    }
    else if (result.Exception != null)
    {
      // Pipeline-level exception (not from a specific node)
      logger.LogError("Pipeline Error:");
      logger.LogError("  {ErrorMessage}", result.Exception.Message);

      if (result.Exception.StackTrace != null)
      {
        logger.LogError("  Stack Trace:");
        var stackLines = result.Exception.StackTrace.Split('\n');
        foreach (var line in stackLines.Take(10))
        {
          logger.LogError("    {StackLine}", line.TrimEnd());
        }
      }
    }

    logger.LogError("");

    // Generate error report and issue URL
    var report = RuntimeErrorReport.FromFlowResult(result);
    var issueUrl = GitHubIssueUrlBuilder.Build(report);

    if (report.Classification == ErrorClassification.PossibleFrameworkBug)
    {
      logger.LogError(
        "This failure may indicate a bug in Flowthru. If a pipeline passes"
          + " pre-flight checks, it should complete successfully."
      );
      logger.LogError("Please consider reporting this issue:");
    }
    else
    {
      logger.LogWarning(
        "This failure appears to be caused by an external factor"
          + " (network, filesystem, resource exhaustion, etc.)."
      );
      logger.LogWarning("If you believe this is a Flowthru bug, you can still report it:");
    }

    logger.LogError("  {IssueUrl}", issueUrl);
    logger.LogError("");
    logger.LogError("================================================================");
  }
}
