using System.Text;
using System.Text.RegularExpressions;

namespace Flowthru.Diagnostics.Mermaid.Internal;

/// <summary>
/// Renders filename templates with dynamic token replacement.
/// Carry-over helper local to the Mermaid metadata extension —
/// duplicated verbatim with the JSON metadata extension; a follow-up
/// will lift this into a shared Diagnostics-helper namespace.
/// </summary>
internal static class FilenameTemplateParser
{
  private static readonly Regex _tokenPattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

  public static string Render(string flowName, string template, string? timestamp)
  {
    var result = _tokenPattern.Replace(template, match =>
    {
      var token = match.Groups[1].Value;
      return token switch
      {
        "FlowName" => SanitizeFilename(flowName),
        "Timestamp" => timestamp ?? string.Empty,
        _ => match.Value,
      };
    });

    return CollapseEmptySeparators(result);
  }

  private static string CollapseEmptySeparators(string input)
  {
    var collapsed = Regex.Replace(input, @"[-_]{2,}", m => m.Value[0].ToString());
    collapsed = Regex.Replace(collapsed, @"[-_]+(\.[^.]+)$", "$1");
    return collapsed.Trim('-', '_');
  }

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
