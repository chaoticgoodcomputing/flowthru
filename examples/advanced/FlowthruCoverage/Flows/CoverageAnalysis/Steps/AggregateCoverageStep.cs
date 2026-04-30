using Flowthru.Core.Steps;
using FlowthruCoverage.Data._02_Intermediate.Schemas;
using FlowthruCoverage.Data._03_Primary.Schemas;
#if FUNIT_ENABLED
using Flowthru.FUnit;
#endif

namespace FlowthruCoverage.Flows.Coverage.Steps;

/// <summary>
/// Aggregates line-level coverage rows into per-(TestProject, SrcPackage) totals.
/// The output is tidy-format: one row per combination, pivot-ready for a heatmap.
/// </summary>
[FlowthruStep]
public static class AggregateCoverageStep
{
  public static Func<
    IEnumerable<LineCoverageRow>,
    IEnumerable<PackageCoverageRow>
  > Create()
  {
    return rows =>
      rows
        .GroupBy(r => (r.TestProject, r.SrcPackage))
        .Select(g =>
        {
          var total = g.Count();
          var covered = g.Count(r => r.Hits > 0);
          var percent = total > 0 ? Math.Round(100.0 * covered / total, 2) : 0.0;

          return new PackageCoverageRow
          {
            TestProject = g.Key.TestProject,
            SrcPackage = g.Key.SrcPackage,
            CoveredLines = covered,
            TotalLines = total,
            CoveragePercent = percent,
          };
        })
        .OrderBy(r => r.SrcPackage)
        .ThenBy(r => r.TestProject);
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="AggregateCoverageStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static LineCoverageRow Row(string testProject, string srcPackage, int hits) =>
      new()
      {
        TestProject = testProject,
        SrcPackage = srcPackage,
        SourceFile = "src/Stub.cs",
        ClassName = "Stub",
        MethodName = "Stub",
        MethodSignature = "()",
        LineNumber = 1,
        Hits = hits,
      };

    /// <summary>
    /// All-uncovered group reports CoveragePercent=0 with CoveredLines=0 and TotalLines
    /// equal to the input row count.
    /// </summary>
    [StepTest(typeof(AggregateCoverageStep))]
    public void AllUncovered_ReportsZeroPercent()
    {
      var rows = new[] { Row("T", "P", 0), Row("T", "P", 0), Row("T", "P", 0) };

      var result = Invoke(AggregateCoverageStep.Create(), rows).Single();

      Assert.That(result.CoveredLines, Is.EqualTo(0));
      Assert.That(result.TotalLines, Is.EqualTo(3));
      Assert.That(result.CoveragePercent, Is.EqualTo(0.0));
    }

    /// <summary>
    /// All-covered group reports 100% — any hit count above zero counts the line as covered,
    /// regardless of magnitude.
    /// </summary>
    [StepTest(typeof(AggregateCoverageStep))]
    public void AllCovered_ReportsHundredPercent()
    {
      var rows = new[] { Row("T", "P", 1), Row("T", "P", 100), Row("T", "P", 5) };

      var result = Invoke(AggregateCoverageStep.Create(), rows).Single();

      Assert.That(result.CoveredLines, Is.EqualTo(3));
      Assert.That(result.TotalLines, Is.EqualTo(3));
      Assert.That(result.CoveragePercent, Is.EqualTo(100.0));
    }

    /// <summary>
    /// CoveragePercent is rounded to 2 decimal places — verifies the
    /// <c>Math.Round(..., 2)</c> contract documented in <see cref="PackageCoverageRow.CoveragePercent"/>.
    /// </summary>
    [StepTest(typeof(AggregateCoverageStep))]
    public void PartialCoverage_RoundsToTwoDecimals()
    {
      var rows = new[]
      {
        Row("T", "P", 1),
        Row("T", "P", 0),
        Row("T", "P", 0),
      };

      var result = Invoke(AggregateCoverageStep.Create(), rows).Single();

      Assert.That(result.CoveragePercent, Is.EqualTo(33.33));
    }

    /// <summary>
    /// Distinct (TestProject, SrcPackage) tuples produce separate rows; grouping keys do not
    /// bleed across boundaries.
    /// </summary>
    [StepTest(typeof(AggregateCoverageStep))]
    public void DistinctGroups_ProduceSeparateRows()
    {
      var rows = new[]
      {
        Row("TestA", "PkgA", 1),
        Row("TestA", "PkgB", 0),
        Row("TestB", "PkgA", 1),
      };

      var result = Invoke(AggregateCoverageStep.Create(), rows).ToList();

      Assert.That(result, Has.Count.EqualTo(3));
      Assert.That(result.Select(r => (r.TestProject, r.SrcPackage)), Is.EquivalentTo(new[]
      {
        ("TestA", "PkgA"),
        ("TestA", "PkgB"),
        ("TestB", "PkgA"),
      }));
    }

    /// <summary>
    /// Output ordering is SrcPackage ascending, then TestProject ascending — important so the
    /// downstream pivot lands rows in deterministic order.
    /// </summary>
    [StepTest(typeof(AggregateCoverageStep))]
    public void Output_IsSortedBySrcPackageThenTestProject()
    {
      var rows = new[]
      {
        Row("TestB", "PkgZ", 1),
        Row("TestA", "PkgA", 1),
        Row("TestB", "PkgA", 1),
      };

      var result = Invoke(AggregateCoverageStep.Create(), rows).ToList();

      Assert.That(result.Select(r => (r.SrcPackage, r.TestProject)), Is.EqualTo(new[]
      {
        ("PkgA", "TestA"),
        ("PkgA", "TestB"),
        ("PkgZ", "TestB"),
      }));
    }
  }
#endif
}
