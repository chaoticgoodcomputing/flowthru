using Flowthru.Step;
using FlowthruCoverage.Data._02_Intermediate.Schemas;
using FlowthruCoverage.Data._03_Primary.Schemas;
#if FUNIT_ENABLED
using Flowthru.Step.Testing;
#endif

namespace FlowthruCoverage.Flows.Coverage.Steps;

/// <summary>
/// Builds a nested package coverage report from flat line-level rows.
///
/// Groups <see cref="LineCoverageRow"/> records into a four-level hierarchy:
/// package → namespace → class → method, with per-test-project hit counts at the leaf.
///
/// <c>TotalHits</c> for each (method, test project) pair is the hit count of the method's
/// earliest instrumented line (minimum <c>LineNumber</c>), which Cobertura records as the
/// entry-point hit count. Summing all lines would overcount loop iterations and branches.
///
/// Output is one <see cref="PackageCoverageReport"/> per distinct <c>SrcPackage</c>.
/// </summary>
[FlowthruStep]
public static class BuildMethodCoverageStep
{
  public static Func<IEnumerable<LineCoverageRow>, IEnumerable<PackageCoverageReport>> Create()
  {
    return rows =>
      rows.GroupBy(r => r.SrcPackage)
        .OrderBy(g => g.Key)
        .Select(pkgGroup => new PackageCoverageReport
        {
          Package = pkgGroup.Key,
          Namespaces = pkgGroup
            .GroupBy(r => r.ClassName)
            .Select(classGroup =>
            {
              var (ns, shortClass) = SplitClassName(classGroup.Key);
              return (Namespace: ns, ShortClass: shortClass, Rows: classGroup);
            })
            .GroupBy(t => t.Namespace)
            .OrderBy(g => g.Key)
            .Select(nsGroup => new NamespaceCoverage
            {
              Namespace = nsGroup.Key,
              Classes = nsGroup
                .GroupBy(t => t.ShortClass)
                .OrderBy(g => g.Key)
                .Select(clsGroup => new ClassCoverage
                {
                  ClassName = string.IsNullOrEmpty(clsGroup.Key) ? null : clsGroup.Key,
                  Methods = clsGroup
                    .SelectMany(t => t.Rows)
                    .GroupBy(r => (r.MethodName, r.MethodSignature))
                    .OrderBy(g => g.Key.MethodName)
                    .Select(methodGroup =>
                    {
                      var byProject = methodGroup
                        .GroupBy(r => r.TestProject)
                        .OrderBy(g => g.Key)
                        .Select(projGroup => new TestProjectHits
                        {
                          TestProjectName = projGroup.Key,
                          TotalHits = projGroup
                            .OrderBy(r => r.LineNumber)
                            .First()
                            .Hits,
                        })
                        .ToList();

                      // Distinct line numbers — Cobertura emits one <line> per instrumented
                      // line per test project, so deduplicating gives the true method size.
                      var distinctLines = methodGroup
                        .Select(r => r.LineNumber)
                        .Distinct()
                        .Count();

                      // SourceFile is uniform within a (className, methodName, signature)
                      // group — every contributing line came from the same Cobertura
                      // <class filename="..."> entry — so first-occurrence is sufficient.
                      var sourceFile = methodGroup.First().SourceFile;

                      return new MethodCoverage
                      {
                        MethodSignature =
                          methodGroup.Key.MethodName + methodGroup.Key.MethodSignature,
                        SourceFile = string.IsNullOrEmpty(sourceFile) ? null : sourceFile,
                        LineCount = distinctLines,
                        TotalHits = byProject.Sum(p => p.TotalHits),
                        TestProjects = byProject,
                      };
                    })
                    .ToList(),
                })
                .ToList(),
            })
            .ToList(),
        });
  }

  /// <summary>
  /// Splits a Cobertura fully-qualified class name into (namespace, short class name).
  /// "Flowthru.Extensions.EFCore.Bulk.Internal.BulkConfigMapper"
  ///   → ("Flowthru.Extensions.EFCore.Bulk.Internal", "BulkConfigMapper")
  /// </summary>
  private static (string Namespace, string ShortClassName) SplitClassName(string fullyQualified)
  {
    var lastDot = fullyQualified.LastIndexOf('.');
    return lastDot >= 0
      ? (fullyQualified[..lastDot], fullyQualified[(lastDot + 1)..])
      : (string.Empty, fullyQualified);
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="BuildMethodCoverageStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static LineCoverageRow Row(
      string testProject,
      string srcPackage,
      string className,
      string methodName,
      string signature,
      int lineNumber,
      int hits,
      string sourceFile = "src/Stub.cs"
    ) =>
      new()
      {
        TestProject = testProject,
        SrcPackage = srcPackage,
        SourceFile = sourceFile,
        ClassName = className,
        MethodName = methodName,
        MethodSignature = signature,
        LineNumber = lineNumber,
        Hits = hits,
      };

