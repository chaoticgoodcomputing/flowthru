using Flowthru.Core.Steps;
using FlowthruCoverage.Data._01_Raw.Schemas;
using FlowthruCoverage.Data._02_Intermediate.Schemas;
using FlowthruCoverage.Data._04_Reporting.Schemas;
#if FUNIT_ENABLED
using Flowthru.FUnit;
#endif

namespace FlowthruCoverage.Flows.Reporting.Steps;

/// <summary>
/// Builds the flat project → file → method icicle hierarchy for src libraries.
///
/// SourceFile values are canonicalised to the path suffix starting at the package segment
/// (e.g. <c>Abstractions/Foo.cs</c>) so that local-disk paths and SourceLink URLs pointing at
/// the same file collapse into one node. Without this collapse, real hits sit under the
/// https rows and the local rows show 0% — most Flowthru tests consume the libraries via
/// NuGet+SourceLink rather than project references.
///
/// For each method group (per (SrcPackage, RelativePath, ClassName, MethodName, MethodSignature)):
///   • <c>TotalLines</c> = distinct LineNumbers contributed across test projects
///   • <c>CoveredLines</c> = distinct LineNumbers where any test project hit > 0
///
/// File nodes sum the methods within (SrcPackage, RelativePath); project nodes sum the files
/// within SrcPackage. Coverage percent is recomputed at each level from the rolled-up totals.
///
/// Filtering: only manifest <c>Library</c> packages are kept — those are the projects under
/// <c>src/</c>. Rows with an empty SourceFile are dropped (no file to attribute them to);
/// rows whose source path can't be canonicalised (no <c>/{srcPackage}/</c> segment) are also
/// dropped, since their file identity would be ambiguous.
///
/// Output rows are ordered Project → File → Method with stable alphabetical order within each
/// level so the icicle layout is deterministic.
/// </summary>
[FlowthruStep]
public static class BuildIcicleCoverageStep
{
  public static Func<
    (IEnumerable<LineCoverageRow>, IEnumerable<ProjectManifestEntry>),
    IEnumerable<IcicleCoverageNode>
  > Create()
  {
    return inputs =>
    {
      var (rows, manifestEntries) = inputs;

      var libraryPackages = manifestEntries
        .Where(e => e.ProjectType == "Library")
        .Select(e => e.AssemblyName)
        .ToHashSet(StringComparer.Ordinal);

      var canonical = rows.Where(r => libraryPackages.Contains(r.SrcPackage))
        .Where(r => !string.IsNullOrEmpty(r.SourceFile))
        .Select(r => (Row: r, Relative: TryCanonicalisePath(r.SrcPackage, r.SourceFile)))
        .Where(t => t.Relative is not null)
        .ToList();

      var methodNodes = canonical
        .GroupBy(t => (
          t.Row.SrcPackage,
          Relative: t.Relative!,
          t.Row.ClassName,
          t.Row.MethodName,
          t.Row.MethodSignature
        ))
        .Select(g =>
        {
          var lineHits = g.GroupBy(t => t.Row.LineNumber)
            .Select(lg => new { Line = lg.Key, Covered = lg.Any(t => t.Row.Hits > 0) })
            .ToList();

          var totalLines = lineHits.Count;
          var coveredLines = lineHits.Count(l => l.Covered);

          var fileId = MakeFileId(g.Key.SrcPackage, g.Key.Relative);
          var methodId = $"{fileId}::{g.Key.ClassName}.{g.Key.MethodName}{g.Key.MethodSignature}";
          var label = ShortClassName(g.Key.ClassName) is { Length: > 0 } shortClass
            ? $"{shortClass}.{g.Key.MethodName}{g.Key.MethodSignature}"
            : $"{g.Key.MethodName}{g.Key.MethodSignature}";

          return new IcicleCoverageNode
          {
            Id = methodId,
            ParentId = fileId,
            Label = label,
            Level = "Method",
            CoveredLines = coveredLines,
            TotalLines = totalLines,
            CoveragePercent = Percent(coveredLines, totalLines),
          };
        })
        .Where(n => n.TotalLines > 0)
        .ToList();

      var fileNodes = methodNodes
        .GroupBy(m => m.ParentId)
        .Select(g =>
        {
          var totalLines = g.Sum(m => m.TotalLines);
          var coveredLines = g.Sum(m => m.CoveredLines);
          var (srcPackage, relative) = SplitFileId(g.Key);
          return new IcicleCoverageNode
          {
            Id = g.Key,
            ParentId = srcPackage,
            Label = relative,
            Level = "File",
            CoveredLines = coveredLines,
            TotalLines = totalLines,
            CoveragePercent = Percent(coveredLines, totalLines),
          };
        })
        .ToList();

      var projectNodes = fileNodes
        .GroupBy(f => f.ParentId)
        .Select(g =>
        {
          var totalLines = g.Sum(f => f.TotalLines);
          var coveredLines = g.Sum(f => f.CoveredLines);
          return new IcicleCoverageNode
          {
            Id = g.Key,
            ParentId = string.Empty,
            Label = g.Key,
            Level = "Project",
            CoveredLines = coveredLines,
            TotalLines = totalLines,
            CoveragePercent = Percent(coveredLines, totalLines),
          };
        })
        .ToList();

      return projectNodes
        .OrderBy(p => p.Id, StringComparer.Ordinal)
        .Concat(fileNodes.OrderBy(f => f.Id, StringComparer.Ordinal))
        .Concat(methodNodes.OrderBy(m => m.Id, StringComparer.Ordinal));
    };
  }

