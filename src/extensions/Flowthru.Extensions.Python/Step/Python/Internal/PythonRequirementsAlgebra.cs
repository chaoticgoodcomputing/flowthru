using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Aggregates <see cref="PythonPackageRequirement"/> declarations
/// from every contributing capability into a per-package effective
/// constraint with declarer attribution. The algebra's core operation
/// per ADR-0013 — the analyzer (slice 3) and pre-flight hook consume
/// the same folded shape.
/// </summary>
internal static class PythonRequirementsAlgebra
{
  /// <summary>
  /// Fold a sequence of declared requirements into per-package
  /// effective constraints. Multiple declarers asking for the same
  /// package collapse via constraint intersection; the result carries
  /// every <c>Reason</c> string so downstream error messages can name
  /// the responsible capabilities.
  /// </summary>
  /// <param name="requirements">
  /// Flat list of all declared requirements across capabilities.
  /// Empty input yields an empty result; this is the identity.
  /// </param>
  /// <returns>
  /// One <see cref="EffectiveRequirement"/> per distinct package name,
  /// ordered by package name (case-insensitive) for deterministic
  /// diagnostic ordering. Requirements whose
  /// <see cref="PythonPackageRequirement.VersionConstraint"/> fails
  /// to parse are surfaced via <see cref="EffectiveRequirement.UnparseableSource"/>
  /// — the algebra does not silently drop them.
  /// </returns>
  public static ImmutableArray<EffectiveRequirement> Fold(
    IEnumerable<PythonPackageRequirement> requirements
  )
  {
    // Per-package accumulator: package-name (canonicalised lower)
    // → (canonical-cased name, folded constraint, list of (reason,
    //    raw-constraint-string-for-display)).
    var byPackage = new Dictionary<
      string,
      (string Name, PythonVersionConstraint Constraint, List<DeclarerNote> Declarers, string? UnparseableSource)
    >(StringComparer.OrdinalIgnoreCase);

    foreach (var req in requirements)
    {
      if (string.IsNullOrWhiteSpace(req.Package)) continue;
      var key = req.Package.Trim();
      var canonical = key.ToLowerInvariant();

      PythonVersionConstraint clauseConstraint;
      string? unparseable = null;
      if (string.IsNullOrWhiteSpace(req.VersionConstraint))
      {
        clauseConstraint = PythonVersionConstraint.Any;
      }
      else if (PythonVersionConstraint.TryParse(req.VersionConstraint, out var parsed))
      {
        clauseConstraint = parsed;
      }
      else
      {
        clauseConstraint = PythonVersionConstraint.Any;
        unparseable = req.VersionConstraint;
      }

      var declarerNote = new DeclarerNote(req.Reason, req.VersionConstraint);

      if (byPackage.TryGetValue(canonical, out var existing))
      {
        var folded = existing.Constraint.IntersectWith(clauseConstraint);
        existing.Declarers.Add(declarerNote);
        byPackage[canonical] = (
          existing.Name,
          folded,
          existing.Declarers,
          existing.UnparseableSource ?? unparseable
        );
      }
      else
      {
        byPackage[canonical] = (
          key,
          clauseConstraint,
          new List<DeclarerNote> { declarerNote },
          unparseable
        );
      }
    }

    return byPackage
      .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
      .Select(kvp => new EffectiveRequirement(
        Package: kvp.Value.Name,
        Constraint: kvp.Value.Constraint,
        Declarers: kvp.Value.Declarers.ToImmutableArray(),
        UnparseableSource: kvp.Value.UnparseableSource
      ))
      .ToImmutableArray();
  }
}

/// <summary>
/// One package's folded requirement state — the constraint that
/// satisfies every declarer's clause, plus the list of declarer
/// reasons for attribution in diagnostics.
/// </summary>
internal sealed record EffectiveRequirement(
  string Package,
  PythonVersionConstraint Constraint,
  ImmutableArray<DeclarerNote> Declarers,
  string? UnparseableSource
);

/// <summary>
/// One declarer's contribution — the <see cref="PythonPackageRequirement.Reason"/>
/// they passed plus the raw constraint string they declared (kept for
/// diagnostic readability — the folded constraint may not exactly
/// match any single declarer's string).
/// </summary>
internal readonly record struct DeclarerNote(string Reason, string? RawConstraint)
{
  public override string ToString() =>
    string.IsNullOrWhiteSpace(RawConstraint)
      ? Reason
      : $"{Reason} (declared `{RawConstraint}`)";
}
