using Flowthru.Core.Steps;
using FlowthruCoverage.Data._01_Raw.Schemas;
using FlowthruCoverage.Data._03_Primary.Schemas;
#if FUNIT_ENABLED
using Flowthru.FUnit;
#endif

namespace FlowthruCoverage.Flows.Coverage.Steps;

/// <summary>
/// Variant of <see cref="BuildMethodHitSummaryStep"/> where the row ID uses only the method
/// name — <c>{namespace}.{className}.{methodName}</c> — rather than the full signature.
///
/// Multiple overloads sharing the same name are collapsed into a single row by summing
/// <c>TotalHits</c> and taking the union of test projects (distinct projects that hit any
/// overload at all). Ordering is the same: subgroup (Core → Extensions → Misc) then
/// <c>TotalHits</c> ascending.
/// </summary>
[FlowthruStep]
public static class BuildMethodNameSummaryStep
{
  private static readonly Dictionary<string, int> SubgroupOrder =
    new(StringComparer.Ordinal)
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

      // Flatten to one tuple per (method, test-project) combination, carrying through the
      // method-level metadata (SourceFile, LineCount) needed to reassemble the collapsed row.
      return reports
        .SelectMany(pkg =>
        {
          var subgroup = subgroupByPackage.GetValueOrDefault(pkg.Package, string.Empty);

          return pkg.Namespaces.SelectMany(ns =>
            ns.Classes.SelectMany(cls =>
              cls.Methods.SelectMany(method =>
              {
                var nameId = BuildId(
                  ns.Namespace,
                  cls.ClassName,
                  MethodName(method.MethodSignature)
                );

                return method.TestProjects.Select(proj =>
                  (
                    nameId,
                    subgroup,
                    method.SourceFile,
                    method.LineCount,
                    method.MethodSignature,
                    proj.TestProjectName,
                    proj.TotalHits
                  )
                );
              })
            )
          );
        })
        .GroupBy(t => (t.nameId, t.subgroup))
        .Select(g =>
        {
          // SourceFile collapses across overloads — they all live in the same class file.
          // Take the first non-empty value to handle the rare case where Cobertura omits
          // filename for some overloads but not others.
          var sourceFile = g.Select(t => t.SourceFile).FirstOrDefault(s => !string.IsNullOrEmpty(s));

          // LineCount sums across collapsed overloads (Bar() + Bar(int) → combined size),
          // but only once per distinct overload signature so cross-test-project rows
          // don't inflate the count.
          var lineCount = g
            .GroupBy(t => t.MethodSignature)
            .Sum(sigGroup => sigGroup.First().LineCount);

          return new MethodHitSummaryRow
          {
            Id = g.Key.nameId,
            Subgroup = g.Key.subgroup,
            SourceFile = sourceFile ?? string.Empty,
            LineCount = lineCount,
            TotalHits = g.Sum(t => t.TotalHits),
            ProjectHits = g.GroupBy(t => t.TestProjectName)
              .Count(proj => proj.Sum(t => t.TotalHits) > 0),
          };
        })
        .OrderBy(r => SubgroupOrder.GetValueOrDefault(r.Subgroup, int.MaxValue))
        .ThenBy(r => r.TotalHits)
        .ThenBy(r => r.Id);
    };
  }

  private static string MethodName(string methodSignature)
  {
    var parenIdx = methodSignature.IndexOf('(');
    return parenIdx >= 0 ? methodSignature[..parenIdx] : methodSignature;
  }

  private static string BuildId(string ns, string? className, string methodName)
  {
    if (string.IsNullOrEmpty(ns) && string.IsNullOrEmpty(className))
      return methodName;

    if (string.IsNullOrEmpty(className))
      return $"{ns}.{methodName}";

    if (string.IsNullOrEmpty(ns))
      return $"{className}.{methodName}";

    return $"{ns}.{className}.{methodName}";
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="BuildMethodNameSummaryStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static ProjectManifestEntry Manifest(string assemblyName, string subgroup) =>
      new()
      {
        AssemblyName = assemblyName,
        ProjectType = "Library",
        Subgroup = subgroup,
      };

    private static MethodCoverage Method(string sig, params (string Project, int Hits)[] projects) =>
      Method(sig, sourceFile: "src/Stub.cs", lineCount: 1, projects);

    private static MethodCoverage Method(
      string sig,
      string? sourceFile,
      int lineCount,
      params (string Project, int Hits)[] projects
    ) =>
      new()
      {
        MethodSignature = sig,
        SourceFile = sourceFile,
        LineCount = lineCount,
        TotalHits = projects.Sum(p => p.Hits),
        TestProjects = projects
          .Select(p => new TestProjectHits { TestProjectName = p.Project, TotalHits = p.Hits })
          .ToList(),
      };

    private static PackageCoverageReport Report(
      string package,
      string ns,
      string? className,
      params MethodCoverage[] methods
    ) =>
      new()
      {
        Package = package,
        Namespaces = new()
        {
          new()
          {
            Namespace = ns,
            Classes = new() { new() { ClassName = className, Methods = methods.ToList() } },
          },
        },
      };

    /// <summary>
    /// Overloads share the same name but different signatures. After name-summarization, they
    /// collapse into one row with TotalHits summed across signatures.
    /// </summary>
    [StepTest(typeof(BuildMethodNameSummaryStep))]
    public void Overloads_CollapseIntoOneRowWithSummedHits()
    {
      var reports = new[]
      {
        Report("Pkg", "Ns", "Foo",
          Method("Bar()", ("T", 3)),
          Method("Bar(int)", ("T", 7))),
      };
      var manifest = new[] { Manifest("Pkg", "Core") };

      var result = Invoke(BuildMethodNameSummaryStep.Create(), (reports, manifest)).ToList();

      Assert.That(result, Has.Count.EqualTo(1));
      Assert.That(result[0].Id, Is.EqualTo("Ns.Foo.Bar"));
      Assert.That(result[0].TotalHits, Is.EqualTo(10));
    }

    /// <summary>
    /// ProjectHits unions across overloads — a project that hits any overload counts once.
    /// Verifies the documented "distinct projects covering ANY overload" semantic.
    /// </summary>
    [StepTest(typeof(BuildMethodNameSummaryStep))]
    public void ProjectHits_UnionsAcrossOverloads()
    {
      var reports = new[]
      {
        Report("Pkg", "Ns", "Foo",
          Method("Bar()", ("TestA", 1), ("TestB", 0)),
          Method("Bar(int)", ("TestA", 0), ("TestB", 1))),
      };
      var manifest = new[] { Manifest("Pkg", "Core") };

      var result = Invoke(BuildMethodNameSummaryStep.Create(), (reports, manifest)).Single();

      // Both projects hit at least one overload — union has 2 distinct hitting projects.
      Assert.That(result.ProjectHits, Is.EqualTo(2));
    }

    /// <summary>
    /// Methods sharing a name across DIFFERENT classes do NOT collapse — only same-class
    /// overloads collapse. Distinct (namespace, className, methodName) triples produce
    /// distinct rows.
    /// </summary>
    [StepTest(typeof(BuildMethodNameSummaryStep))]
    public void DifferentClasses_KeepSameNameMethodsSeparate()
    {
      var reports = new[]
      {
        Report("Pkg", "Ns", "FooA", Method("Bar()", ("T", 1))),
        Report("Pkg", "Ns", "FooB", Method("Bar()", ("T", 2))),
      };
      var manifest = new[] { Manifest("Pkg", "Core") };

      var result = Invoke(BuildMethodNameSummaryStep.Create(), (reports, manifest)).ToList();

      Assert.That(result, Has.Count.EqualTo(2));
      Assert.That(result.Select(r => r.Id), Is.EquivalentTo(new[] { "Ns.FooA.Bar", "Ns.FooB.Bar" }));
    }

    /// <summary>
    /// Same sort contract as <see cref="BuildMethodHitSummaryStep"/>: subgroup primary,
    /// TotalHits ascending secondary. The name-summarization step must preserve it.
    /// </summary>
    [StepTest(typeof(BuildMethodNameSummaryStep))]
    public void Output_IsSortedBySubgroupThenTotalHits()
    {
      var reports = new[]
      {
        Report("Misc", "Misc", "C", Method("M()", ("T", 1))),
        Report("Core", "Core", "C", Method("M()", ("T", 5))),
      };
      var manifest = new[]
      {
        Manifest("Core", "Core"),
        Manifest("Misc", "Misc"),
      };

      var result = Invoke(BuildMethodNameSummaryStep.Create(), (reports, manifest)).ToList();

      Assert.That(result.Select(r => r.Subgroup), Is.EqualTo(new[] { "Core", "Misc" }));
    }

    /// <summary>
    /// LineCount sums across collapsed overloads: <c>Bar()</c> at 5 lines + <c>Bar(int)</c>
    /// at 7 lines collapses into one row of 12 lines. Importantly, the sum is per distinct
    /// overload signature — multiple test-project entries for the same overload don't double-count.
    /// </summary>
    [StepTest(typeof(BuildMethodNameSummaryStep))]
    public void LineCount_SumsAcrossOverloadsButNotAcrossTestProjects()
    {
      var reports = new[]
      {
        Report("Pkg", "Ns", "Foo",
          Method("Bar()", sourceFile: "src/Foo.cs", lineCount: 5, ("TestA", 1), ("TestB", 0)),
          Method("Bar(int)", sourceFile: "src/Foo.cs", lineCount: 7, ("TestA", 0), ("TestB", 1))),
      };
      var manifest = new[] { Manifest("Pkg", "Core") };

      var result = Invoke(BuildMethodNameSummaryStep.Create(), (reports, manifest)).Single();

      Assert.That(result.LineCount, Is.EqualTo(12)); // 5 + 7, not 24 (× 2 test projects)
    }

    /// <summary>
    /// Overloads in the same class share a source file — the collapsed row carries that
    /// shared file path. Picks the first non-empty value to handle Cobertura entries that
    /// occasionally drop the filename attribute on individual overloads.
    /// </summary>
    [StepTest(typeof(BuildMethodNameSummaryStep))]
    public void SourceFile_ResolvesFromAnyContributingOverload()
    {
      var reports = new[]
      {
        Report("Pkg", "Ns", "Foo",
          Method("Bar()", sourceFile: null, lineCount: 1, ("T", 1)),
          Method("Bar(int)", sourceFile: "src/Foo.cs", lineCount: 1, ("T", 1))),
      };
      var manifest = new[] { Manifest("Pkg", "Core") };

      var result = Invoke(BuildMethodNameSummaryStep.Create(), (reports, manifest)).Single();

      Assert.That(result.SourceFile, Is.EqualTo("src/Foo.cs"));
    }
  }
#endif
}
