using Flowthru.Step;
using FlowthruCoverage.Data._01_Raw.Schemas;
using FlowthruCoverage.Data._02_Intermediate.Schemas;
using FlowthruCoverage.Data._04_Reporting.Schemas;
#if FUNIT_ENABLED
using Flowthru.Step.Testing;
#endif

namespace FlowthruCoverage.Flows.Reporting.Steps;

/// <summary>
/// Builds the per-library icicle hierarchy (Project → Directory → File →
/// Method) with line-level provenance tracking. Each node carries four
/// counts — total / any-covered / unit-covered / integration-covered —
/// from which the downstream renderer composes an RGB colour encoding.
/// </summary>
/// <remarks>
/// <para>
/// Each line's provenance is computed once from the full
/// <see cref="LineCoverageRow"/> stream by classifying every hitting
/// <c>TestProject</c> against the manifest: the unit-test project is the
/// one whose name equals <c>SrcPackage + ".Tests"</c>; an integration
/// hit comes from any manifest entry with <c>ProjectType="Example"</c>
/// (example pipelines act as integration coverage for the libraries they
/// exercise). Everything else (peer test projects, dedicated integration
/// suites, helpers) still contributes to "any covered" but doesn't tag
/// unit or integration individually.
/// </para>
/// <para>
/// Structural machinery: path canonicalisation, directory chain walk,
/// bottom-up roll-up. Each node carries the four counts the downstream
/// renderer uses to derive the per-tile RGB encoding.
/// </para>
/// </remarks>
[FlowthruStep]
public static class BuildProvenanceIcicleStep
{
  /// <summary>
  /// Suffix that marks a test project as the declared unit-test counterpart
  /// of its same-named library (e.g. <c>Flowthru.Core.Tests</c> for
  /// <c>Flowthru.Core</c>).
  /// </summary>
  public const string UnitTestSuffix = ".Tests";

  public static Func<
    (IEnumerable<LineCoverageRow>, IEnumerable<ProjectManifestEntry>),
    IEnumerable<ProvenanceIcicleNode>
  > Create()
  {
    return inputs =>
    {
      var (rows, manifestEntries) = inputs;

      var manifestList = manifestEntries.ToList();
      var libraryPackages = manifestList
        .Where(e => e.ProjectType == "Library")
        .Select(e => e.AssemblyName)
        .ToHashSet(StringComparer.Ordinal);
      // Manifest "Example" rows are the integration-coverage sources: the
      // example pipelines that drive libraries through end-to-end runs.
      var integrationAssemblies = manifestList
        .Where(e => string.Equals(e.ProjectType, "Example", StringComparison.Ordinal))
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
          var srcPackage = g.Key.SrcPackage;
          var unitAssembly = srcPackage + UnitTestSuffix;

          // For each distinct line, derive a 3-bit provenance:
          // (anyCovered, unitCovered, integrationCovered). Lines with zero
          // hits across all rows count toward the denominator only.
          var lineProvenance = g.GroupBy(t => t.Row.LineNumber)
            .Select(lg =>
            {
              var hits = lg.Where(t => t.Row.Hits > 0).ToList();
              return new
              {
                Any = hits.Count > 0,
                Unit = hits.Any(t =>
                  string.Equals(t.Row.TestProject, unitAssembly, StringComparison.Ordinal)
                ),
                Integration = hits.Any(t => integrationAssemblies.Contains(t.Row.TestProject)),
              };
            })
            .ToList();

          var totalLines = lineProvenance.Count;
          var anyCovered = lineProvenance.Count(l => l.Any);
          var unitCovered = lineProvenance.Count(l => l.Unit);
          var integrationCovered = lineProvenance.Count(l => l.Integration);

          var fileId = MakeFileId(srcPackage, g.Key.Relative);
          var methodId = $"{fileId}::{g.Key.ClassName}.{g.Key.MethodName}{g.Key.MethodSignature}";
          var label = ShortClassName(g.Key.ClassName) is { Length: > 0 } shortClass
            ? $"{shortClass}.{g.Key.MethodName}{g.Key.MethodSignature}"
            : $"{g.Key.MethodName}{g.Key.MethodSignature}";

          return new ProvenanceIcicleNode
          {
            Id = methodId,
            ParentId = fileId,
            Label = label,
            Level = "Method",
            TotalLines = totalLines,
            AnyCovered = anyCovered,
            UnitCovered = unitCovered,
            IntegrationCovered = integrationCovered,
          };
        })
        .Where(n => n.TotalLines > 0)
        .ToList();

