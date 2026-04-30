using Flowthru.Core.Steps;
using FlowthruCoverage.Data._01_Raw.Schemas;
using FlowthruCoverage.Data._02_Intermediate.Schemas;
using FlowthruCoverage.Data._04_Reporting.Schemas;
#if FUNIT_ENABLED
using Flowthru.FUnit;
#endif

namespace FlowthruCoverage.Flows.Reporting.Steps;

/// <summary>
/// Builds the icicle hierarchy for src libraries: Project → Directory(/sub-dirs) → File →
/// Method, with intermediate Directory levels mirroring the on-disk folder structure
/// (e.g. <c>Flowthru.Core / Data / Capabilities / Bar.cs / Bar.M()</c>).
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
/// Aggregation rolls up bottom-up: methods → files → directories (innermost to outermost) →
/// project. Coverage percent is recomputed at each level from the rolled-up totals.
///
/// Identifier conventions:
///   • Project — <c>{SrcPackage}</c>
///   • Directory — <c>{SrcPackage}::{path}/</c> (trailing slash distinguishes dirs from files)
///   • File — <c>{SrcPackage}::{relativeFilePath}</c>
///   • Method — <c>{fileId}::{className}.{methodName}{signature}</c>
///
/// Filtering: only manifest <c>Library</c> packages are kept — those are the projects under
/// <c>src/</c>. Rows with an empty SourceFile are dropped (no file to attribute them to);
/// rows whose source path can't be canonicalised (no <c>/{srcPackage}/</c> segment) are also
/// dropped, since their file identity would be ambiguous.
///
/// Output rows are ordered Project → Directory → File → Method, alphabetical within each level.
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

      // File nodes parent on the deepest containing directory (or the project for files at
      // the package root). Label is just the basename — the directory chain becomes the
      // explicit hierarchy below.
      var fileNodes = methodNodes
        .GroupBy(m => m.ParentId)
        .Select(g =>
        {
          var totalLines = g.Sum(m => m.TotalLines);
          var coveredLines = g.Sum(m => m.CoveredLines);
          var (srcPackage, relative) = SplitFileId(g.Key);
          var (parentId, fileLabel) = ResolveFileParent(srcPackage, relative);
          return new IcicleCoverageNode
          {
            Id = g.Key,
            ParentId = parentId,
            Label = fileLabel,
            Level = "File",
            CoveredLines = coveredLines,
            TotalLines = totalLines,
            CoveragePercent = Percent(coveredLines, totalLines),
          };
        })
        .ToList();

      // Walk every file up its directory chain, recording (dirId → parentDirId, label) once.
      var directoryParents = new Dictionary<string, string>(StringComparer.Ordinal);
      var directoryLabels = new Dictionary<string, string>(StringComparer.Ordinal);
      foreach (var file in fileNodes)
        RecordDirectoryChain(file.ParentId, directoryParents, directoryLabels);

      // Aggregate Directory totals bottom-up. Children of a directory are immediate files
      // (whose ParentId equals the dir id) plus immediate sub-directories (whose entry in
      // directoryParents maps to this dir id). Sorting by depth descending ensures every
      // sub-directory's totals are already computed when we visit its parent.
      var filesByParent = fileNodes
        .GroupBy(f => f.ParentId, StringComparer.Ordinal)
        .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

      var subdirsByParent = directoryParents
        .GroupBy(kv => kv.Value, StringComparer.Ordinal)
        .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList(), StringComparer.Ordinal);

      var dirAggregates = new Dictionary<string, (int Covered, int Total)>(StringComparer.Ordinal);
      foreach (var dirId in directoryParents.Keys.OrderByDescending(id => id.Count(c => c == '/')))
      {
        var covered = 0;
        var total = 0;
        if (filesByParent.TryGetValue(dirId, out var fs))
        {
          covered += fs.Sum(f => f.CoveredLines);
          total += fs.Sum(f => f.TotalLines);
        }
        if (subdirsByParent.TryGetValue(dirId, out var ss))
        {
          foreach (var subId in ss)
          {
            var (sc, st) = dirAggregates[subId];
            covered += sc;
            total += st;
          }
        }
        dirAggregates[dirId] = (covered, total);
      }

      var directoryNodes = directoryParents
        .Select(kv =>
        {
          var (covered, total) = dirAggregates[kv.Key];
          return new IcicleCoverageNode
          {
            Id = kv.Key,
            ParentId = kv.Value,
            Label = directoryLabels[kv.Key],
            Level = "Directory",
            CoveredLines = covered,
            TotalLines = total,
            CoveragePercent = Percent(covered, total),
          };
        })
        .ToList();

      // Project nodes aggregate the union of their immediate children (top-level files +
      // top-level directories). Project IDs are exactly those ParentId values without a "::".
      var projectIds = fileNodes.Select(f => f.ParentId)
        .Concat(directoryNodes.Select(d => d.ParentId))
        .Where(p => !p.Contains("::", StringComparison.Ordinal))
        .ToHashSet(StringComparer.Ordinal);

      var projectNodes = projectIds
        .Select(projectId =>
        {
          var topFiles = filesByParent.TryGetValue(projectId, out var fs) ? fs : new List<IcicleCoverageNode>();
          var topDirs = subdirsByParent.TryGetValue(projectId, out var ds) ? ds : new List<string>();

          var covered = topFiles.Sum(f => f.CoveredLines)
            + topDirs.Sum(d => dirAggregates[d].Covered);
          var total = topFiles.Sum(f => f.TotalLines)
            + topDirs.Sum(d => dirAggregates[d].Total);

          return new IcicleCoverageNode
          {
            Id = projectId,
            ParentId = string.Empty,
            Label = projectId,
            Level = "Project",
            CoveredLines = covered,
            TotalLines = total,
            CoveragePercent = Percent(covered, total),
          };
        })
        .ToList();

      return projectNodes
        .OrderBy(p => p.Id, StringComparer.Ordinal)
        .Concat(directoryNodes.OrderBy(d => d.Id, StringComparer.Ordinal))
        .Concat(fileNodes.OrderBy(f => f.Id, StringComparer.Ordinal))
        .Concat(methodNodes.OrderBy(m => m.Id, StringComparer.Ordinal));
    };
  }

  /// <summary>
  /// Splits a relative file path into (parent directory ID, file basename). Files at the
  /// package root land directly under the project (ParentId = SrcPackage); files in any
  /// subdirectory get the deepest directory's id (with trailing slash) as their parent.
  /// </summary>
  private static (string ParentId, string Label) ResolveFileParent(string srcPackage, string relativePath)
  {
    var lastSlash = relativePath.LastIndexOf('/');
    if (lastSlash < 0)
      return (srcPackage, relativePath);

    var dirPath = relativePath[..lastSlash];
    var fileName = relativePath[(lastSlash + 1)..];
    return ($"{srcPackage}::{dirPath}/", fileName);
  }

  /// <summary>
  /// Walks from a starting node ID up its directory chain, recording each unseen directory's
  /// parent ID and display label. The walk stops at the project boundary (the first ID
  /// without a <c>::</c> separator). Idempotent — already-seen directories short-circuit.
  /// </summary>
  private static void RecordDirectoryChain(
    string startId,
    Dictionary<string, string> directoryParents,
    Dictionary<string, string> directoryLabels
  )
  {
    var current = startId;
    while (true)
    {
      var idx = current.IndexOf("::", StringComparison.Ordinal);
      if (idx < 0)
        return; // Reached the project boundary.

      if (directoryParents.ContainsKey(current))
        return; // Already processed this dir and its ancestors on a prior walk.

      var srcPackage = current[..idx];
      var dirPath = current[(idx + 2)..].TrimEnd('/');
      var lastSlash = dirPath.LastIndexOf('/');

      string parent;
      string label;
      if (lastSlash < 0)
      {
        parent = srcPackage;
        label = dirPath;
      }
      else
      {
        parent = $"{srcPackage}::{dirPath[..lastSlash]}/";
        label = dirPath[(lastSlash + 1)..];
      }

      directoryParents[current] = parent;
      directoryLabels[current] = label;
      current = parent;
    }
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
  public class Tests : FUnitContext
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
    /// File labels are basenames; directory segments become their own intermediate Directory
    /// nodes. A file at <c>Sub/A.cs</c> renders as Project → Directory("Sub") → File("A.cs").
    /// </summary>
    [StepTest(typeof(BuildIcicleCoverageStep))]
    public void FileLabel_IsBasename_DirectorySegmentsBecomeOwnLevel()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/Sub/A.cs", "Pkg.Sub.A", "M", "()", 1, 1),
      };
      var manifest = new[] { Manifest("Pkg", "Library") };

      var nodes = Invoke(BuildIcicleCoverageStep.Create(), (rows, manifest)).ToList();
      var file = nodes.Single(n => n.Level == "File");
      var directories = nodes.Where(n => n.Level == "Directory").ToList();

      Assert.That(file.Label, Is.EqualTo("A.cs"));
      Assert.That(directories.Select(d => d.Label), Is.EqualTo(new[] { "Sub" }));
      Assert.That(file.ParentId, Is.EqualTo(directories.Single().Id));
    }

    /// <summary>
    /// Files at the package root parent directly on the project — no intermediate Directory
    /// node is emitted when the relative path has no slash. Confirms the degenerate case.
    /// </summary>
    [StepTest(typeof(BuildIcicleCoverageStep))]
    public void RootLevelFile_ParentsDirectlyOnProject_NoDirectoryNode()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/Foo.cs", "Pkg.Foo", "M", "()", 1, 1),
      };
      var manifest = new[] { Manifest("Pkg", "Library") };

      var nodes = Invoke(BuildIcicleCoverageStep.Create(), (rows, manifest)).ToList();

      Assert.That(nodes.Any(n => n.Level == "Directory"), Is.False);
      var file = nodes.Single(n => n.Level == "File");
      Assert.That(file.ParentId, Is.EqualTo("Pkg"));
    }

    /// <summary>
    /// Multiple sub-directory levels nest correctly: a file at <c>Data/Capabilities/Bar.cs</c>
    /// produces two Directory nodes (<c>Data</c>, <c>Capabilities</c>) with the deeper one
    /// parented on the shallower.
    /// </summary>
    [StepTest(typeof(BuildIcicleCoverageStep))]
    public void NestedDirectories_FormProperParentChain()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/Data/Capabilities/Bar.cs", "Pkg.Data.Capabilities.Bar", "M", "()", 1, 1),
      };
      var manifest = new[] { Manifest("Pkg", "Library") };

      var nodes = Invoke(BuildIcicleCoverageStep.Create(), (rows, manifest)).ToList();
      var directories = nodes.Where(n => n.Level == "Directory").ToList();

      var data = directories.Single(d => d.Label == "Data");
      var caps = directories.Single(d => d.Label == "Capabilities");

      Assert.That(data.ParentId, Is.EqualTo("Pkg"));
      Assert.That(caps.ParentId, Is.EqualTo(data.Id));
      Assert.That(nodes.Single(n => n.Level == "File").ParentId, Is.EqualTo(caps.Id));
    }

    /// <summary>
    /// Directory-level totals roll up from descendants. A directory with a single file at
    /// 50% and a sub-directory containing one file at 100% reports 75% across 4 lines.
    /// </summary>
    [StepTest(typeof(BuildIcicleCoverageStep))]
    public void DirectoryTotals_RollUpFromFilesAndSubDirectories()
    {
      var rows = new[]
      {
        // Top.cs in Sub/: 1/2 covered
        Row("Pkg", "/repo/src/Pkg/Sub/Top.cs", "Pkg.Sub.Top", "M", "()", 1, 1),
        Row("Pkg", "/repo/src/Pkg/Sub/Top.cs", "Pkg.Sub.Top", "M", "()", 2, 0),
        // Deep.cs in Sub/Inner/: 2/2 covered
        Row("Pkg", "/repo/src/Pkg/Sub/Inner/Deep.cs", "Pkg.Sub.Inner.Deep", "M", "()", 1, 1),
        Row("Pkg", "/repo/src/Pkg/Sub/Inner/Deep.cs", "Pkg.Sub.Inner.Deep", "M", "()", 2, 1),
      };
      var manifest = new[] { Manifest("Pkg", "Library") };

      var nodes = Invoke(BuildIcicleCoverageStep.Create(), (rows, manifest)).ToList();
      var sub = nodes.Single(n => n.Level == "Directory" && n.Label == "Sub");

      Assert.That(sub.TotalLines, Is.EqualTo(4));
      Assert.That(sub.CoveredLines, Is.EqualTo(3));
      Assert.That(sub.CoveragePercent, Is.EqualTo(75.0));
    }

    /// <summary>
    /// Parent ids form a valid tree: every non-Project node's ParentId resolves to another
    /// node's Id; Project nodes have empty ParentId. With Directory nodes, this contract
    /// extends to a multi-level chain (Method → File → Dir(s) → Project).
    /// </summary>
    [StepTest(typeof(BuildIcicleCoverageStep))]
    public void ParentIds_FormValidTree()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", 1, 1),
        Row("Pkg", "/repo/src/Pkg/Sub/Inner/B.cs", "Pkg.Sub.Inner.B", "M", "()", 1, 1),
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
