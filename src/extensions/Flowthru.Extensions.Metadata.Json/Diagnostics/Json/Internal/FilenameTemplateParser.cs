using System.Text;
using System.Text.RegularExpressions;

namespace Flowthru.Diagnostics.Json.Internal;

/// <summary>
/// Renders filename templates with dynamic token replacement.
/// Carry-over helper local to the JSON metadata extension.
/// </summary>
/// <remarks>
/// <para>
/// Supported tokens:
/// </para>
/// <list type="bullet">
/// <item><c>{FlowName}</c> — sanitised flow label.</item>
/// <item><c>{Timestamp}</c> — formatted timestamp (empty when disabled).</item>
/// </list>
/// <para>
/// <strong>Slice-aware tokens deferred.</strong> The legacy parser
/// supported <c>{SliceType}</c>, <c>{Flows}</c>, <c>{From}</c>,
/// <c>{To}</c>, <c>{Only}</c>; the new <see cref="Flow.BuiltFlow"/>
/// surface doesn't carry slice metadata (the slicer takes target
/// labels as parameters and doesn't store them on the flow). If
/// slice-aware filenames return, they'll thread through additional
/// <c>Render</c> overloads — out of scope for this migration.
/// </para>
/// </remarks>
internal static class FilenameTemplateParser
{
  private static readonly Regex _tokenPattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

  /// <summary>Render <paramref name="template"/> against <paramref name="flowName"/> + <paramref name="timestamp"/>.</summary>
  public static string Render(string flowName, string template, string? timestamp)
  {
    var result = _tokenPattern.Replace(template, match =>
    {
      var token = match.Groups[1].Value;
      return token switch
      {
        "FlowName" => SanitizeFilename(flowName),
        "Timestamp" => timestamp ?? string.Empty,
        _ => match.Value, // unknown tokens left as-is
      };
    });

    return CollapseEmptySeparators(result);
  }

  /// <summary>
  /// Collapse consecutive separator characters and trim leading/trailing
  /// separators. Prevents <c>file--name</c> when a token resolves empty.
  /// </summary>
  private static string CollapseEmptySeparators(string input)
  {
    var collapsed = Regex.Replace(input, @"[-_]{2,}", m => m.Value[0].ToString());
    collapsed = Regex.Replace(collapsed, @"[-_]+(\.[^.]+)$", "$1");
    return collapsed.Trim('-', '_');
  }

  /// <summary>Replace OS-invalid filename characters with underscores.</summary>
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
