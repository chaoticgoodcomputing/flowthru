using Flowthru.Core.Steps;
using FlowthruCoverage.Data._01_Raw.Schemas;
using FlowthruCoverage.Data._03_Primary.Schemas;
using FlowthruCoverage.Data._04_Reporting.Schemas;
#if FUNIT_ENABLED
using Flowthru.FUnit;
#endif

namespace FlowthruCoverage.Flows.Reporting.Steps;

/// <summary>
/// Annotates each <see cref="PackageCoverageRow"/> with its heatmap section, test-column subgroup,
/// and source-package subgroup — all driven by the project manifest rather than name heuristics.
/// Rows whose <c>SrcPackage</c> is not a manifest <c>Library</c> entry are excluded entirely.
/// Returns rows sorted in section → subgroup → package-name order.
/// </summary>
[FlowthruStep]
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

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="ClassifyCoverageStep"/>.</summary>
  public class Tests : FunitContext
  {
    private static ProjectManifestEntry Manifest(string assemblyName, string projectType, string subgroup) =>
      new()
      {
        AssemblyName = assemblyName,
        ProjectType = projectType,
        Subgroup = subgroup,
      };

    private static PackageCoverageRow Row(string testProject, string srcPackage, double percent) =>
      new()
      {
        TestProject = testProject,
        SrcPackage = srcPackage,
        CoveredLines = 1,
        TotalLines = 1,
        CoveragePercent = percent,
      };

    /// <summary>
    /// Rows whose SrcPackage is not a manifest <c>Library</c> entry are excluded from the real
    /// rows entirely — they don't fit the Library × LibraryTest grid the heatmap displays.
    /// </summary>
    [StepTest(typeof(ClassifyCoverageStep))]
    public void NonLibrarySrcPackage_IsExcluded()
    {
      var rows = new[]
      {
        Row("Pkg.Tests", "Pkg", 50.0),
        Row("Pkg.Tests", "NotALibrary", 50.0),
      };
      var manifest = new[]
      {
        Manifest("Pkg", "Library", "Core"),
        Manifest("Pkg.Tests", "LibraryTest", "Core"),
      };

      var result = Invoke(ClassifyCoverageStep.Create(), (rows, manifest)).ToList();

      var realRows = result.Where(r => !r.IsGhost).ToList();
      Assert.That(realRows, Has.Count.EqualTo(1));
      Assert.That(realRows[0].SrcPackage, Is.EqualTo("Pkg"));
    }

    /// <summary>
    /// A test project that ran but hit nothing is demoted to ghost rows so the X column
    /// is grayed out — confirms the all-zero demotion documented in the implementation.
    /// </summary>
    [StepTest(typeof(ClassifyCoverageStep))]
    public void AllZeroTestProject_IsDemotedToGhost()
    {
      var rows = new[]
      {
        Row("Pkg.Tests", "Pkg", 0.0),
      };
      var manifest = new[]
      {
        Manifest("Pkg", "Library", "Core"),
        Manifest("Pkg.Tests", "LibraryTest", "Core"),
      };

      var result = Invoke(ClassifyCoverageStep.Create(), (rows, manifest)).ToList();

      Assert.That(result.Where(r => !r.IsGhost), Is.Empty);
      Assert.That(result.Any(r => r.IsGhost && r.TestProject == "Pkg.Tests"), Is.True);
    }

    /// <summary>
    /// Section is derived from the test project's manifest entry: <c>LibraryTest</c> →
    /// "Library Tests", <c>IntegrationTest</c> → "Integration Tests", anything else →
    /// "Examples". LibraryTest entries must follow the <c>Foo.Tests</c> naming convention —
    /// the step assumes this when computing pair anchors.
    /// </summary>
    [StepTest(typeof(ClassifyCoverageStep))]
    public void Section_IsDerivedFromTestProjectType()
    {
      var rows = new[]
      {
        Row("Pkg.Tests", "Pkg", 50.0),
        Row("IntT", "Pkg", 60.0),
        Row("Ex", "Pkg", 70.0),
      };
      var manifest = new[]
      {
        Manifest("Pkg", "Library", "Core"),
        Manifest("Pkg.Tests", "LibraryTest", "Core"),
        Manifest("IntT", "IntegrationTest", ""),
        Manifest("Ex", "Example", ""),
      };

      var result = Invoke(ClassifyCoverageStep.Create(), (rows, manifest))
        .Where(r => !r.IsGhost)
        .ToDictionary(r => r.TestProject, r => r.Section);

      Assert.That(result["Pkg.Tests"], Is.EqualTo("Library Tests"));
      Assert.That(result["IntT"], Is.EqualTo("Integration Tests"));
      Assert.That(result["Ex"], Is.EqualTo("Examples"));
    }

    /// <summary>
    /// A library with no Cobertura data still appears as a ghost Y-row anchor — the heatmap
    /// needs to know the package exists in the manifest even when nothing measured it.
    /// </summary>
    [StepTest(typeof(ClassifyCoverageStep))]
    public void LibraryWithNoCoverageData_GetsGhostAnchor()
    {
      var rows = new[] { Row("PkgA.Tests", "PkgA", 50.0) };
      var manifest = new[]
      {
        Manifest("PkgA", "Library", "Core"),
        Manifest("PkgA.Tests", "LibraryTest", "Core"),
        Manifest("PkgB", "Library", "Core"),
        Manifest("PkgB.Tests", "LibraryTest", "Core"),
      };

      var result = Invoke(ClassifyCoverageStep.Create(), (rows, manifest)).ToList();

      Assert.That(result.Any(r => r.IsGhost && r.SrcPackage == "PkgB"), Is.True);
    }

    /// <summary>
    /// Output is sorted by Section → Subgroup → TestProject (with .Tests suffix stripped) →
    /// SrcPackage. Verifies the deterministic ordering the downstream pivot relies on.
    /// </summary>
    [StepTest(typeof(ClassifyCoverageStep))]
    public void Output_IsSortedBySectionThenSubgroupThenProject()
    {
      var rows = new[]
      {
        Row("Ext.Tests", "Ext", 50.0),
        Row("Core.Tests", "Core", 50.0),
      };
      var manifest = new[]
      {
        Manifest("Core", "Library", "Core"),
        Manifest("Core.Tests", "LibraryTest", "Core"),
        Manifest("Ext", "Library", "Extensions"),
        Manifest("Ext.Tests", "LibraryTest", "Extensions"),
      };

      var realResult = Invoke(ClassifyCoverageStep.Create(), (rows, manifest))
        .Where(r => !r.IsGhost)
        .ToList();

      Assert.That(realResult.Select(r => r.Subgroup), Is.EqualTo(new[] { "Core", "Extensions" }));
    }
  }
#endif
}