    /// <summary>Empty input yields no PackageCoverageReports.</summary>
    [FUnitStepTest(typeof(BuildMethodCoverageStep))]
    public void EmptyInput_YieldsEmptyOutput()
    {
      var result = Invoke(BuildMethodCoverageStep.Create(), Enumerable.Empty<LineCoverageRow>());

      Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// A method's TotalHits is the hit count of its earliest instrumented line, NOT the sum
    /// across all lines — Cobertura records the entry-point hit count there. Summing would
    /// overcount loop iterations.
    /// </summary>
    [FUnitStepTest(typeof(BuildMethodCoverageStep))]
    public void MethodTotalHits_UsesEarliestLineHitCount()
    {
      var rows = new[]
      {
        Row("T", "Pkg", "Pkg.Foo", "Bar", "()", lineNumber: 20, hits: 100),
        Row("T", "Pkg", "Pkg.Foo", "Bar", "()", lineNumber: 10, hits: 5),
        Row("T", "Pkg", "Pkg.Foo", "Bar", "()", lineNumber: 30, hits: 50),
      };

      var report = Invoke(BuildMethodCoverageStep.Create(), rows).Single();
      var method = report.Namespaces.Single().Classes.Single().Methods.Single();
      var projHits = method.TestProjects.Single();

      Assert.That(projHits.TotalHits, Is.EqualTo(5));
    }

    /// <summary>
    /// SplitClassName produces (namespace, shortClassName). For
    /// <c>Pkg.Sub.Foo</c>, the namespace is <c>Pkg.Sub</c> and the short class is <c>Foo</c>.
    /// </summary>
    [FUnitStepTest(typeof(BuildMethodCoverageStep))]
    public void NestedClassName_SplitsIntoNamespaceAndShortClass()
    {
      var rows = new[] { Row("T", "Pkg", "Pkg.Sub.Foo", "Bar", "()", 1, 1) };

      var report = Invoke(BuildMethodCoverageStep.Create(), rows).Single();
      var ns = report.Namespaces.Single();
      var cls = ns.Classes.Single();

      Assert.That(ns.Namespace, Is.EqualTo("Pkg.Sub"));
      Assert.That(cls.ClassName, Is.EqualTo("Foo"));
    }

    /// <summary>
    /// A class name with no dots (root-namespace type) lands in an empty-namespace bucket;
    /// its ClassName is the full (unqualified) name. The null ClassName branch only fires
    /// when Cobertura reports a method with no class context at all (empty class name).
    /// </summary>
    [FUnitStepTest(typeof(BuildMethodCoverageStep))]
    public void RootNamespaceClass_LandsInEmptyNamespaceBucket()
    {
      var rows = new[] { Row("T", "Pkg", "Foo", "Bar", "()", 1, 1) };

      var report = Invoke(BuildMethodCoverageStep.Create(), rows).Single();
      var ns = report.Namespaces.Single();

      Assert.That(ns.Namespace, Is.EqualTo(string.Empty));
      Assert.That(ns.Classes.Single().ClassName, Is.EqualTo("Foo"));
    }

    /// <summary>
    /// The same method exercised by multiple test projects produces one MethodCoverage entry
    /// with one TestProjectHits per project — confirms the per-test-project breakdown.
    /// </summary>
    [FUnitStepTest(typeof(BuildMethodCoverageStep))]
    public void SameMethodAcrossTestProjects_YieldsPerProjectHits()
    {
      var rows = new[]
      {
        Row("TestA", "Pkg", "Pkg.Foo", "Bar", "()", 10, 5),
        Row("TestB", "Pkg", "Pkg.Foo", "Bar", "()", 10, 0),
      };

      var report = Invoke(BuildMethodCoverageStep.Create(), rows).Single();
      var method = report.Namespaces.Single().Classes.Single().Methods.Single();

      Assert.That(method.TestProjects, Has.Count.EqualTo(2));
      Assert.That(method.TotalHits, Is.EqualTo(5)); // sum across project entry-point hits
    }

    /// <summary>
    /// SourceFile passes through from the underlying line entries — coverage triage relies
    /// on this to navigate from a row directly to the file. An empty source file collapses
    /// to null so consumers can distinguish "missing" from "present but empty string".
    /// </summary>
    [FUnitStepTest(typeof(BuildMethodCoverageStep))]
    public void SourceFile_PassesThroughFromUnderlyingLines()
    {
      var rows = new[] { Row("T", "Pkg", "Pkg.Foo", "Bar", "()", 1, 1, sourceFile: "src/Pkg/Foo.cs") };

      var method = Invoke(BuildMethodCoverageStep.Create(), rows)
        .Single()
        .Namespaces.Single()
        .Classes.Single()
        .Methods.Single();

      Assert.That(method.SourceFile, Is.EqualTo("src/Pkg/Foo.cs"));
    }

    /// <summary>
    /// LineCount is the count of distinct line numbers contributing to the method —
    /// not the total row count. Multiple test projects covering the same lines must NOT
    /// inflate the count.
    /// </summary>
    [FUnitStepTest(typeof(BuildMethodCoverageStep))]
    public void LineCount_DeduplicatesLinesAcrossTestProjects()
    {
      var rows = new[]
      {
        Row("TestA", "Pkg", "Pkg.Foo", "Bar", "()", lineNumber: 10, hits: 5),
        Row("TestA", "Pkg", "Pkg.Foo", "Bar", "()", lineNumber: 11, hits: 5),
        Row("TestB", "Pkg", "Pkg.Foo", "Bar", "()", lineNumber: 10, hits: 0),
        Row("TestB", "Pkg", "Pkg.Foo", "Bar", "()", lineNumber: 11, hits: 0),
      };

      var method = Invoke(BuildMethodCoverageStep.Create(), rows)
        .Single()
        .Namespaces.Single()
        .Classes.Single()
        .Methods.Single();

      Assert.That(method.LineCount, Is.EqualTo(2)); // lines 10 and 11, not 4 rows
    }
  }
#endif
}
