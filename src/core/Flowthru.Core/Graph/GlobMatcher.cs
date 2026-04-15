using System.Text;
using System.Text.RegularExpressions;

namespace Flowthru.Core.Graph;

/// <summary>
/// Converts glob patterns to compiled regular expressions for matching step and catalog item labels.
/// </summary>
/// <remarks>
/// <para>
/// Flowthru label globs use <c>.</c> as the segment separator (e.g., <c>FlowName.StepName</c>).
/// The wildcard semantics mirror common glob conventions:
/// </para>
/// <list type="bullet">
///   <item><c>**</c> — matches any sequence of characters, including <c>.</c> segment separators</item>
///   <item><c>*</c>  — matches any sequence of characters within a single segment (no <c>.</c>)</item>
///   <item><c>?</c>  — matches any single character within a segment (no <c>.</c>)</item>
/// </list>
/// <para>
/// All other characters are treated as literals (regex-escaped).
/// Matching is always case-insensitive.
/// </para>
/// </remarks>
internal static class GlobMatcher
{
    /// <summary>
    /// Returns true if the value contains any glob metacharacters (<c>*</c> or <c>?</c>).
    /// </summary>
    public static bool IsPattern(string value) => value.Contains('*') || value.Contains('?');

    /// <summary>
    /// Converts a glob pattern to a compiled, case-insensitive <see cref="Regex"/>.
    /// </summary>
    public static Regex ToRegex(string pattern)
    {
        var sb = new StringBuilder("^");
        int i = 0;
        while (i < pattern.Length)
        {
            if (i + 1 < pattern.Length && pattern[i] == '*' && pattern[i + 1] == '*')
            {
                sb.Append(".*");
                i += 2;
            }
            else if (pattern[i] == '*')
            {
                sb.Append("[^.]*");
                i++;
            }
            else if (pattern[i] == '?')
            {
                sb.Append("[^.]");
                i++;
            }
            else
            {
                sb.Append(Regex.Escape(pattern[i].ToString()));
                i++;
            }
        }
        sb.Append('$');
        return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
