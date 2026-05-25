using System.Collections.Generic;
using System.Collections.Immutable;

namespace Flowthru.Extensions.Python.SourceGenerators;

/// <summary>
/// Line-based extractor for the <c>[[package]]</c> / <c>name</c> /
/// <c>version</c> triples in a <c>uv.lock</c> TOML file. Sufficient
/// for the FTPY1501 / FTPY1502 analyzer's needs — we never reflect
/// on uv-specific keys (resolution markers, source URLs, etc.), just
/// the installed package set with versions. Rolling a full TOML
/// parser here is overkill; uv's lockfile shape is regular and the
/// format is stable.
/// </summary>
internal static class UvLockParser
{
  /// <summary>
  /// Extract <c>name</c> → <c>version</c> entries from a uv.lock
  /// file's text. Returns a case-insensitive map. Malformed
  /// fragments are silently skipped — better to surface "missing X"
  /// from the analyzer than to half-fail the parse on an edge case
  /// that doesn't affect the requirement we care about.
  /// </summary>
  public static ImmutableDictionary<string, string> ParsePackages(string content)
  {
    if (string.IsNullOrWhiteSpace(content))
      return ImmutableDictionary<string, string>.Empty;

    var builder = ImmutableDictionary.CreateBuilder<string, string>(System.StringComparer.OrdinalIgnoreCase);

    // The lockfile uses [[package]] section headers, with name / version
    // keys directly under each. Walk line-by-line; a [[package]] header
    // starts a new entry, subsequent name / version lines populate it,
    // any other [section] header closes the entry without emitting.
    string? currentName = null;
    string? currentVersion = null;
    var inPackageSection = false;

    using var reader = new System.IO.StringReader(content);
    string? line;
    while ((line = reader.ReadLine()) != null)
    {
      var trimmed = line.Trim();
      if (trimmed.Length == 0 || trimmed[0] == '#') continue;

      if (trimmed.StartsWith("[[package]]", System.StringComparison.Ordinal))
      {
        EmitIfComplete(builder, ref currentName, ref currentVersion);
        inPackageSection = true;
        continue;
      }

      if (trimmed[0] == '[')
      {
        // Any other section header — flush + leave package mode.
        EmitIfComplete(builder, ref currentName, ref currentVersion);
        inPackageSection = false;
        continue;
      }

      if (!inPackageSection) continue;

      if (TryParseKey(trimmed, "name", out var name)) currentName = name;
      else if (TryParseKey(trimmed, "version", out var version)) currentVersion = version;
    }

    EmitIfComplete(builder, ref currentName, ref currentVersion);
    return builder.ToImmutable();
  }

  private static void EmitIfComplete(
    ImmutableDictionary<string, string>.Builder builder,
    ref string? name,
    ref string? version
  )
  {
    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(version))
    {
      // Last write wins — uv.lock should not duplicate entries but we
      // do not want to crash on edge cases.
      builder[name!] = version!;
    }
    name = null;
    version = null;
  }

  /// <summary>
  /// Match a <c>key = "value"</c> line where <paramref name="key"/>
  /// is the expected name. Tolerates the four shapes uv emits:
  /// <c>name = "x"</c>, <c>name="x"</c>, <c>name = 'x'</c>,
  /// <c>name = x</c> (unquoted, rare).
  /// </summary>
  private static bool TryParseKey(string line, string key, out string value)
  {
    value = string.Empty;
    if (!line.StartsWith(key, System.StringComparison.Ordinal)) return false;

    var rest = line.Substring(key.Length).TrimStart();
    if (rest.Length == 0 || rest[0] != '=') return false;
    rest = rest.Substring(1).TrimStart();
    if (rest.Length == 0) return false;

    var first = rest[0];
    if (first == '"' || first == '\'')
    {
      var closing = rest.IndexOf(first, 1);
      if (closing < 0) return false;
      value = rest.Substring(1, closing - 1);
      return true;
    }

    // Unquoted scalar — uv rarely emits this for name/version but be
    // permissive. Take everything up to a trailing comment.
    var hash = rest.IndexOf('#');
    value = (hash < 0 ? rest : rest.Substring(0, hash)).Trim();
    return value.Length > 0;
  }
}