  private static string MakeFileId(string srcPackage, string sourceFile) =>
    $"{srcPackage}::{sourceFile}";

  private static (string SrcPackage, string SourceFile) SplitFileId(string fileId)
  {
    var idx = fileId.IndexOf("::", StringComparison.Ordinal);
    return idx < 0
      ? (string.Empty, fileId)
      : (fileId[..idx], fileId[(idx + 2)..]);
  }

  /// <summary>
  /// Canonicalises a Cobertura SourceFile to the path suffix starting just after the
  /// <c>/{srcPackage}/</c> segment, so e.g.
  /// <c>/.../src/core/Flowthru.Core/Abstractions/Foo.cs</c> and
  /// <c>https://.../flowthru/{sha}/src/core/Flowthru.Core/Abstractions/Foo.cs</c>
  /// both collapse to <c>Abstractions/Foo.cs</c>. Returns <see langword="null"/> when the
  /// segment isn't present — those rows are excluded from the icicle, since their file
  /// identity would be ambiguous and they'd otherwise appear as a flat dump of basenames.
  /// </summary>
  private static string? TryCanonicalisePath(string srcPackage, string sourceFile)
  {
    var marker = $"/{srcPackage}/";
    var idx = sourceFile.LastIndexOf(marker, StringComparison.Ordinal);
    return idx >= 0 ? sourceFile[(idx + marker.Length)..] : null;
  }

  /// <summary>
  /// Returns the unqualified class name (last dot-separated segment), or empty for empty input.
  /// </summary>
  private static string ShortClassName(string fullyQualified)
  {
    if (string.IsNullOrEmpty(fullyQualified))
      return string.Empty;
    var lastDot = fullyQualified.LastIndexOf('.');
    return lastDot >= 0 ? fullyQualified[(lastDot + 1)..] : fullyQualified;
  }

  private static double Percent(int covered, int total) =>
    total == 0 ? 0.0 : Math.Round(covered * 100.0 / total, 2);

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="BuildIcicleCoverageStep"/>.</summary>
  public class Tests : FunitContext
  {
    private static ProjectManifestEntry Manifest(string assemblyName, string projectType) =>
      new()
      {
        AssemblyName = assemblyName,
        ProjectType = projectType,
        Subgroup = "Core",
      };

    private static LineCoverageRow Row(
      string srcPackage,
      string sourceFile,
      string className,
      string methodName,
      string methodSignature,
      int lineNumber,
      int hits,
      string testProject = "T"
    ) =>
      new()
      {
        TestProject = testProject,
        SrcPackage = srcPackage,
        SourceFile = sourceFile,
        ClassName = className,
        MethodName = methodName,
        MethodSignature = methodSignature,
        LineNumber = lineNumber,
        Hits = hits,
      };

    /// <summary>
    /// Three single-method files at 100/50/0 % roll up to a project at 50 % (3 lines covered
    /// out of 6 instrumented). Confirms the example shape the icicle is built to render.
    /// </summary>
    [StepTest(typeof(BuildIcicleCoverageStep))]
    public void ThreeFiles_OneMethodEach_RollUpToProjectAggregate()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", lineNumber: 1, hits: 1),
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", lineNumber: 2, hits: 1),

        Row("Pkg", "/repo/src/Pkg/B.cs", "Pkg.B", "M", "()", lineNumber: 1, hits: 1),
        Row("Pkg", "/repo/src/Pkg/B.cs", "Pkg.B", "M", "()", lineNumber: 2, hits: 0),

        Row("Pkg", "/repo/src/Pkg/C.cs", "Pkg.C", "M", "()", lineNumber: 1, hits: 0),
        Row("Pkg", "/repo/src/Pkg/C.cs", "Pkg.C", "M", "()", lineNumber: 2, hits: 0),
      };
      var manifest = new[] { Manifest("Pkg", "Library") };

      var nodes = Invoke(BuildIcicleCoverageStep.Create(), (rows, manifest)).ToList();
      var project = nodes.Single(n => n.Level == "Project");
      var files = nodes.Where(n => n.Level == "File").OrderBy(n => n.Label).ToList();

