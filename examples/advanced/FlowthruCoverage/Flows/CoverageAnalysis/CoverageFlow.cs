using Flowthru.Core.Flows;
using FlowthruCoverage.Data;
using FlowthruCoverage.Flows.Coverage.Steps;

namespace FlowthruCoverage.Flows.Coverage;

/// <summary>
/// Processes staged Cobertura XML files into a pivot-ready coverage heatmap.
///
/// Prerequisites: run the <c>_stage-coverage-xml</c> NX target to populate
/// <c>Data/_01_Raw/Datasets/</c> with one <c>{ProjectName}.xml</c> per test project.
///
/// Outputs:
/// - <c>Data/_02_Intermediate/Datasets/line_coverage.csv</c> — line-level detail
/// - <c>Data/_03_Primary/Datasets/package_coverage.csv</c> — heatmap source (tidy format, per test project × package)
/// - <c>Data/_03_Primary/Datasets/method_coverage.json</c> — nested report: package → namespace → class → method → per-test-project hits
/// - <c>Data/_03_Primary/Datasets/method_hit_summary.csv</c> — flat per-method hit summary (full signature), ordered by subgroup then TotalHits ascending
/// - <c>Data/_03_Primary/Datasets/method_name_summary.csv</c> — same but ID uses method name only; overloads collapsed
/// </summary>
public static class CoverageAnalysisFlow
{
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "FlattenCobertura",
        description: "Deserializes each staged Cobertura XML and flattens it into line-level rows tagged with the test project name.",
        transform: FlattenCoberturaStep.Create(),
        input: catalog.CoverageXmlFiles,
        output: catalog.LineCoverage
      );

      pipeline.AddStep(
        label: "AggregateCoverage",
        description: "Groups line coverage by (TestProject, SrcPackage) and computes covered/total/percent. Output is tidy-format — pivot on TestProject vs SrcPackage for the heatmap.",
        transform: AggregateCoverageStep.Create(),
        input: catalog.LineCoverage,
        output: catalog.PackageCoverage
      );

      pipeline.AddStep(
        label: "FilterCompilerGenerated",
        description: "Drops compiler-synthesized line-coverage rows (async state machines, cached lambdas, display-class closures, lambda bodies) so the method-aggregation path reports only authored methods. Forks off LineCoverage; AggregateCoverage continues to consume the unfiltered item.",
        transform: FilterCompilerGeneratedStep.Create(),
        input: catalog.LineCoverage,
        output: catalog.MethodLineCoverage
      );

      pipeline.AddStep(
        label: "BuildMethodCoverage",
        description: "Groups line coverage into a nested package → namespace → class → method hierarchy. Each method leaf lists per-test-project hit counts. Output is one PackageCoverageReport per source assembly.",
        transform: BuildMethodCoverageStep.Create(),
        input: catalog.MethodLineCoverage,
        output: catalog.MethodCoverage
      );

      pipeline.AddStep(
        label: "BuildMethodHitSummary",
        description: "Flattens the nested method coverage report into a per-method summary row with a fully-qualified Id, Subgroup (from manifest), TotalHits, and ProjectHits. Ordered by subgroup (Core → Extensions → Misc) then TotalHits ascending.",
        transform: BuildMethodHitSummaryStep.Create(),
        input: (catalog.MethodCoverage, catalog.ProjectManifest),
        output: catalog.MethodHitSummary
      );

      pipeline.AddStep(
        label: "BuildMethodNameSummary",
        description: "Variant of BuildMethodHitSummary where the Id uses only the method name. Overloads sharing the same name are collapsed into one row: TotalHits summed, ProjectHits unioned across overloads. Same sort order: subgroup then TotalHits ascending.",
        transform: BuildMethodNameSummaryStep.Create(),
        input: (catalog.MethodCoverage, catalog.ProjectManifest),
        output: catalog.MethodNameSummary
      );
    });
  }
}
