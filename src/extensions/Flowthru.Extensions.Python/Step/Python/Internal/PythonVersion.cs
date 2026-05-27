using System;
using System.Collections.Immutable;
using System.Globalization;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Minimal-viable PEP 440 version representation: dotted-numeric
/// release segments + optional pre-release tag (a / b / rc with a
/// numeric suffix). Sufficient for ~99% of declared Python ML deps.
/// Explicit non-goals: epochs (<c>1!1.0</c>), local versions
/// (<c>1.0+local</c>), post / dev primary releases (<c>1.0.post1</c>,
/// <c>1.0.dev1</c>) — these almost never appear in declared
/// requirements, only in installed-package metadata, and we punt on
/// them until a real case surfaces.
/// </summary>
internal readonly record struct PythonVersion(
  ImmutableArray<int> Release,
  PythonPreReleaseKind PreReleaseKind,
  int PreReleaseNumber
) : IComparable<PythonVersion>
{
  public static readonly PythonVersion Zero = new(
    ImmutableArray.Create(0),
    PythonPreReleaseKind.None,
    0
  );

  /// <summary>
  /// Parse a version string (PEP 440 subset). Returns <c>true</c> on
  /// success; <paramref name="version"/> carries
  /// <see cref="Zero"/> on failure. Permissive normalisation: leading
  /// <c>v</c> stripped, <c>alpha</c>/<c>beta</c> normalised to
  /// <c>a</c>/<c>b</c>, dot / dash separators between the release
  /// and pre-release segment tolerated.
  /// </summary>
  public static bool TryParse(string input, out PythonVersion version)
  {
    version = Zero;
    if (string.IsNullOrWhiteSpace(input)) return false;

    var s = input.Trim();
    // String overloads (not char) — char overloads of StartsWith /
    // Contains are post-netstandard2.0; the source-generator project
    // that links this file is netstandard2.0.
    if (s.StartsWith("v", StringComparison.Ordinal) || s.StartsWith("V", StringComparison.Ordinal))
    {
      s = s.Substring(1);
    }

    // Reject PEP 440 features we cannot accurately represent —
    // silently dropping these would misinform downstream comparisons
    // (epoch is order-significant; local versions are a distinct
    // tag-equality semantic). The hook's lenient-parse fallback
    // catches these and skips the constraint check.
    if (s.Length == 0 || s[0] == '.') return false;
    if (s.IndexOf('!') >= 0) return false;
    if (s.IndexOf('+') >= 0) return false;

    // Split release segment from prerelease segment. The prerelease
    // marker is any of a/b/c/rc/alpha/beta after a numeric prefix,
    // optionally preceded by '.', '-', or '_'. We scan for the first
    // non-digit-non-dot character.
    var releaseEnd = 0;
    while (releaseEnd < s.Length && (char.IsDigit(s[releaseEnd]) || s[releaseEnd] == '.'))
    {
      releaseEnd++;
    }
    var releasePart = s.Substring(0, releaseEnd);
    var preReleasePart = s.Substring(releaseEnd);

    var releaseSegments = releasePart.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
    if (releaseSegments.Length == 0) return false;

    var parsedRelease = ImmutableArray.CreateBuilder<int>(releaseSegments.Length);
    foreach (var segment in releaseSegments)
    {
      if (!int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
        return false;
      parsedRelease.Add(n);
    }

    var (kind, number) = ParsePreRelease(preReleasePart);
    if (kind == PythonPreReleaseKind.Invalid) return false;

    version = new PythonVersion(
      parsedRelease.MoveToImmutable(),
      kind,
      number
    );
    return true;
  }

  private static (PythonPreReleaseKind, int) ParsePreRelease(string s)
  {
    if (string.IsNullOrEmpty(s)) return (PythonPreReleaseKind.None, 0);

    // Permissive separators per PEP 440 normalisation (§ "Pre-release
    // separators"): strip a leading '.', '-', or '_' before the kind tag.
    var trimmed = s.TrimStart('.', '-', '_');

    var (kind, rest) = trimmed switch
    {
      var x when x.StartsWith("alpha", StringComparison.OrdinalIgnoreCase) =>
        (PythonPreReleaseKind.Alpha, x.Substring(5)),
      var x when x.StartsWith("beta", StringComparison.OrdinalIgnoreCase) =>
        (PythonPreReleaseKind.Beta, x.Substring(4)),
      var x when x.StartsWith("rc", StringComparison.OrdinalIgnoreCase) =>
        (PythonPreReleaseKind.ReleaseCandidate, x.Substring(2)),
      // single-letter forms must not be followed by another letter
      // (otherwise "abc" would be parsed as alpha + "bc")
      var x when x.Length > 0 && (x[0] == 'a' || x[0] == 'A') &&
        (x.Length == 1 || !char.IsLetter(x[1])) =>
        (PythonPreReleaseKind.Alpha, x.Substring(1)),
      var x when x.Length > 0 && (x[0] == 'b' || x[0] == 'B') &&
        (x.Length == 1 || !char.IsLetter(x[1])) =>
        (PythonPreReleaseKind.Beta, x.Substring(1)),
      // PEP 440 also accepts bare 'c' as an alias for 'rc'.
      var x when x.Length > 0 && (x[0] == 'c' || x[0] == 'C') &&
        (x.Length == 1 || !char.IsLetter(x[1])) =>
        (PythonPreReleaseKind.ReleaseCandidate, x.Substring(1)),
      // Anything else (post/dev/local tails, garbage) — we silently
      // accept by treating the *version up to here* as a final
      // release, ignoring the tail. PEP 440-installed strings often
      // have post/dev suffixes we don't model; failing on them would
      // make `pip list` output break the algebra.
      _ => (PythonPreReleaseKind.None, string.Empty),
    };

    if (kind == PythonPreReleaseKind.None) return (PythonPreReleaseKind.None, 0);

    // Allow optional separator between kind and number ('a.1', 'a-1').
    rest = rest.TrimStart('.', '-', '_');
    if (rest.Length == 0)
    {
      return (kind, 0);
    }

    if (!int.TryParse(rest, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
    {
      // Garbage after kind tag — be lenient, treat as the kind with
      // number 0 rather than failing the whole parse.
      return (kind, 0);
    }
    return (kind, n);
  }

  /// <inheritdoc/>
  public int CompareTo(PythonVersion other)
  {
    var max = Math.Max(Release.Length, other.Release.Length);
    for (var i = 0; i < max; i++)
    {
      var a = i < Release.Length ? Release[i] : 0;
      var b = i < other.Release.Length ? other.Release[i] : 0;
      var cmp = a.CompareTo(b);
      if (cmp != 0) return cmp;
    }

    // Same release segments — prerelease ordering: any prerelease
    // sorts *before* the equivalent final release. PEP 440 § "Summary
    // of permitted suffixes and relative ordering".
    if (PreReleaseKind == other.PreReleaseKind)
    {
      return PreReleaseNumber.CompareTo(other.PreReleaseNumber);
    }
    return PreReleaseKind.CompareTo(other.PreReleaseKind);
  }

  /// <summary>
  /// Canonical PEP 440-shaped string form. Round-trips with
  /// <see cref="TryParse"/> for any version this struct can represent.
  /// </summary>
  public override string ToString()
  {
    var release = string.Join(".", Release);
    return PreReleaseKind switch
    {
      PythonPreReleaseKind.Alpha => $"{release}a{PreReleaseNumber}",
      PythonPreReleaseKind.Beta => $"{release}b{PreReleaseNumber}",
      PythonPreReleaseKind.ReleaseCandidate => $"{release}rc{PreReleaseNumber}",
      _ => release,
    };
  }
}

/// <summary>
/// Ordering of pre-release kinds — values chosen so default
/// <see cref="int"/> comparison gives PEP 440's documented sort
/// (alpha &lt; beta &lt; rc &lt; final).
/// </summary>
internal enum PythonPreReleaseKind
{
  Alpha = 0,
  Beta = 1,
  ReleaseCandidate = 2,
  None = 3,
  Invalid = -1,
}
