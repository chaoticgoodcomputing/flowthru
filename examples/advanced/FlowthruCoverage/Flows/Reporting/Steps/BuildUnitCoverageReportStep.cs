using System.Globalization;
using System.Text;
using Flowthru.Step;
using FlowthruCoverage.Data._04_Reporting.Schemas;
#if FUNIT_ENABLED
using Flowthru.Step.Testing;
#endif

namespace FlowthruCoverage.Flows.Reporting.Steps;

/// <summary>
/// Populates the unit-coverage markdown template with computed data
/// fragments — a scoreboard of every library, drill-downs for failing
/// libraries, and a per-library method checklist split into "quick wins"
/// (exercised by integration but not unit) and "cold spots" (no coverage
/// anywhere).
/// </summary>
/// <remarks>
/// <para>
/// Instructions and structure live in the template
/// (<see cref="FlowthruCoverage.Data.Catalog.UnitCoverageReportTemplate"/>),
/// not here — this step is a pure data renderer. Tokens of the form
/// <c>{{name}}</c> are replaced by the values in <see cref="Substitutions"/>;
/// any token the template doesn't reference is silently ignored, and any
/// unknown token in the template is left untouched.
/// </para>
/// <para>
/// Sort metric throughout is <em>lines-to-threshold</em>
/// (<c>max(0, threshold × Total − UnitCovered)</c>) — a 50%-covered 200-line
/// library is less urgent than a 75%-covered 4 000-line library, and the
/// report should reflect that.
/// </para>
/// </remarks>
[FlowthruStep]
public static class BuildUnitCoverageReportStep
{
  /// <summary>
  /// Configuration options for the unit-coverage report. Bound from
  /// <c>Flowthru:Flows:Reporting:UnitCoverageReportOptions</c> via the
  /// catalog, so changing the threshold in <c>appsettings.json</c>
  /// invalidates this step's cached output.
  /// </summary>
  public record Options
  {
    /// <summary>Unit-coverage threshold below which a library is flagged.</summary>
    public double UnitThreshold { get; init; } = 0.80;
  }

  /// <summary>Token names this step knows how to substitute in the template.</summary>
  public static readonly IReadOnlyList<string> SupportedTokens = new[]
  {
    "threshold_pct",
    "total_libraries",
    "failing_count",
    "quick_wins_count",
    "cold_spots_count",
    "scoreboard_table",
    "drilldown_sections",
    "quick_wins_sections",
    "cold_spots_sections",
  };

