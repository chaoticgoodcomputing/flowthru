using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Unit tests for the PEP 440 subset (<see cref="PythonVersion"/>,
/// <see cref="PythonVersionConstraint"/>) plus the fold algebra
/// (<see cref="PythonRequirementsAlgebra"/>) per ADR-0013. All three
/// types are pure functions — tests cover parse, comparison,
/// satisfaction, intersection, and folding without any subprocess
/// or DI dependency.
/// </summary>
[TestFixture]
[Category("Python")]
public class PythonRequirementsAlgebraTests
{
  // ── PythonVersion.TryParse ──────────────────────────────────────────

  [TestCase("1", 1)]
  [TestCase("1.0", 1, 0)]
  [TestCase("1.2.3", 1, 2, 3)]
  [TestCase("0.30.1", 0, 30, 1)]
  [TestCase("v1.0", 1, 0)]
  [TestCase("V2.5", 2, 5)]
  public void TryParse_AcceptsDottedNumericVersions(string input, params int[] expectedRelease)
  {
    Assert.That(PythonVersion.TryParse(input, out var v), Is.True);
    Assert.That(v.Release, Is.EqualTo(expectedRelease));
    Assert.That(v.PreReleaseKind, Is.EqualTo(PythonPreReleaseKind.None));
  }

  // PythonPreReleaseKind is internal; pass as int and cast inside.
  [TestCase("1.0a1", (int)PythonPreReleaseKind.Alpha, 1)]
  [TestCase("1.0b2", (int)PythonPreReleaseKind.Beta, 2)]
  [TestCase("1.0rc3", (int)PythonPreReleaseKind.ReleaseCandidate, 3)]
  [TestCase("1.0alpha4", (int)PythonPreReleaseKind.Alpha, 4)]
  [TestCase("1.0beta5", (int)PythonPreReleaseKind.Beta, 5)]
  [TestCase("1.0.rc1", (int)PythonPreReleaseKind.ReleaseCandidate, 1)]
  [TestCase("1.0-a1", (int)PythonPreReleaseKind.Alpha, 1)]
  [TestCase("1.0c1", (int)PythonPreReleaseKind.ReleaseCandidate, 1)]  // PEP 440: bare 'c' aliases 'rc'
  public void TryParse_AcceptsPreReleaseSuffix(string input, int kindAsInt, int n)
  {
    var kind = (PythonPreReleaseKind)kindAsInt;
    Assert.That(PythonVersion.TryParse(input, out var v), Is.True);
    Assert.That(v.PreReleaseKind, Is.EqualTo(kind));
    Assert.That(v.PreReleaseNumber, Is.EqualTo(n));
  }

  [TestCase("")]
  [TestCase("   ")]
  [TestCase("not.a.version")]
  [TestCase(".5")]  // leading dot — no release segment before
  public void TryParse_RejectsMalformed(string input)
  {
    Assert.That(PythonVersion.TryParse(input, out _), Is.False);
  }

  [Test]
  public void TryParse_IsLenientAboutTrailingMetadata()
  {
    // pip list often reports versions with post/dev/local tails our
    // subset doesn't model. Be lenient — parse as the final release.
    Assert.That(PythonVersion.TryParse("1.5.post1", out var v), Is.True);
    Assert.That(v.Release, Is.EqualTo(new[] { 1, 5 }));
  }

  // ── PythonVersion.CompareTo ─────────────────────────────────────────

  [Test]
  public void Compare_OrdersByReleaseSegmentsLexicographically()
  {
    var v1_0 = Parse("1.0");
    var v1_5 = Parse("1.5");
    var v2_0 = Parse("2.0");
    Assert.That(v1_0.CompareTo(v1_5), Is.LessThan(0));
    Assert.That(v1_5.CompareTo(v2_0), Is.LessThan(0));
    Assert.That(v2_0.CompareTo(v1_0), Is.GreaterThan(0));
  }

  [Test]
  public void Compare_PreReleaseSortsBeforeFinal()
  {
    var rc = Parse("1.0rc1");
    var final = Parse("1.0");
    Assert.That(rc.CompareTo(final), Is.LessThan(0), "1.0rc1 < 1.0");
  }

  [Test]
  public void Compare_PreReleaseKindOrdering()
  {
    Assert.That(Parse("1.0a1").CompareTo(Parse("1.0b1")), Is.LessThan(0));
    Assert.That(Parse("1.0b1").CompareTo(Parse("1.0rc1")), Is.LessThan(0));
    Assert.That(Parse("1.0rc1").CompareTo(Parse("1.0")), Is.LessThan(0));
  }

  [Test]
  public void Compare_TreatsShortAndLongAsEqualOnZeroExtension()
  {
    // 1.0 == 1.0.0 — PEP 440 treats missing trailing segments as 0.
    Assert.That(Parse("1.0").CompareTo(Parse("1.0.0")), Is.EqualTo(0));
  }

  // ── PythonVersionConstraint.TryParse + Satisfies ────────────────────

  [TestCase(">=1.0", "1.0", true)]
  [TestCase(">=1.0", "0.9", false)]
  [TestCase("==1.0", "1.0", true)]
  [TestCase("==1.0", "1.0.1", false)]
  [TestCase("!=1.5", "1.5", false)]
  [TestCase("!=1.5", "1.6", true)]
  [TestCase(">1.0", "1.0", false)]
  [TestCase(">1.0", "1.0.1", true)]
  [TestCase("<2.0", "1.9", true)]
  [TestCase("<2.0", "2.0", false)]
  [TestCase("<=2.0", "2.0", true)]
  public void Constraint_SingleClauseSatisfaction(string clause, string installed, bool expected)
  {
    Assert.That(PythonVersionConstraint.TryParse(clause, out var c), Is.True);
    Assert.That(c.Satisfies(Parse(installed)), Is.EqualTo(expected));
  }

  [TestCase(">=1.0,<2.0", "1.5", true)]
  [TestCase(">=1.0,<2.0", "2.0", false)]
  [TestCase(">=1.0,<2.0", "0.9", false)]
  [TestCase(">=1.0,!=1.5,<2.0", "1.5", false)]
  [TestCase(">=1.0,!=1.5,<2.0", "1.6", true)]
  public void Constraint_CompoundSatisfaction(string clauses, string installed, bool expected)
  {
    Assert.That(PythonVersionConstraint.TryParse(clauses, out var c), Is.True);
    Assert.That(c.Satisfies(Parse(installed)), Is.EqualTo(expected));
  }

  [TestCase("~=1.4", "1.4", true)]
  [TestCase("~=1.4", "1.5", true)]
  [TestCase("~=1.4", "2.0", false)]
  [TestCase("~=1.4.5", "1.4.5", true)]
  [TestCase("~=1.4.5", "1.4.9", true)]
  [TestCase("~=1.4.5", "1.5.0", false)]
  [TestCase("~=1.4.5", "1.4.4", false)]
  public void Constraint_CompatibleRelease(string clause, string installed, bool expected)
  {
    Assert.That(PythonVersionConstraint.TryParse(clause, out var c), Is.True);
    Assert.That(c.Satisfies(Parse(installed)), Is.EqualTo(expected),
      $"{clause} vs {installed}");
  }

  [Test]
  public void Constraint_EmptyIsIdentity()
  {
    Assert.That(PythonVersionConstraint.TryParse("", out var any), Is.True);
    Assert.That(any.Satisfies(Parse("0.0.1")), Is.True);
    Assert.That(any.Satisfies(Parse("99.99.99")), Is.True);
  }

  [Test]
  public void Constraint_BareVersionDefaultsToEqual()
  {
    // pip's permissive convention — "1.0" by itself means "==1.0".
    Assert.That(PythonVersionConstraint.TryParse("1.0", out var c), Is.True);
    Assert.That(c.Satisfies(Parse("1.0")), Is.True);
    Assert.That(c.Satisfies(Parse("1.1")), Is.False);
  }

  [Test]
  public void Constraint_IntersectWith_CombinesClauses()
  {
    PythonVersionConstraint.TryParse(">=1.0", out var lo);
    PythonVersionConstraint.TryParse("<2.0", out var hi);
    var both = lo.IntersectWith(hi);
    Assert.That(both.Satisfies(Parse("1.5")), Is.True);
    Assert.That(both.Satisfies(Parse("0.5")), Is.False);
    Assert.That(both.Satisfies(Parse("2.0")), Is.False);
  }

  [Test]
  public void Constraint_IntersectWith_UnsatisfiableHasNoSolutions()
  {
    // No symbolic detection in slice 2 — but the resulting compound
    // constraint must refuse every test version.
    PythonVersionConstraint.TryParse(">=2.0", out var lo);
    PythonVersionConstraint.TryParse("<1.0", out var hi);
    var bad = lo.IntersectWith(hi);
    foreach (var v in new[] { "0.5", "1.0", "1.5", "2.0", "2.5" })
    {
      Assert.That(bad.Satisfies(Parse(v)), Is.False, $"unsatisfiable should reject {v}");
    }
  }

  // ── PythonRequirementsAlgebra.Fold ──────────────────────────────────

  [Test]
  public void Fold_EmptyInputIsIdentity()
  {
    var result = PythonRequirementsAlgebra.Fold(Array.Empty<PythonPackageRequirement>());
    Assert.That(result, Is.Empty);
  }

  [Test]
  public void Fold_GroupsByPackageNameCaseInsensitively()
  {
    var input = new[]
    {
      new PythonPackageRequirement("PyArrow", ">=14", "A"),
      new PythonPackageRequirement("pyarrow", ">=15", "B"),
      new PythonPackageRequirement("PYARROW", null, "C"),
    };

    var result = PythonRequirementsAlgebra.Fold(input);
    Assert.That(result, Has.Length.EqualTo(1));
    var entry = result[0];
    Assert.That(entry.Declarers, Has.Length.EqualTo(3));
  }

  [Test]
  public void Fold_IntersectsConstraintsAcrossDeclarers()
  {
    var input = new[]
    {
      new PythonPackageRequirement("pyarrow", ">=14", "A"),
      new PythonPackageRequirement("pyarrow", "<16", "B"),
    };

    var result = PythonRequirementsAlgebra.Fold(input);
    Assert.That(result, Has.Length.EqualTo(1));
    var folded = result[0].Constraint;

    Assert.That(folded.Satisfies(Parse("14.0")), Is.True);
    Assert.That(folded.Satisfies(Parse("15.5")), Is.True);
    Assert.That(folded.Satisfies(Parse("16.0")), Is.False);
    Assert.That(folded.Satisfies(Parse("13.0")), Is.False);
  }

  [Test]
  public void Fold_OrdersResultsByPackageNameDeterministically()
  {
    var input = new[]
    {
      new PythonPackageRequirement("zlib", null, "Z"),
      new PythonPackageRequirement("accelerate", ">=0.30", "A"),
      new PythonPackageRequirement("pyarrow", ">=14", "P"),
    };

    var result = PythonRequirementsAlgebra.Fold(input).Select(r => r.Package).ToArray();
    Assert.That(result, Is.EqualTo(new[] { "accelerate", "pyarrow", "zlib" }));
  }

  [Test]
  public void Fold_PreservesUnparseableSourceForDiagnostics()
  {
    var input = new[]
    {
      new PythonPackageRequirement("badpkg", "not-a-version-spec", "X"),
    };

    var result = PythonRequirementsAlgebra.Fold(input);
    Assert.That(result, Has.Length.EqualTo(1));
    Assert.That(result[0].UnparseableSource, Is.EqualTo("not-a-version-spec"));
  }

  [Test]
  public void Fold_SkipsEntriesWithBlankPackageName()
  {
    var input = new[]
    {
      new PythonPackageRequirement("", ">=1", "X"),
      new PythonPackageRequirement("  ", null, "Y"),
      new PythonPackageRequirement("real", ">=1", "Z"),
    };

    var result = PythonRequirementsAlgebra.Fold(input);
    Assert.That(result, Has.Length.EqualTo(1));
    Assert.That(result[0].Package, Is.EqualTo("real"));
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static PythonVersion Parse(string s)
  {
    Assert.That(PythonVersion.TryParse(s, out var v), Is.True, $"failed to parse '{s}'");
    return v;
  }
}
