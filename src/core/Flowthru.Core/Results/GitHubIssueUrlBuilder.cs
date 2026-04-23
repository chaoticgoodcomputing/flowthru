using System.Text;

namespace Flowthru.Core.Results;

/// <summary>
/// Builds a pre-filled GitHub issue URL from a <see cref="RuntimeErrorReport"/>.
/// </summary>
public static class GitHubIssueUrlBuilder
{
  private const string BaseUrl = "https://github.com/chaoticgoodcomputing/flowthru/issues/new";

  // GitHub truncates URLs around 8192 characters. Leave headroom for the
  // query-string envelope (?title=...&body=...&labels=...).
  private const int MaxUrlLength = 8000;

  /// <summary>
  /// Generates a GitHub new-issue URL pre-populated with failure context.
  /// </summary>
  public static string Build(RuntimeErrorReport report)
  {
    var title = BuildTitle(report);
    var body = BuildBody(report);
    var label =
      report.Classification == ErrorClassification.PossibleFrameworkBug ? "bug" : "external-error";

    var url =
      $"{BaseUrl}?title={Uri.EscapeDataString(title)}"
      + $"&body={Uri.EscapeDataString(body)}"
      + $"&labels={Uri.EscapeDataString(label)}";

    if (url.Length > MaxUrlLength)
    {
      // Re-build with a truncated body to stay within limits.
      var budget =
        MaxUrlLength
        - BaseUrl.Length
        - "?title=".Length
        - Uri.EscapeDataString(title).Length
        - "&body=".Length
        - "&labels=".Length
        - Uri.EscapeDataString(label).Length;

      var truncatedBody = TruncateEncodedBody(body, budget);
      url =
        $"{BaseUrl}?title={Uri.EscapeDataString(title)}"
        + $"&body={Uri.EscapeDataString(truncatedBody)}"
        + $"&labels={Uri.EscapeDataString(label)}";
    }

    return url;
  }

  private static string BuildTitle(RuntimeErrorReport report)
  {
    var exType = report.Exception.GetType().Name;
    var step = report.FailedStepName is not null ? $" in {report.FailedStepName}" : "";
    return $"[Runtime Error] {exType}{step}";
  }

  private static string BuildBody(RuntimeErrorReport report)
  {
    var sb = new StringBuilder();

    sb.AppendLine("## Runtime Error Report");
    sb.AppendLine();
    sb.AppendLine("*This issue was generated automatically by Flowthru's error reporter.*");
    sb.AppendLine();

    sb.AppendLine("### Environment");
    sb.AppendLine();
    sb.AppendLine($"- **Flowthru:** {report.FlowthruVersion}");
    sb.AppendLine($"- **Runtime:** {report.RuntimeVersion}");
    sb.AppendLine($"- **OS:** {report.OperatingSystem}");
    sb.AppendLine();

    sb.AppendLine("### Failure Context");
    sb.AppendLine();
    if (report.FlowName is not null)
      sb.AppendLine($"- **Flow:** {report.FlowName}");
    if (report.FailedStepName is not null)
      sb.AppendLine($"- **Failed Step:** {report.FailedStepName}");
    sb.AppendLine(
      $"- **Classification:** {(report.Classification == ErrorClassification.PossibleFrameworkBug ? "Possible framework bug" : "External / environmental error")}"
    );
    if (report.CompletedSteps.Count > 0)
      sb.AppendLine($"- **Completed Steps:** {string.Join(", ", report.CompletedSteps)}");
    sb.AppendLine();

    sb.AppendLine("### Exception");
    sb.AppendLine();
    sb.AppendLine($"**Type:** `{report.Exception.GetType().FullName}`");
    sb.AppendLine();
    sb.AppendLine($"**Message:** {report.Exception.Message}");
    sb.AppendLine();

    if (report.Exception.StackTrace is not null)
    {
      sb.AppendLine("**Stack Trace:**");
      sb.AppendLine("```");
      // Include up to 15 frames — enough for diagnosis without bloating the URL.
      var lines = report.Exception.StackTrace.Split('\n');
      foreach (var line in lines.Take(15))
        sb.AppendLine(line.TrimEnd());
      if (lines.Length > 15)
        sb.AppendLine($"... ({lines.Length - 15} more frames)");
      sb.AppendLine("```");
    }

    sb.AppendLine();
    sb.AppendLine(
      "---\n*Please add any additional context about what the pipeline was doing when this error occurred.*"
    );

    return sb.ToString();
  }

  private static string TruncateEncodedBody(string body, int encodedBudget)
  {
    const string suffix = "\n\n*(truncated — body exceeded URL length limit)*";

    // Binary search for the longest prefix whose encoded form fits the budget.
    var lo = 0;
    var hi = body.Length;
    while (lo < hi)
    {
      var mid = (lo + hi + 1) / 2;
      if (
        Uri.EscapeDataString(body[..mid]).Length + Uri.EscapeDataString(suffix).Length
        <= encodedBudget
      )
        lo = mid;
      else
        hi = mid - 1;
    }

    return body[..lo] + suffix;
  }
}
