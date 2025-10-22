using Flowthru.Pipelines;
using Microsoft.Extensions.Logging;

namespace Flowthru.Results;

/// <summary>
/// Formats pipeline results as human-readable console output.
/// </summary>
/// <remarks>
/// This is the default formatter used by FlowthruApplication.
/// Produces colorful, detailed output suitable for interactive terminal sessions.
/// </remarks>
public class ConsoleResultFormatter : IPipelineResultFormatter {
  /// <inheritdoc />
  public void Format(PipelineResult result, ILogger logger) {
    if (result.Success) {
      FormatSuccess(result, logger);
    } else {
      FormatFailure(result, logger);
    }
  }

  private void FormatSuccess(PipelineResult result, ILogger logger) {
    logger.LogInformation("════════════════════════════════════════════════════════════════");
    logger.LogInformation("Pipeline: {PipelineName}", result.PipelineName ?? "Unknown");
    logger.LogInformation("Status: ✓ SUCCESS");
    logger.LogInformation("Duration: {Duration:F2}s", result.ExecutionTime.TotalSeconds);
    logger.LogInformation("════════════════════════════════════════════════════════════════");
    logger.LogInformation("");

    if (result.NodeResults.Count > 0) {
      logger.LogInformation("Nodes Executed ({Count}):", result.NodeResults.Count);

      foreach (var (nodeName, nodeResult) in result.NodeResults) {
        if (nodeResult.Success) {
          logger.LogInformation(
            "  ✓ {NodeName,-40} {Duration,6:F2}s   ({InputCount,6} → {OutputCount,6} records)",
            nodeResult.NodeName,
            nodeResult.ExecutionTime.TotalSeconds,
            nodeResult.InputCount,
            nodeResult.OutputCount);
        } else {
          // This shouldn't happen in a successful pipeline, but handle it anyway
          logger.LogWarning(
            "  ✗ {NodeName,-40} {Duration,6:F2}s   FAILED",
            nodeResult.NodeName,
            nodeResult.ExecutionTime.TotalSeconds);
        }
      }

      logger.LogInformation("");
    }

    logger.LogInformation("════════════════════════════════════════════════════════════════");
  }

  private void FormatFailure(PipelineResult result, ILogger logger) {
    logger.LogError("════════════════════════════════════════════════════════════════");
    logger.LogError("Pipeline: {PipelineName}", result.PipelineName ?? "Unknown");
    logger.LogError("Status: ✗ FAILED");
    logger.LogError("Duration: {Duration:F2}s", result.ExecutionTime.TotalSeconds);
    logger.LogError("════════════════════════════════════════════════════════════════");
    logger.LogError("");

    // Show which nodes succeeded before failure
    var succeededNodes = result.NodeResults.Values.Where(n => n.Success).ToList();
    var failedNode = result.NodeResults.Values.FirstOrDefault(n => !n.Success);

    if (succeededNodes.Any()) {
      logger.LogInformation("Nodes Completed Before Failure ({Count}):", succeededNodes.Count);
      foreach (var nodeResult in succeededNodes) {
        logger.LogInformation(
          "  ✓ {NodeName,-40} {Duration,6:F2}s",
          nodeResult.NodeName,
          nodeResult.ExecutionTime.TotalSeconds);
      }
      logger.LogError("");
    }

    // Show failed node
    if (failedNode != null) {
      logger.LogError("Failed Node:");
      logger.LogError("  ✗ {NodeName}", failedNode.NodeName);
      logger.LogError("  Duration: {Duration:F2}s", failedNode.ExecutionTime.TotalSeconds);

      if (failedNode.Exception != null) {
        logger.LogError("  Error: {ErrorMessage}", failedNode.Exception.Message);
        logger.LogError("  Stack Trace:");

        // Format stack trace with indentation
        var stackLines = failedNode.Exception.StackTrace?.Split('\n') ?? Array.Empty<string>();
        foreach (var line in stackLines.Take(10)) // Limit to first 10 lines
        {
          logger.LogError("    {StackLine}", line.TrimEnd());
        }

        if (stackLines.Length > 10) {
          logger.LogError("    ... ({MoreLines} more lines)", stackLines.Length - 10);
        }
      }
    } else if (result.Exception != null) {
      // Pipeline-level exception (not from a specific node)
      logger.LogError("Pipeline Error:");
      logger.LogError("  {ErrorMessage}", result.Exception.Message);

      if (result.Exception.StackTrace != null) {
        logger.LogError("  Stack Trace:");
        var stackLines = result.Exception.StackTrace.Split('\n');
        foreach (var line in stackLines.Take(10)) {
          logger.LogError("    {StackLine}", line.TrimEnd());
        }
      }
    }

    logger.LogError("");
    logger.LogError("════════════════════════════════════════════════════════════════");
  }
}
