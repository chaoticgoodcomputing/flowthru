using Flowthru.Core.Steps;
using FlowthruCoverage.Data._01_Raw.Schemas;
using FlowthruCoverage.Data._03_Primary.Schemas;
#if FUNIT_ENABLED
using Flowthru.FUnit;
#endif

namespace FlowthruCoverage.Flows.Coverage.Steps;

/// <summary>
/// Flattens the nested <see cref="PackageCoverageReport"/> hierarchy into one
/// <see cref="MethodHitSummaryRow"/> per method. The manifest is joined to supply
/// each package's <c>Subgroup</c> (Core / Extensions / Misc).
/// Rows are ordered by subgroup (Core → Extensions → Misc) then <c>TotalHits</c> ascending.
/// </summary>
[FlowthruStep]
public static class BuildMethodHitSummaryStep
{
  private static readonly Dictionary<string, int> SubgroupOrder = new(StringComparer.Ordinal)
  {
    ["Core"] = 0,
    ["Extensions"] = 1,
    ["Misc"] = 2,
  };

  public static Func<
    (IEnumerable<PackageCoverageReport>, IEnumerable<ProjectManifestEntry>),
    IEnumerable<MethodHitSummaryRow>
  > Create()
  {
    return inputs =>
    {
      var (reports, manifestEntries) = inputs;

      var subgroupByPackage = manifestEntries
        .Where(e => e.ProjectType == "Library")
        .ToDictionary(e => e.AssemblyName, e => e.Subgroup, StringComparer.Ordinal);

      return reports
        .SelectMany(pkg =>
        {
          var subgroup = subgroupByPackage.GetValueOrDefault(pkg.Package, string.Empty);

          return pkg.Namespaces.SelectMany(ns =>
            ns.Classes.SelectMany(cls =>
              cls.Methods.Select(method =>
              {
                var id = BuildId(ns.Namespace, cls.ClassName, method.MethodSignature);

                return new MethodHitSummaryRow
                {
                  Id = id,
                  Subgroup = subgroup,
                  SourceFile = method.SourceFile ?? string.Empty,
                  LineCount = method.LineCount,
                  TotalHits = method.TotalHits,
                  ProjectHits = method.TestProjects.Count(p => p.TotalHits > 0),
                };
              })
            )
          );
        })
        .OrderBy(r => SubgroupOrder.GetValueOrDefault(r.Subgroup, int.MaxValue))
        .ThenBy(r => r.TotalHits)
        .ThenBy(r => r.Id);
    };
  }

