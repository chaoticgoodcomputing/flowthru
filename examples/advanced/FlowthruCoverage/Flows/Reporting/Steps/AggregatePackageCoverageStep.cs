using Flowthru.Step;
using FlowthruCoverage.Data._04_Reporting.Schemas;
#if FUNIT_ENABLED
using Flowthru.Step.Testing;
#endif

namespace FlowthruCoverage.Flows.Reporting.Steps;

/// <summary>
/// Rolls up per-(TestProject, SrcPackage) pivot rows to per-SrcPackage rows by taking
/// the maximum coverage across test projects.
/// </summary>
/// <remarks>
/// <para>
/// The detail-level <see cref="PivotCoverageRow"/> emits one row per (TestProject,
/// SrcPackage) pair. A package tested by multiple projects appears multiple times.
/// The <see cref="PivotCoverageRow.IsGhost"/> rows (CoveragePercent = -1) represent
/// missing pairings and are excluded from the aggregation entirely; surfacing them
/// would always lose to a real reading.
/// </para>
/// <para>
/// Output is sorted by SrcSubgroup → SrcPackage so the rolled-up CSV is human-scannable.
/// </para>
/// </remarks>
[FlowthruStep]
public static class AggregatePackageCoverageStep
{
  public static Func<
    IEnumerable<PivotCoverageRow>,
    IEnumerable<PackageCoverageMaxRow>
  > Create()
  {
    return rows =>
      rows.Where(r => !r.IsGhost)
        .GroupBy(r => r.SrcPackage, StringComparer.Ordinal)
        .Select(group =>
        {
          var best = group.OrderByDescending(r => r.CoveragePercent).First();
          return new PackageCoverageMaxRow
          {
            SrcPackage = best.SrcPackage,
            SrcSubgroup = best.SrcSubgroup,
            MaxCoveragePercent = best.CoveragePercent,
            BestTestProject = best.TestProject,
          };
        })
        .OrderBy(r => r.SrcSubgroup, StringComparer.Ordinal)
        .ThenBy(r => r.SrcPackage, StringComparer.Ordinal);
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="AggregatePackageCoverageStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static PivotCoverageRow Row(
      string testProject,
      string srcPackage,
      double percent,
      string subgroup = "Core",
      bool isGhost = false
    ) =>
      new()
      {
        Section = "Library Tests",
        Subgroup = subgroup,
        SrcSubgroup = subgroup,
        TestProject = testProject,
        SrcPackage = srcPackage,
        CoveragePercent = percent,
        IsGhost = isGhost,
      };

    [FUnitStepTest(typeof(AggregatePackageCoverageStep))]
    public void EmptyInput_YieldsEmptyOutput()
    {
      var result = Invoke(
        AggregatePackageCoverageStep.Create(),
        Enumerable.Empty<PivotCoverageRow>()
      );

      Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// A package tested by multiple projects collapses to a single row whose
    /// MaxCoveragePercent is the highest reading and BestTestProject names the
    /// project that produced it. This is the load-bearing case — `SourceGenerators`
    /// in `Core.Tests` reports 0% but in `SourceGenerators.Tests` reports 74.41%;
    /// without max-rollup the package looks uncovered.
    /// </summary>
    [FUnitStepTest(typeof(AggregatePackageCoverageStep))]
    public void MultipleTestProjects_PerPackage_Collapses_ToMax()
    {
      var rows = new[]
      {
        Row("Foo.Tests", "Foo", 0.0),
        Row("FooSpecial.Tests", "Foo", 74.41),
      };

      var result = Invoke(AggregatePackageCoverageStep.Create(), rows).ToList();

      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].SrcPackage, Is.EqualTo("Foo"));
      Assert.That(result[0].MaxCoveragePercent, Is.EqualTo(74.41));
      Assert.That(result[0].BestTestProject, Is.EqualTo("FooSpecial.Tests"));
    }

    [FUnitStepTest(typeof(AggregatePackageCoverageStep))]
    public void GhostRows_AreExcludedFromAggregation()
    {
      var rows = new[]
      {
        Row("Foo.Tests", "Foo", 50.0),
        Row("Foo.Tests", "Foo", -1, isGhost: true),
      };

      var result = Invoke(AggregatePackageCoverageStep.Create(), rows).ToList();

      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].MaxCoveragePercent, Is.EqualTo(50.0));
    }

    [FUnitStepTest(typeof(AggregatePackageCoverageStep))]
    public void Output_IsSortedBySubgroupThenPackage()
    {
      var rows = new[]
      {
        Row("Z.Tests", "Z", 30.0, subgroup: "Extensions"),
        Row("A.Tests", "A", 40.0, subgroup: "Core"),
        Row("M.Tests", "M", 50.0, subgroup: "Core"),
      };

      var result = Invoke(AggregatePackageCoverageStep.Create(), rows).Select(r => r.SrcPackage).ToList();

      Assert.That(result, Is.EqualTo(new[] { "A", "M", "Z" }));
    }
  }
#endif
}