      Assert.That(project.CoveragePercent, Is.EqualTo(50.0));
      Assert.That(project.TotalLines, Is.EqualTo(6));
      Assert.That(project.CoveredLines, Is.EqualTo(3));
      Assert.That(files.Select(f => f.CoveragePercent), Is.EqualTo(new[] { 100.0, 50.0, 0.0 }));
    }

    /// <summary>
    /// A line hit by any test project counts as covered. Two test projects, one with hits=0
    /// and one with hits=5 on the same line, must produce CoveredLines=1.
    /// </summary>
    [StepTest(typeof(BuildIcicleCoverageStep))]
    public void LineCovered_IfAnyTestProjectHits()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", 1, 0, testProject: "T1"),
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", 1, 5, testProject: "T2"),
      };
      var manifest = new[] { Manifest("Pkg", "Library") };

      var method = Invoke(BuildIcicleCoverageStep.Create(), (rows, manifest))
        .Single(n => n.Level == "Method");

      Assert.That(method.TotalLines, Is.EqualTo(1));
      Assert.That(method.CoveredLines, Is.EqualTo(1));
    }

    /// <summary>
    /// Non-Library manifest entries (test projects, examples) and source paths that don't
    /// contain a <c>/{srcPackage}/</c> segment are excluded — the icicle only describes
    /// authored src/ code, and unattributable rows would otherwise muddy the file hierarchy.
    /// </summary>
    [StepTest(typeof(BuildIcicleCoverageStep))]
    public void NonLibraryAndUnattributableSources_AreExcluded()
    {
      var rows = new[]
      {
        Row("Lib", "/repo/src/Lib/A.cs", "Lib.A", "M", "()", 1, 1),
        Row("Lib.Tests", "/repo/tests/Lib.Tests/T.cs", "Lib.T", "M", "()", 1, 1),
        Row("Lib", "https://example.com/foo.cs", "Lib.X", "M", "()", 1, 1),
      };
      var manifest = new[]
      {
        Manifest("Lib", "Library"),
        Manifest("Lib.Tests", "LibraryTest"),
      };

      var nodes = Invoke(BuildIcicleCoverageStep.Create(), (rows, manifest)).ToList();

      Assert.That(nodes.Where(n => n.Level == "Project").Select(n => n.Id), Is.EqualTo(new[] { "Lib" }));
      Assert.That(nodes.Count(n => n.Level == "Method"), Is.EqualTo(1));
    }

    /// <summary>
    /// Local-disk paths and SourceLink URLs that resolve to the same logical file collapse
    /// into one method node. Without this collapse, real coverage (which lives on the https
    /// rows for tests that consume libraries via NuGet+SourceLink) gets dropped and every
    /// project reads 0%.
    /// </summary>
    [StepTest(typeof(BuildIcicleCoverageStep))]
    public void LocalAndUrlPaths_CollapseByCanonicalRelativePath()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", lineNumber: 1, hits: 0, testProject: "T1"),
        Row("Pkg", "https://example.com/repo/sha/src/Pkg/A.cs", "Pkg.A", "M", "()", lineNumber: 1, hits: 5, testProject: "T2"),
      };
      var manifest = new[] { Manifest("Pkg", "Library") };

      var nodes = Invoke(BuildIcicleCoverageStep.Create(), (rows, manifest)).ToList();

      Assert.That(nodes.Count(n => n.Level == "File"), Is.EqualTo(1));
      Assert.That(nodes.Count(n => n.Level == "Method"), Is.EqualTo(1));
      Assert.That(nodes.Single(n => n.Level == "Method").CoveragePercent, Is.EqualTo(100.0));
    }

    /// <summary>
    /// The file label is the path suffix relative to the package segment of the source path.
    /// </summary>
    [StepTest(typeof(BuildIcicleCoverageStep))]
    public void FileLabel_IsRelativeToPackageSegment()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/Sub/A.cs", "Pkg.Sub.A", "M", "()", 1, 1),
      };
      var manifest = new[] { Manifest("Pkg", "Library") };

      var file = Invoke(BuildIcicleCoverageStep.Create(), (rows, manifest))
        .Single(n => n.Level == "File");

      Assert.That(file.Label, Is.EqualTo("Sub/A.cs"));
    }

    /// <summary>
    /// Parent ids form a valid tree: every Method has its File as ParentId, every File has
    /// its Project as ParentId, every Project has empty ParentId.
    /// </summary>
    [StepTest(typeof(BuildIcicleCoverageStep))]
    public void ParentIds_FormValidTree()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", 1, 1),
      };
      var manifest = new[] { Manifest("Pkg", "Library") };

      var nodes = Invoke(BuildIcicleCoverageStep.Create(), (rows, manifest)).ToList();
      var ids = nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);

      foreach (var node in nodes)
      {
        if (node.Level == "Project")
          Assert.That(node.ParentId, Is.EqualTo(string.Empty));
        else
          Assert.That(ids, Does.Contain(node.ParentId));
      }
    }
  }
#endif
}
