using FlowthruCoverage.Data._01_Raw.Schemas;
using FlowthruCoverage.Data._03_Primary.Schemas;
using FlowthruCoverage.Data._04_Reporting.Schemas;

namespace FlowthruCoverage.Flows.Reporting.Steps;

/// <summary>
/// Annotates each <see cref="PackageCoverageRow"/> with its heatmap section, test-column subgroup,
/// and source-package subgroup — all driven by the project manifest rather than name heuristics.
/// Rows whose <c>SrcPackage</c> is not a manifest <c>Library</c> entry are excluded entirely.
/// Returns rows sorted in section → subgroup → package-name order.
/// </summary>
public static class ClassifyCoverageStep
{
  public static Func<
    (IEnumerable<PackageCoverageRow>, IEnumerable<ProjectManifestEntry>),
    IEnumerable<PivotCoverageRow>
  > Create()
  {
    return inputs =>
    {
      var (rows, manifestEntries) = inputs;

      var manifest = manifestEntries.ToDictionary(e => e.AssemblyName, StringComparer.Ordinal);

      var libEntries = manifest
        .Values.Where(e => e.ProjectType == "Library")
        .ToDictionary(e => e.AssemblyName, StringComparer.Ordinal);

      var testEntries = manifest
        .Values.Where(e => e.ProjectType == "LibraryTest")
        .ToDictionary(e => e.AssemblyName, StringComparer.Ordinal);

      var libraryPackages = libEntries.Keys.ToHashSet(StringComparer.Ordinal);

      // ── Real rows ────────────────────────────────────────────────────────────
      var realRows = rows.Where(r => libraryPackages.Contains(r.SrcPackage))
        .Where(r => manifest.ContainsKey(r.TestProject))
        .Select(r =>
        {
          var testEntry = manifest[r.TestProject];
          var srcEntry = manifest[r.SrcPackage];
          return new PivotCoverageRow
          {
            Section = SectionFromProjectType(testEntry.ProjectType),
            Subgroup = testEntry.Subgroup,
            SrcSubgroup = srcEntry.Subgroup,
            TestProject = r.TestProject,
            SrcPackage = r.SrcPackage,
            CoveragePercent = r.CoveragePercent,
            IsGhost = false,
          };
        })
        .ToList();

      // A test project is only "real" if it covers at least one src package at > 0%.
      // All-zero test projects (ran but hit nothing) are demoted to ghosts so the
      // X-column is grayed out rather than the paired src Y-row.
      var coveringTestProjs = realRows
        .Where(r => r.CoveragePercent > 0)
        .Select(r => r.TestProject)
        .ToHashSet(StringComparer.Ordinal);

      realRows = realRows.Where(r => coveringTestProjs.Contains(r.TestProject)).ToList();

      var realSrcPkgs = realRows.Select(r => r.SrcPackage).ToHashSet(StringComparer.Ordinal);
      var realTestProjs = realRows.Select(r => r.TestProject).ToHashSet(StringComparer.Ordinal);

      // ── Ghost anchors ─────────────────────────────────────────────────────────
      // One anchor row per "missing" entity so Python knows the package/test exists
      // in the manifest even though it produced no Cobertura data (or has no pair).
      // Deduplicated by (TestProject, SrcPackage) key.
      var ghostAnchors = new Dictionary<(string, string), PivotCoverageRow>();

      void AddGhost(string testProj, string testSubgroup, string srcPkg, string srcSubgroup) =>
        ghostAnchors.TryAdd(
          (testProj, srcPkg),
          new PivotCoverageRow
          {
            Section = "Library Tests",
            Subgroup = testSubgroup,
            SrcSubgroup = srcSubgroup,
            TestProject = testProj,
            SrcPackage = srcPkg,
            CoveragePercent = -1,
            IsGhost = true,
          }
        );

      // Library src with no Cobertura data → ghost Y row
      foreach (var (name, entry) in libEntries.Where(kv => !realSrcPkgs.Contains(kv.Key)))
      {
        var testName = name + ".Tests";
        var testSubgroup = testEntries.TryGetValue(testName, out var te)
          ? te.Subgroup
          : entry.Subgroup;
        AddGhost(testName, testSubgroup, name, entry.Subgroup);
      }

      // LibraryTest with no Cobertura data → ghost X column
      foreach (var (name, entry) in testEntries.Where(kv => !realTestProjs.Contains(kv.Key)))
      {
        var baseName = name[..^".Tests".Length];
        var srcSubgroup = libEntries.TryGetValue(baseName, out var le)
          ? le.Subgroup
          : entry.Subgroup;
        AddGhost(name, entry.Subgroup, baseName, srcSubgroup);
      }

      // Library with no paired LibraryTest in manifest → synthetic ghost X column
      foreach (
        var (name, entry) in libEntries.Where(kv => !testEntries.ContainsKey(kv.Key + ".Tests"))
      )
        AddGhost(name + ".Tests", entry.Subgroup, name, entry.Subgroup);

      // LibraryTest with no paired Library in manifest → synthetic ghost Y row
      foreach (var (name, entry) in testEntries)
      {
        var baseName = name[..^".Tests".Length];
        if (!libEntries.ContainsKey(baseName))
          AddGhost(name, entry.Subgroup, baseName, entry.Subgroup);
      }

      return realRows
        .Concat(ghostAnchors.Values)
        .OrderBy(r => SectionOrder(r.Section))
        .ThenBy(r => r.Section == "Library Tests" ? SubgroupOrder(r.Subgroup) : 0)
        .ThenBy(
          r =>
            r.Section == "Library Tests"
              ? r.TestProject.EndsWith(".Tests", StringComparison.Ordinal)
                ? r.TestProject[..^".Tests".Length]
                : r.TestProject
              : r.TestProject,
          StringComparer.Ordinal
        )
        .ThenBy(r => r.SrcPackage, StringComparer.Ordinal);
    };
  }

  private static string SectionFromProjectType(string projectType) =>
    projectType switch
    {
      "LibraryTest" => "Library Tests",
      "IntegrationTest" => "Integration Tests",
      _ => "Examples",
    };

  private static int SectionOrder(string section) =>
    section switch
    {
      "Library Tests" => 0,
      "Integration Tests" => 1,
      _ => 2,
    };

  private static int SubgroupOrder(string subgroup) =>
    subgroup switch
    {
      "Core" => 0,
      "Extensions" => 1,
      _ => 2,
    };
}