      var fileNodes = methodNodes
        .GroupBy(m => m.ParentId)
        .Select(g =>
        {
          var (srcPackage, relative) = SplitFileId(g.Key);
          var (parentId, fileLabel) = ResolveFileParent(srcPackage, relative);
          return new ProvenanceIcicleNode
          {
            Id = g.Key,
            ParentId = parentId,
            Label = fileLabel,
            Level = "File",
            TotalLines = g.Sum(m => m.TotalLines),
            AnyCovered = g.Sum(m => m.AnyCovered),
            UnitCovered = g.Sum(m => m.UnitCovered),
            IntegrationCovered = g.Sum(m => m.IntegrationCovered),
          };
        })
        .ToList();

      var directoryParents = new Dictionary<string, string>(StringComparer.Ordinal);
      var directoryLabels = new Dictionary<string, string>(StringComparer.Ordinal);
      foreach (var file in fileNodes)
        RecordDirectoryChain(file.ParentId, directoryParents, directoryLabels);

      var filesByParent = fileNodes
        .GroupBy(f => f.ParentId, StringComparer.Ordinal)
        .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

      var subdirsByParent = directoryParents
        .GroupBy(kv => kv.Value, StringComparer.Ordinal)
        .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList(), StringComparer.Ordinal);

      var dirAggregates = new Dictionary<string, NodeCounts>(StringComparer.Ordinal);
      foreach (var dirId in directoryParents.Keys.OrderByDescending(id => id.Count(c => c == '/')))
      {
        var acc = new NodeCounts();
        if (filesByParent.TryGetValue(dirId, out var fs))
        {
          foreach (var f in fs) acc = acc.Add(f);
        }
        if (subdirsByParent.TryGetValue(dirId, out var ss))
        {
          foreach (var subId in ss) acc = acc.Add(dirAggregates[subId]);
        }
        dirAggregates[dirId] = acc;
      }

      var directoryNodes = directoryParents
        .Select(kv =>
        {
          var counts = dirAggregates[kv.Key];
          return new ProvenanceIcicleNode
          {
            Id = kv.Key,
            ParentId = kv.Value,
            Label = directoryLabels[kv.Key],
            Level = "Directory",
            TotalLines = counts.Total,
            AnyCovered = counts.Any,
            UnitCovered = counts.Unit,
            IntegrationCovered = counts.Integration,
          };
        })
        .ToList();

      var projectIds = fileNodes.Select(f => f.ParentId)
        .Concat(directoryNodes.Select(d => d.ParentId))
        .Where(p => !p.Contains("::", StringComparison.Ordinal))
        .ToHashSet(StringComparer.Ordinal);

      var projectNodes = projectIds
        .Select(projectId =>
        {
          var topFiles = filesByParent.TryGetValue(projectId, out var fs)
            ? fs : new List<ProvenanceIcicleNode>();
          var topDirs = subdirsByParent.TryGetValue(projectId, out var ds)
            ? ds : new List<string>();

          var acc = new NodeCounts();
          foreach (var f in topFiles) acc = acc.Add(f);
          foreach (var d in topDirs) acc = acc.Add(dirAggregates[d]);

          return new ProvenanceIcicleNode
          {
            Id = projectId,
            ParentId = string.Empty,
            Label = projectId,
            Level = "Project",
            TotalLines = acc.Total,
            AnyCovered = acc.Any,
            UnitCovered = acc.Unit,
            IntegrationCovered = acc.Integration,
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
  /// Aggregator-friendly four-tuple of line counts, used during the
  /// bottom-up directory roll-up. Records the same four numbers
  /// surfaced on the output <see cref="ProvenanceIcicleNode"/>.
  /// </summary>
  private readonly record struct NodeCounts(int Total, int Any, int Unit, int Integration)
  {
    public NodeCounts Add(ProvenanceIcicleNode n) => new(
      Total + n.TotalLines,
      Any + n.AnyCovered,
      Unit + n.UnitCovered,
      Integration + n.IntegrationCovered
    );

    public NodeCounts Add(NodeCounts other) => new(
      Total + other.Total,
      Any + other.Any,
      Unit + other.Unit,
      Integration + other.Integration
    );
  }

  // ── Path / id helpers ──────────────────────────────────────────────

  private static (string ParentId, string Label) ResolveFileParent(string srcPackage, string relativePath)
  {
    var lastSlash = relativePath.LastIndexOf('/');
    if (lastSlash < 0)
      return (srcPackage, relativePath);
    var dirPath = relativePath[..lastSlash];
    var fileName = relativePath[(lastSlash + 1)..];
    return ($"{srcPackage}::{dirPath}/", fileName);
  }

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
      if (idx < 0) return;
      if (directoryParents.ContainsKey(current)) return;

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

  private static string? TryCanonicalisePath(string srcPackage, string sourceFile)
  {
    var marker = $"/{srcPackage}/";
    var idx = sourceFile.LastIndexOf(marker, StringComparison.Ordinal);
    return idx >= 0 ? sourceFile[(idx + marker.Length)..] : null;
  }

  private static string ShortClassName(string fullyQualified)
  {
    if (string.IsNullOrEmpty(fullyQualified))
      return string.Empty;
    var lastDot = fullyQualified.LastIndexOf('.');
    return lastDot >= 0 ? fullyQualified[(lastDot + 1)..] : fullyQualified;
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="BuildProvenanceIcicleStep"/>.</summary>
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
      string testProject
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

    [FUnitStepTest(typeof(BuildProvenanceIcicleStep))]
    public void RobustlyCovered_ByUnitAndIntegration_BothCountsIncrement()
    {
      // Line 1 hit by both the unit test (Pkg.Tests) and an integration source (ExA).
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", 1, 1, "Pkg.Tests"),
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", 1, 1, "ExA"),
      };
      var manifest = new[]
      {
        Manifest("Pkg", "Library"),
        Manifest("Pkg.Tests", "LibraryTest"),
        Manifest("ExA", "Example"),
      };

      var method = Invoke(BuildProvenanceIcicleStep.Create(), (rows, manifest))
        .Single(n => n.Level == "Method");

      Assert.That(method.TotalLines, Is.EqualTo(1));
      Assert.That(method.AnyCovered, Is.EqualTo(1));
      Assert.That(method.UnitCovered, Is.EqualTo(1));
      Assert.That(method.IntegrationCovered, Is.EqualTo(1));
    }

    [FUnitStepTest(typeof(BuildProvenanceIcicleStep))]
    public void UnitOnly_LineCountsAsUnitButNotIntegration()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", 1, 1, "Pkg.Tests"),
      };
      var manifest = new[]
      {
        Manifest("Pkg", "Library"),
        Manifest("Pkg.Tests", "LibraryTest"),
      };

      var method = Invoke(BuildProvenanceIcicleStep.Create(), (rows, manifest))
        .Single(n => n.Level == "Method");

      Assert.That(method.AnyCovered, Is.EqualTo(1));
      Assert.That(method.UnitCovered, Is.EqualTo(1));
      Assert.That(method.IntegrationCovered, Is.EqualTo(0));
    }

    [FUnitStepTest(typeof(BuildProvenanceIcicleStep))]
    public void IntegrationOnly_LineCountsAsIntegrationButNotUnit()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", 1, 1, "ExA"),
      };
      var manifest = new[]
      {
        Manifest("Pkg", "Library"),
        Manifest("ExA", "Example"),
      };

      var method = Invoke(BuildProvenanceIcicleStep.Create(), (rows, manifest))
        .Single(n => n.Level == "Method");

      Assert.That(method.AnyCovered, Is.EqualTo(1));
      Assert.That(method.UnitCovered, Is.EqualTo(0));
      Assert.That(method.IntegrationCovered, Is.EqualTo(1));
    }

    [FUnitStepTest(typeof(BuildProvenanceIcicleStep))]
    public void PeerOnly_AnyCovered_ButNeitherUnitNorIntegration()
    {
      // Hit only by a peer test project (manifest type = LibraryTest,
      // but name doesn't match the unit-test convention for Pkg).
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", 1, 1, "OtherPkg.Tests"),
      };
      var manifest = new[]
      {
        Manifest("Pkg", "Library"),
        Manifest("OtherPkg.Tests", "LibraryTest"),
      };

      var method = Invoke(BuildProvenanceIcicleStep.Create(), (rows, manifest))
        .Single(n => n.Level == "Method");

      Assert.That(method.AnyCovered, Is.EqualTo(1));
      Assert.That(method.UnitCovered, Is.EqualTo(0));
      Assert.That(method.IntegrationCovered, Is.EqualTo(0));
    }

    [FUnitStepTest(typeof(BuildProvenanceIcicleStep))]
    public void Uncovered_AllCountsZero_ExceptTotal()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", 1, 0, "Pkg.Tests"),
      };
      var manifest = new[]
      {
        Manifest("Pkg", "Library"),
        Manifest("Pkg.Tests", "LibraryTest"),
      };

      var method = Invoke(BuildProvenanceIcicleStep.Create(), (rows, manifest))
        .Single(n => n.Level == "Method");

      Assert.That(method.TotalLines, Is.EqualTo(1));
      Assert.That(method.AnyCovered, Is.EqualTo(0));
      Assert.That(method.UnitCovered, Is.EqualTo(0));
      Assert.That(method.IntegrationCovered, Is.EqualTo(0));
    }

    [FUnitStepTest(typeof(BuildProvenanceIcicleStep))]
    public void DirectoryRollup_SumsCountsAcrossDescendants()
    {
      var rows = new[]
      {
        // Sub/Top.cs: 1 unit-only line + 1 uncovered line
        Row("Pkg", "/repo/src/Pkg/Sub/Top.cs", "Pkg.Sub.Top", "M", "()", 1, 1, "Pkg.Tests"),
        Row("Pkg", "/repo/src/Pkg/Sub/Top.cs", "Pkg.Sub.Top", "M", "()", 2, 0, "Pkg.Tests"),
        // Sub/Inner/Deep.cs: 1 integration-only line
        Row("Pkg", "/repo/src/Pkg/Sub/Inner/Deep.cs", "Pkg.Sub.Inner.Deep", "M", "()", 1, 1, "ExA"),
      };
      var manifest = new[]
      {
        Manifest("Pkg", "Library"),
        Manifest("Pkg.Tests", "LibraryTest"),
        Manifest("ExA", "Example"),
      };

      var sub = Invoke(BuildProvenanceIcicleStep.Create(), (rows, manifest))
        .Single(n => n.Level == "Directory" && n.Label == "Sub");

      Assert.That(sub.TotalLines, Is.EqualTo(3));
      Assert.That(sub.AnyCovered, Is.EqualTo(2));
      Assert.That(sub.UnitCovered, Is.EqualTo(1));
      Assert.That(sub.IntegrationCovered, Is.EqualTo(1));
    }

    [FUnitStepTest(typeof(BuildProvenanceIcicleStep))]
    public void ParentIds_FormValidTree()
    {
      var rows = new[]
      {
        Row("Pkg", "/repo/src/Pkg/A.cs", "Pkg.A", "M", "()", 1, 1, "Pkg.Tests"),
        Row("Pkg", "/repo/src/Pkg/Sub/Inner/B.cs", "Pkg.Sub.Inner.B", "M", "()", 1, 1, "Pkg.Tests"),
      };
      var manifest = new[]
      {
        Manifest("Pkg", "Library"),
        Manifest("Pkg.Tests", "LibraryTest"),
      };

      var nodes = Invoke(BuildProvenanceIcicleStep.Create(), (rows, manifest)).ToList();
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
