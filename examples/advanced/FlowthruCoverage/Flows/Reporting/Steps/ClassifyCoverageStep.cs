using FlowthruCoverage.Data._03_Primary.Schemas;
using FlowthruCoverage.Data._04_Reporting.Schemas;

namespace FlowthruCoverage.Flows.Reporting.Steps;

/// <summary>
/// Annotates each <see cref="PackageCoverageRow"/> with its heatmap section and
/// returns rows sorted in section order (Library Tests → Integration Tests → Examples),
/// then by TestProject, then by SrcPackage.
/// </summary>
public static class ClassifyCoverageStep
{
  public static Func<IEnumerable<PackageCoverageRow>, IEnumerable<PivotCoverageRow>> Create()
  {
    return rows =>
    {
      var rowList = rows.ToList();
      var srcPackages = rowList.Select(r => r.SrcPackage).ToHashSet(StringComparer.Ordinal);

      return rowList
        .Select(r => new PivotCoverageRow
        {
          Section = Classify(r.TestProject, srcPackages),
          TestProject = r.TestProject,
          SrcPackage = r.SrcPackage,
          CoveragePercent = r.CoveragePercent,
        })
        .OrderBy(r => SectionOrder(r.Section))
        .ThenBy(
          r =>
            r.Section == "Library Tests"
              ? r.TestProject[..^".Tests".Length] // sort by base package name to align with Y axis
              : r.TestProject,
          StringComparer.Ordinal
        )
        .ThenBy(r => r.SrcPackage, StringComparer.Ordinal);
    };
  }

  private static string Classify(string testProject, HashSet<string> srcPackages) =>
    !testProject.EndsWith(".Tests", StringComparison.Ordinal) ? "Examples"
    : srcPackages.Contains(testProject[..^".Tests".Length]) ? "Library Tests"
    : "Integration Tests";

  private static int SectionOrder(string section) =>
    section switch
    {
      "Library Tests" => 0,
      "Integration Tests" => 1,
      _ => 2,
    };
}
