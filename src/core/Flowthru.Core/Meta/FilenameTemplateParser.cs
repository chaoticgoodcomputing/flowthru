using System.Text;
using System.Text.RegularExpressions;
using Flowthru.Core.Graph.Meta.Models;

namespace Flowthru.Core.Meta;

/// <summary>
/// Renders filename templates with dynamic token replacement.
/// </summary>
/// <remarks>
/// <para>
/// Supports tokens for pipeline metadata and slice criteria:
/// </para>
/// <list type="bullet">
/// <item><c>{FlowName}</c> - Sanitized pipeline name</item>
/// <item><c>{Timestamp}</c> - Formatted timestamp (empty if disabled)</item>
/// <item><c>{SliceType}</c> - Slice descriptor: "From", "To", "Only", "Flows", "Mixed", or empty</item>
/// <item><c>{Flows}</c> - Comma-separated list of flow names</item>
/// <item><c>{From}</c> - Comma-separated list of from labels</item>
/// <item><c>{To}</c> - Comma-separated list of to labels</item>
/// <item><c>{Only}</c> - Comma-separated list of only labels</item>
/// <item><c>{Tags}</c> - Comma-separated list of tags</item>
/// </list>
/// <para>
/// <strong>Empty Token Collapsing:</strong> Consecutive separators (hyphens, underscores)
/// around empty tokens are collapsed to prevent patterns like <c>file--name</c> or
/// <c>file-.ext</c> when slice data is absent.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// </para>
/// <code>
/// Template: "dag-{FlowName}-{Timestamp}-{SliceType}"
/// Unsliced: "dag-DataProcessing-20260304-153045"
/// Sliced:   "dag-DataProcessing-20260304-153045-FromNodes"
/// </code>
/// </remarks>
internal static class FilenameTemplateParser
{
  private static readonly Regex _tokenPattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

  /// <summary>
  /// Renders a filename template by replacing tokens with values from the DAG metadata.
  /// </summary>
  /// <param name="template">Template string with {Token} placeholders</param>
  /// <param name="dag">DAG metadata containing pipeline and slice information</param>
  /// <param name="timestamp">Optional timestamp string (empty if timestamp disabled)</param>
  /// <returns>Rendered filename with tokens replaced and empty segments collapsed</returns>
  public static string Render(DagMetadata dag, string template, string? timestamp)
  {
    var result = _tokenPattern.Replace(
      template,
      match =>
      {
        var token = match.Groups[1].Value;
        return token switch
        {
          "FlowName" => SanitizeFilename(dag.FlowName),
          "Timestamp" => timestamp ?? string.Empty,
          "SliceType" => dag.AppliedSlice?.GetSliceTypeDescriptor() ?? string.Empty,
          "Flows" => FormatList(dag.AppliedSlice?.Flows),
          "From" => FormatList(dag.AppliedSlice?.From),
          "To" => FormatList(dag.AppliedSlice?.To),
          "Only" => FormatList(dag.AppliedSlice?.Only),
          _ => match.Value, // Unknown tokens left as-is
        };
      }
    );

    // Collapse consecutive separators and trim leading/trailing separators
    return CollapseEmptySeparators(result);
  }

  /// <summary>
  /// Formats an array as a comma-separated string, or empty string if null/empty.
  /// </summary>
  private static string FormatList(string[]? items)
  {
    return items?.Length > 0 ? string.Join(",", items) : string.Empty;
  }

  /// <summary>
  /// Collapses consecutive separator characters (-, _) and trims leading/trailing separators.
  /// </summary>
  /// <remarks>
  /// Prevents patterns like "file--name" or "file-" when tokens are empty.
  /// Preserves extension separators (dots) for proper file extensions.
  /// </remarks>
  private static string CollapseEmptySeparators(string input)
  {
    // Replace multiple consecutive hyphens/underscores with a single instance
    var collapsed = Regex.Replace(input, @"[-_]{2,}", m => m.Value[0].ToString());

    // Remove separators immediately before file extensions
    collapsed = Regex.Replace(collapsed, @"[-_]+(\.[^.]+)$", "$1");

    // Trim leading/trailing separators
    return collapsed.Trim('-', '_');
  }

  /// <summary>
  /// Sanitizes a filename by replacing invalid characters with underscores.
  /// </summary>
  private static string SanitizeFilename(string filename)
  {
    var invalid = Path.GetInvalidFileNameChars();
    var sanitized = new StringBuilder(filename.Length);

    foreach (var c in filename)
    {
      sanitized.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
    }

    return sanitized.ToString();
  }
}