  public static Func<
    (IEnumerable<ProvenanceIcicleNode>, string, Options),
    byte[]
  > Create()
  {
    return inputs =>
    {
      var (nodes, template, options) = inputs;
      var threshold = options.UnitThreshold;
      var all = nodes.ToList();
      var projects = all
        .Where(n => n.Level == "Project" && n.TotalLines > 0)
        .OrderByDescending(n => LinesToThreshold(n, threshold))
        .ThenBy(p => p.Id, StringComparer.Ordinal)
        .ToList();

      var methodsNeedingUnit = all
        .Where(n => n.Level == "Method" && n.TotalLines > 0 && n.UnitCovered == 0)
        .ToList();
      var quickWins = methodsNeedingUnit.Where(m => m.IntegrationCovered > 0).ToList();
      var coldSpots = methodsNeedingUnit.Where(m => m.IntegrationCovered == 0).ToList();

      var subs = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["threshold_pct"] = ((int)Math.Round(threshold * 100))
          .ToString(CultureInfo.InvariantCulture),
        ["total_libraries"] = projects.Count.ToString(CultureInfo.InvariantCulture),
        ["failing_count"] = projects.Count(p => IsFailing(p, threshold)).ToString(CultureInfo.InvariantCulture),
        ["quick_wins_count"] = quickWins.Count.ToString(CultureInfo.InvariantCulture),
        ["cold_spots_count"] = coldSpots.Count.ToString(CultureInfo.InvariantCulture),
        ["scoreboard_table"] = RenderScoreboardTable(projects, threshold),
        ["drilldown_sections"] = RenderDrilldownSections(all, projects, threshold),
        ["quick_wins_sections"] = RenderMethodSections(quickWins, projects),
        ["cold_spots_sections"] = RenderMethodSections(coldSpots, projects),
      };

      return Encoding.UTF8.GetBytes(Substitute(template, subs));
    };
  }

  // ── Substitution ───────────────────────────────────────────────────

  internal static string Substitute(string template, IReadOnlyDictionary<string, string> subs)
  {
    var output = template;
    foreach (var (token, value) in subs)
      output = output.Replace("{{" + token + "}}", value);
    return output;
  }

  // ── Metric helpers ─────────────────────────────────────────────────

  private static int LinesToThreshold(ProvenanceIcicleNode n, double threshold) =>
    n.TotalLines <= 0
      ? 0
      : (int)Math.Ceiling(Math.Max(0.0, threshold * n.TotalLines - n.UnitCovered));

  private static bool IsFailing(ProvenanceIcicleNode n, double threshold) =>
    n.TotalLines > 0 && (double)n.UnitCovered / n.TotalLines < threshold;

  private static double Pct(int covered, int total) =>
    total > 0 ? 100.0 * covered / total : 0.0;

  private static string FormatPct(double pct) =>
    pct.ToString("0.0", CultureInfo.InvariantCulture) + "%";

  private static string FormatLines(int lines) =>
    lines.ToString("N0", CultureInfo.InvariantCulture);

  // ── Section renderers ──────────────────────────────────────────────

  private static string RenderScoreboardTable(List<ProvenanceIcicleNode> projects, double threshold)
  {
    if (projects.Count == 0)
      return "_No src libraries found._";

    var sb = new StringBuilder();
    sb.AppendLine("| Library | Lines | Unit % | Integration % | Any % | Lines to threshold |");
    sb.AppendLine("|---|---:|---:|---:|---:|---:|");

    foreach (var p in projects)
    {
      var pass = !IsFailing(p, threshold);
      var label = pass ? $"✓ {p.Label}" : $"**{p.Label}**";
      var gap = pass ? "—" : FormatLines(LinesToThreshold(p, threshold));
      sb.AppendLine(
        $"| {label} " +
        $"| {FormatLines(p.TotalLines)} " +
        $"| {FormatPct(Pct(p.UnitCovered, p.TotalLines))} " +
        $"| {FormatPct(Pct(p.IntegrationCovered, p.TotalLines))} " +
        $"| {FormatPct(Pct(p.AnyCovered, p.TotalLines))} " +
        $"| {gap} |"
      );
    }
    return sb.ToString().TrimEnd();
  }

  private static string RenderDrilldownSections(
    List<ProvenanceIcicleNode> all,
    List<ProvenanceIcicleNode> projects,
    double threshold
  )
  {
    var failing = projects.Where(p => IsFailing(p, threshold)).ToList();
    if (failing.Count == 0)
      return "_All libraries pass the unit-coverage threshold. 🎉_";

    var sb = new StringBuilder();
    foreach (var project in failing)
    {
      sb.AppendLine($"### {project.Label}");
      sb.AppendLine();
      sb.AppendLine(
        $"Unit: **{FormatPct(Pct(project.UnitCovered, project.TotalLines))}** " +
        $"({FormatLines(project.UnitCovered)} / {FormatLines(project.TotalLines)} lines). " +
        $"Needs **{FormatLines(LinesToThreshold(project, threshold))}** more lines covered to hit threshold."
      );
      sb.AppendLine();

      var idPrefix = project.Id + "::";
      var subtrees = all
        .Where(n =>
          (n.Level == "Directory" || n.Level == "File")
          && n.Id.StartsWith(idPrefix, StringComparison.Ordinal)
          && IsFailing(n, threshold)
        )
        .OrderByDescending(n => LinesToThreshold(n, threshold))
        .ThenBy(n => n.Id, StringComparer.Ordinal)
        .Take(10)
        .ToList();

      if (subtrees.Count == 0)
      {
        sb.AppendLine(
          "_No directory or file under this library is itself below threshold — " +
          "the gap is spread across many small sites._"
        );
        sb.AppendLine();
        continue;
      }

      sb.AppendLine("| Path | Level | Lines | Unit % | Lines to threshold |");
      sb.AppendLine("|---|---|---:|---:|---:|");
      foreach (var st in subtrees)
      {
        var rel = st.Id[idPrefix.Length..].TrimEnd('/');
        sb.AppendLine(
          $"| `{rel}` " +
          $"| {st.Level} " +
          $"| {FormatLines(st.TotalLines)} " +
          $"| {FormatPct(Pct(st.UnitCovered, st.TotalLines))} " +
          $"| {FormatLines(LinesToThreshold(st, threshold))} |"
        );
      }
      sb.AppendLine();
    }
    return sb.ToString().TrimEnd();
  }

  private static string RenderMethodSections(
    List<ProvenanceIcicleNode> methods,
    List<ProvenanceIcicleNode> projects
  )
  {
    if (methods.Count == 0)
      return "_None._";

    var byProject = methods
      .GroupBy(m => GetProjectId(m.Id), StringComparer.Ordinal)
      .ToDictionary(
        g => g.Key,
        g => g.OrderByDescending(n => n.TotalLines)
          .ThenBy(n => n.Id, StringComparer.Ordinal)
          .ToList(),
        StringComparer.Ordinal
      );

    var sb = new StringBuilder();
    foreach (var project in projects.OrderBy(p => p.Id, StringComparer.Ordinal))
    {
      if (!byProject.TryGetValue(project.Id, out var methodList))
        continue;

      sb.AppendLine($"#### {project.Label} ({methodList.Count})");
      sb.AppendLine();
      foreach (var m in methodList)
      {
        var path = ExtractRelativePath(m.Id);
        sb.AppendLine(
          $"- `{path}` → **{m.Label}** ({FormatLines(m.TotalLines)} lines)"
        );
      }
      sb.AppendLine();
    }
    return sb.ToString().TrimEnd();
  }

  // Method id shape: "{srcPackage}::{relativePath}::{className}.{method}{signature}"
  private static string GetProjectId(string methodId)
  {
    var idx = methodId.IndexOf("::", StringComparison.Ordinal);
    return idx < 0 ? methodId : methodId[..idx];
  }

  private static string ExtractRelativePath(string methodId)
  {
    var first = methodId.IndexOf("::", StringComparison.Ordinal);
    if (first < 0) return string.Empty;
    var rest = methodId[(first + 2)..];
    var second = rest.IndexOf("::", StringComparison.Ordinal);
    return second < 0 ? rest : rest[..second];
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="BuildUnitCoverageReportStep"/>.</summary>
  public class Tests : FUnitContext
  {
    // A minimal template that echoes each placeholder with a distinctive
    // label so tests can assert on substitution without depending on the
    // production template's prose.
    private const string EchoTemplate =
      "threshold={{threshold_pct}}\n" +
      "total={{total_libraries}}\n" +
      "failing={{failing_count}}\n" +
      "qw_count={{quick_wins_count}}\n" +
      "cs_count={{cold_spots_count}}\n" +
      "SCOREBOARD:\n{{scoreboard_table}}\n" +
      "DRILLDOWN:\n{{drilldown_sections}}\n" +
      "QUICK_WINS:\n{{quick_wins_sections}}\n" +
      "COLD_SPOTS:\n{{cold_spots_sections}}\n";

    private static ProvenanceIcicleNode Node(
      string id,
      string parentId,
      string label,
      string level,
      int total,
      int any,
      int unit,
      int integration
    ) => new()
    {
      Id = id,
      ParentId = parentId,
      Label = label,
      Level = level,
      TotalLines = total,
      AnyCovered = any,
      UnitCovered = unit,
      IntegrationCovered = integration,
    };

    private string Run(IEnumerable<ProvenanceIcicleNode> nodes, string template = EchoTemplate) =>
      Encoding.UTF8.GetString(Invoke(BuildUnitCoverageReportStep.Create(), (nodes, template, new Options())));

    [FUnitStepTest(typeof(BuildUnitCoverageReportStep))]
    public void PassingLibrary_ScoreboardHasCheckmark_DrilldownIsEmpty()
    {
      var nodes = new[]
      {
        Node("Pkg", "", "Pkg", "Project", total: 100, any: 90, unit: 85, integration: 50),
      };

      var md = Run(nodes);

      Assert.That(md, Does.Contain("✓ Pkg"));
      Assert.That(md, Does.Contain("All libraries pass"));
      Assert.That(md, Does.Not.Contain("### Pkg")); // no drill-down heading
      Assert.That(md, Does.Contain("failing=0"));
    }

    [FUnitStepTest(typeof(BuildUnitCoverageReportStep))]
    public void FailingLibrary_AppearsInScoreboardAndDrilldown()
    {
      // 100 lines, 50 unit-covered → 50% < 80%, needs 30 more.
      var nodes = new[]
      {
        Node("Pkg", "", "Pkg", "Project", total: 100, any: 80, unit: 50, integration: 60),
      };

      var md = Run(nodes);

      Assert.That(md, Does.Contain("**Pkg**"));   // bolded in scoreboard
      Assert.That(md, Does.Contain("### Pkg"));   // drill-down header
      Assert.That(md, Does.Contain("30"));        // 30 lines to threshold
      Assert.That(md, Does.Contain("failing=1"));
    }

    [FUnitStepTest(typeof(BuildUnitCoverageReportStep))]
    public void QuickWin_GoesToQuickWinsBucket_NotColdSpots()
    {
      var nodes = new[]
      {
        Node("Pkg", "", "Pkg", "Project", total: 10, any: 10, unit: 0, integration: 10),
        Node("Pkg::A.cs", "Pkg", "A.cs", "File", total: 10, any: 10, unit: 0, integration: 10),
        Node(
          "Pkg::A.cs::A.QuickWin()",
          "Pkg::A.cs",
          "A.QuickWin()",
          "Method",
          total: 10, any: 10, unit: 0, integration: 10
        ),
      };

      var md = Run(nodes);

      Assert.That(md, Does.Contain("qw_count=1"));
      Assert.That(md, Does.Contain("cs_count=0"));
      // The method must appear after the QUICK_WINS marker, not after COLD_SPOTS.
      var qwIdx = md.IndexOf("QUICK_WINS:", StringComparison.Ordinal);
      var csIdx = md.IndexOf("COLD_SPOTS:", StringComparison.Ordinal);
      var methodIdx = md.IndexOf("A.QuickWin()", StringComparison.Ordinal);
      Assert.That(methodIdx, Is.GreaterThan(qwIdx).And.LessThan(csIdx));
    }

    [FUnitStepTest(typeof(BuildUnitCoverageReportStep))]
    public void ColdSpot_GoesToColdSpotsBucket_NotQuickWins()
    {
      var nodes = new[]
      {
        Node("Pkg", "", "Pkg", "Project", total: 5, any: 0, unit: 0, integration: 0),
        Node("Pkg::A.cs", "Pkg", "A.cs", "File", total: 5, any: 0, unit: 0, integration: 0),
        Node(
          "Pkg::A.cs::A.ColdSpot()",
          "Pkg::A.cs",
          "A.ColdSpot()",
          "Method",
          total: 5, any: 0, unit: 0, integration: 0
        ),
      };

      var md = Run(nodes);

      Assert.That(md, Does.Contain("qw_count=0"));
      Assert.That(md, Does.Contain("cs_count=1"));
      var csIdx = md.IndexOf("COLD_SPOTS:", StringComparison.Ordinal);
      var methodIdx = md.IndexOf("A.ColdSpot()", StringComparison.Ordinal);
      Assert.That(methodIdx, Is.GreaterThan(csIdx));
    }

    [FUnitStepTest(typeof(BuildUnitCoverageReportStep))]
    public void UnitCoveredMethod_AppearsInNeitherBucket()
    {
      var nodes = new[]
      {
        Node("Pkg", "", "Pkg", "Project", total: 10, any: 10, unit: 10, integration: 0),
        Node("Pkg::A.cs", "Pkg", "A.cs", "File", total: 10, any: 10, unit: 10, integration: 0),
        Node(
          "Pkg::A.cs::A.Covered()",
          "Pkg::A.cs",
          "A.Covered()",
          "Method",
          total: 10, any: 10, unit: 10, integration: 0
        ),
      };

      var md = Run(nodes);

      Assert.That(md, Does.Contain("qw_count=0"));
      Assert.That(md, Does.Contain("cs_count=0"));
      Assert.That(md, Does.Not.Contain("A.Covered()"));
    }

    [FUnitStepTest(typeof(BuildUnitCoverageReportStep))]
    public void EmptyInput_ProducesValidReport_WithNoLibrariesFoundMessage()
    {
      var md = Run(Array.Empty<ProvenanceIcicleNode>());

      Assert.That(md, Does.Contain("total=0"));
      Assert.That(md, Does.Contain("No src libraries found"));
    }

    [FUnitStepTest(typeof(BuildUnitCoverageReportStep))]
    public void UnknownToken_IsLeftUntouched()
    {
      const string template = "Known: {{threshold_pct}} — Unknown: {{not_a_real_token}}";
      var md = Run(Array.Empty<ProvenanceIcicleNode>(), template);

      Assert.That(md, Does.Contain("Known: 80"));
      Assert.That(md, Does.Contain("{{not_a_real_token}}"));
    }
  }
#endif
}
