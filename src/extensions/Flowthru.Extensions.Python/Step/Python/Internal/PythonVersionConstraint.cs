using System;
using System.Collections.Immutable;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// A compound version constraint — list of (operator, version)
/// clauses combined by AND. Sufficient for the common Python
/// requirement-string shape:
/// <c>"&gt;=1.0,&lt;2.0,!=1.5"</c>.
/// </summary>
/// <remarks>
/// <para>
/// Supported operators: <c>==</c>, <c>!=</c>, <c>&gt;=</c>,
/// <c>&lt;=</c>, <c>&gt;</c>, <c>&lt;</c>, <c>~=</c> (compatible
/// release). No epoch / local-version handling; no wildcard
/// operators (<c>==1.0.*</c>). The wildcard form converts to its
/// equivalent compound (<c>&gt;=1.0,&lt;1.1</c>) — callers needing
/// wildcards should expand client-side.
/// </para>
/// <para>
/// Constraint intersection (<see cref="IntersectWith"/>) is the
/// algebra's primary operation: two declarers requiring the same
/// package collapse to the conjunction of their clauses. The result
/// may be unsatisfiable (e.g. <c>&gt;=2.0</c> ∩ <c>&lt;1.0</c>) —
/// this slice does not detect that symbolically; an unsatisfiable
/// constraint just fails for any installed version, surfaced via the
/// <see cref="Satisfies"/> check. Symbolic conflict detection is
/// deferred to the design-time analyzer (slice 3).
/// </para>
/// </remarks>
internal sealed record PythonVersionConstraint(
  ImmutableArray<PythonConstraintClause> Clauses
)
{
  public static readonly PythonVersionConstraint Any = new(ImmutableArray<PythonConstraintClause>.Empty);

  /// <summary>
  /// Parse a constraint string (e.g. <c>"&gt;=14,&lt;16"</c>). A
  /// null or whitespace input yields <see cref="Any"/> — the
  /// identity element for intersection. Returns <c>true</c> when
  /// every clause parsed cleanly; <c>false</c> on the first
  /// unrecognised operator or unparseable version.
  /// </summary>
  public static bool TryParse(string? input, out PythonVersionConstraint constraint)
  {
    constraint = Any;
    // Explicit null check before the string ops — netstandard2.0's
    // reference assembly doesn't carry [NotNullWhen(false)] on
    // string.IsNullOrWhiteSpace, so the analyzer-side build can't
    // narrow the nullable param otherwise.
    if (input is null) return true;
    var trimmedInput = input.Trim();
    if (trimmedInput.Length == 0) return true;

    // Split + TrimEntries is post-netstandard2.0; do it manually so
    // the source-generator project (netstandard2.0) compiles too.
    var clauses = ImmutableArray.CreateBuilder<PythonConstraintClause>();
    foreach (var raw in trimmedInput.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
    {
      if (raw is null) continue;
      var trimmed = raw.Trim();
      if (trimmed.Length == 0) continue;
      if (!TryParseClause(trimmed, out var clause)) return false;
      clauses.Add(clause);
    }

    constraint = new PythonVersionConstraint(clauses.ToImmutable());
    return true;
  }

  /// <summary>
  /// Does <paramref name="version"/> satisfy every clause in this
  /// constraint? Empty constraint (<see cref="Any"/>) returns true
  /// for every version — that is the algebra's identity.
  /// </summary>
  public bool Satisfies(PythonVersion version)
  {
    foreach (var clause in Clauses)
    {
      if (!clause.Satisfies(version)) return false;
    }
    return true;
  }

  /// <summary>
  /// Conjunction of two constraints — every clause from both sides.
  /// Used to fold multiple capability declarations on the same
  /// package into a single effective constraint.
  /// </summary>
  public PythonVersionConstraint IntersectWith(PythonVersionConstraint other)
  {
    if (other.Clauses.Length == 0) return this;
    if (Clauses.Length == 0) return other;
    return new PythonVersionConstraint(Clauses.AddRange(other.Clauses));
  }

  /// <summary>
  /// Canonical comma-joined form (<c>"&gt;=14,&lt;16"</c>). Empty
  /// constraint renders as <c>"*"</c> for diagnostic readability.
  /// </summary>
  public override string ToString() =>
    Clauses.Length == 0 ? "*" : string.Join(",", Clauses);

  private static bool TryParseClause(string raw, out PythonConstraintClause clause)
  {
    clause = default;
    if (string.IsNullOrWhiteSpace(raw)) return false;

    var s = raw.Trim();

    // Order matters — longer operators first so '==' isn't shadowed
    // by '=' and '>=' isn't shadowed by '>'.
    var (op, versionPart) = s switch
    {
      var x when x.StartsWith("===", StringComparison.Ordinal) =>
        // PEP 440 arbitrary-equality. Treat as exact-match for our
        // subset; we don't model the "byte-exact string match"
        // semantics PEP 440 specifies because we lack epoch / local.
        (PythonConstraintOp.Equal, x.Substring(3).TrimStart()),
      var x when x.StartsWith("==", StringComparison.Ordinal) =>
        (PythonConstraintOp.Equal, x.Substring(2).TrimStart()),
      var x when x.StartsWith("!=", StringComparison.Ordinal) =>
        (PythonConstraintOp.NotEqual, x.Substring(2).TrimStart()),
      var x when x.StartsWith(">=", StringComparison.Ordinal) =>
        (PythonConstraintOp.GreaterOrEqual, x.Substring(2).TrimStart()),
      var x when x.StartsWith("<=", StringComparison.Ordinal) =>
        (PythonConstraintOp.LessOrEqual, x.Substring(2).TrimStart()),
      var x when x.StartsWith("~=", StringComparison.Ordinal) =>
        (PythonConstraintOp.CompatibleRelease, x.Substring(2).TrimStart()),
      var x when x.StartsWith(">", StringComparison.Ordinal) =>
        (PythonConstraintOp.Greater, x.Substring(1).TrimStart()),
      var x when x.StartsWith("<", StringComparison.Ordinal) =>
        (PythonConstraintOp.Less, x.Substring(1).TrimStart()),
      // Bare version with no operator — PEP 440 isn't strict here;
      // pip treats it as '=='. Mirror that.
      _ => (PythonConstraintOp.Equal, s),
    };

    if (!PythonVersion.TryParse(versionPart, out var version)) return false;
    clause = new PythonConstraintClause(op, version);
    return true;
  }
}

/// <summary>
/// One clause of a compound constraint — operator plus version.
/// </summary>
internal readonly record struct PythonConstraintClause(
  PythonConstraintOp Op,
  PythonVersion Version
)
{
  public bool Satisfies(PythonVersion installed) => Op switch
  {
    PythonConstraintOp.Equal => installed.CompareTo(Version) == 0,
    PythonConstraintOp.NotEqual => installed.CompareTo(Version) != 0,
    PythonConstraintOp.Greater => installed.CompareTo(Version) > 0,
    PythonConstraintOp.GreaterOrEqual => installed.CompareTo(Version) >= 0,
    PythonConstraintOp.Less => installed.CompareTo(Version) < 0,
    PythonConstraintOp.LessOrEqual => installed.CompareTo(Version) <= 0,
    PythonConstraintOp.CompatibleRelease => SatisfiesCompatibleRelease(installed),
    _ => false,
  };

  /// <summary>
  /// PEP 440 <c>~=X.Y</c> means <c>&gt;=X.Y, &lt;X+1</c>;
  /// <c>~=X.Y.Z</c> means <c>&gt;=X.Y.Z, &lt;X.Y+1</c>. Generally:
  /// drop the last release segment, increment the next-to-last by
  /// one, that is the exclusive upper bound. Lower bound is the
  /// constraint version itself, inclusive.
  /// </summary>
  private bool SatisfiesCompatibleRelease(PythonVersion installed)
  {
    if (installed.CompareTo(Version) < 0) return false;
    if (Version.Release.Length < 2)
    {
      // ~=X by itself is invalid in PEP 440 but be lenient — treat as >=X.
      return true;
    }
    var upperRelease = Version.Release.RemoveAt(Version.Release.Length - 1).ToBuilder();
    upperRelease[upperRelease.Count - 1] += 1;
    var upper = new PythonVersion(
      upperRelease.ToImmutable(),
      PythonPreReleaseKind.None,
      0
    );
    return installed.CompareTo(upper) < 0;
  }

  public override string ToString()
  {
    var opStr = Op switch
    {
      PythonConstraintOp.Equal => "==",
      PythonConstraintOp.NotEqual => "!=",
      PythonConstraintOp.Greater => ">",
      PythonConstraintOp.GreaterOrEqual => ">=",
      PythonConstraintOp.Less => "<",
      PythonConstraintOp.LessOrEqual => "<=",
      PythonConstraintOp.CompatibleRelease => "~=",
      _ => "?",
    };
    return $"{opStr}{Version}";
  }
}

internal enum PythonConstraintOp
{
  Equal,
  NotEqual,
  Greater,
  GreaterOrEqual,
  Less,
  LessOrEqual,
  CompatibleRelease,
}