  private static string BuildId(string ns, string? className, string methodSignature)
  {
    if (string.IsNullOrEmpty(ns) && string.IsNullOrEmpty(className))
      return methodSignature;

    if (string.IsNullOrEmpty(className))
      return $"{ns}.{methodSignature}";

    if (string.IsNullOrEmpty(ns))
      return $"{className}.{methodSignature}";

    return $"{ns}.{className}.{methodSignature}";
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="BuildMethodHitSummaryStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static ProjectManifestEntry Manifest(string assemblyName, string projectType, string subgroup) =>
      new()
      {
        AssemblyName = assemblyName,
        ProjectType = projectType,
        Subgroup = subgroup,
      };

    private static PackageCoverageReport Report(
      string package,
      string ns,
      string? className,
      string methodSig,
      params (string Project, int Hits)[] testProjects
    ) => Report(package, ns, className, methodSig, sourceFile: "src/Stub.cs", lineCount: 1, testProjects);

    private static PackageCoverageReport Report(
      string package,
      string ns,
      string? className,
      string methodSig,
      string? sourceFile,
      int lineCount,
      params (string Project, int Hits)[] testProjects
    ) =>
      new()
      {
        Package = package,
        Namespaces = new()
        {
          new()
          {
            Namespace = ns,
            Classes = new()
            {
              new()
              {
                ClassName = className,
                Methods = new()
                {
                  new()
                  {
                    MethodSignature = methodSig,
                    SourceFile = sourceFile,
                    LineCount = lineCount,
                    TotalHits = testProjects.Sum(p => p.Hits),
                    TestProjects = testProjects
                      .Select(p => new TestProjectHits
                      {
                        TestProjectName = p.Project,
                        TotalHits = p.Hits,
                      })
                      .ToList(),
                  },
                },
              },
            },
          },
        },
      };

    /// <summary>
    /// Subgroup is looked up via the manifest, restricted to <c>Library</c> entries —
    /// non-Library packages get an empty-string subgroup (then sorted to the bottom).
    /// </summary>
    [StepTest(typeof(BuildMethodHitSummaryStep))]
    public void Subgroup_IsResolvedFromLibraryManifestEntries()
    {
      var reports = new[] { Report("Flowthru.Core", "Flowthru.Core", "Foo", "Bar()", ("T", 1)) };
      var manifest = new[]
      {
        Manifest("Flowthru.Core", "Library", "Core"),
        Manifest("Flowthru.Core.Tests", "LibraryTest", "Core"),
      };

      var result = Invoke(BuildMethodHitSummaryStep.Create(), (reports, manifest)).Single();

      Assert.That(result.Subgroup, Is.EqualTo("Core"));
    }

    /// <summary>
    /// ProjectHits is the count of distinct test projects whose hits exceed zero — not the
    /// total number of test projects. Zero-hit projects don't count.
    /// </summary>
    [StepTest(typeof(BuildMethodHitSummaryStep))]
    public void ProjectHits_CountsOnlyProjectsWithNonZeroHits()
    {
      var reports = new[] { Report("Pkg", "Ns", "Cls", "M()", ("TestA", 5), ("TestB", 0), ("TestC", 1)) };
      var manifest = new[] { Manifest("Pkg", "Library", "Core") };

      var result = Invoke(BuildMethodHitSummaryStep.Create(), (reports, manifest)).Single();

      Assert.That(result.ProjectHits, Is.EqualTo(2));
    }

    /// <summary>
    /// Sort order: subgroup (Core → Extensions → Misc) primary, TotalHits ascending secondary,
    /// then Id alphabetical for stable ties.
    /// </summary>
    [StepTest(typeof(BuildMethodHitSummaryStep))]
    public void Output_IsSortedBySubgroupThenTotalHitsAscending()
    {
      var reports = new[]
      {
        Report("Misc", "Misc", "C1", "M1()", ("T", 1)),
        Report("Ext", "Ext", "C2", "M2()", ("T", 5)),
        Report("Core", "Core", "C3", "M3()", ("T", 100)),
        Report("Core", "Core", "C4", "M4()", ("T", 0)),
      };
      var manifest = new[]
      {
        Manifest("Core", "Library", "Core"),
        Manifest("Ext", "Library", "Extensions"),
        Manifest("Misc", "Library", "Misc"),
      };

      var result = Invoke(BuildMethodHitSummaryStep.Create(), (reports, manifest)).ToList();

      Assert.That(result.Select(r => r.Subgroup), Is.EqualTo(new[] { "Core", "Core", "Extensions", "Misc" }));
      Assert.That(result.Select(r => r.TotalHits), Is.EqualTo(new[] { 0, 100, 5, 1 }));
    }

    /// <summary>
    /// Id concatenates namespace, className, and methodSignature with dots — the segment is
    /// omitted when empty. Verifies the four documented cases of <c>BuildId</c>.
    /// </summary>
    [StepTest(typeof(BuildMethodHitSummaryStep))]
    public void Id_IsBuiltFromNamespaceClassNameAndMethodSignature()
    {
      var reports = new[] { Report("Pkg", "Pkg.Ns", "Foo", "Bar()", ("T", 1)) };
      var manifest = new[] { Manifest("Pkg", "Library", "Core") };

      var result = Invoke(BuildMethodHitSummaryStep.Create(), (reports, manifest)).Single();

      Assert.That(result.Id, Is.EqualTo("Pkg.Ns.Foo.Bar()"));
    }

    /// <summary>
    /// SourceFile and LineCount pass through from the upstream MethodCoverage row — these
    /// are the click-to-source and prioritization signals coverage triage relies on.
    /// </summary>
    [StepTest(typeof(BuildMethodHitSummaryStep))]
    public void SourceFileAndLineCount_PassThroughFromMethodCoverage()
    {
      var reports = new[]
      {
        Report("Pkg", "Pkg.Ns", "Foo", "Bar()", sourceFile: "src/Pkg/Foo.cs", lineCount: 42, ("T", 1)),
      };
      var manifest = new[] { Manifest("Pkg", "Library", "Core") };

      var result = Invoke(BuildMethodHitSummaryStep.Create(), (reports, manifest)).Single();

      Assert.That(result.SourceFile, Is.EqualTo("src/Pkg/Foo.cs"));
      Assert.That(result.LineCount, Is.EqualTo(42));
    }

    /// <summary>
    /// A null upstream SourceFile collapses to empty string in the output row — CSV serializers
    /// don't have a clean "null" representation, and downstream consumers can treat empty as
    /// "missing" without ambiguity.
    /// </summary>
    [StepTest(typeof(BuildMethodHitSummaryStep))]
    public void NullSourceFile_CollapsesToEmptyString()
    {
      var reports = new[]
      {
        Report("Pkg", "Pkg.Ns", "Foo", "Bar()", sourceFile: null, lineCount: 1, ("T", 1)),
      };
      var manifest = new[] { Manifest("Pkg", "Library", "Core") };

      var result = Invoke(BuildMethodHitSummaryStep.Create(), (reports, manifest)).Single();

      Assert.That(result.SourceFile, Is.EqualTo(string.Empty));
    }
  }
#endif
}
