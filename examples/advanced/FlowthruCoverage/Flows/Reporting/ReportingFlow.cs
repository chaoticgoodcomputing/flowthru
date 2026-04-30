using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;
using FlowthruCoverage.Data;
using FlowthruCoverage.Data._03_Primary.Schemas;
using FlowthruCoverage.Flows.Reporting.Steps;

namespace FlowthruCoverage.Flows.Reporting;

/// <summary>
/// Reporting pipeline:
/// 1. <see cref="ClassifyCoverageStep"/> (C#) annotates each aggregate row with its heatmap
///    section (Library Tests / Integration Tests / Examples) and writes the ordered CSV.
/// 2. <see cref="generate_coverage_heatmap"/> (Python/Plotly) reads that CSV and produces
///    the PNG heatmap.
/// 3. <see cref="AggregatePackageCoverageStep"/> rolls up the per-(TestProject, SrcPackage)
///    pivot to per-SrcPackage with MAX coverage, written to
///    <c>_04_Reporting/Datasets/package_coverage_max.csv</c>.
/// 4. Two filter steps extract zero-hit methods from the primary summaries; their output is
///    then passed through <see cref="FilterRemoteSourceFilesStep"/> to drop rows whose
///    <c>SourceFile</c> resolves to a remote SourceLink URL, and written to
///    <c>_04_Reporting/Datasets/uncovered_method_hits.csv</c> and
///    <c>_04_Reporting/Datasets/uncovered_method_names.csv</c>.
/// </summary>
public static class ReportingFlow
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "ClassifyCoverage",
        transform: ClassifyCoverageStep.Create(),
        input: (catalog.PackageCoverage, catalog.ProjectManifest),
        output: catalog.PivotCoverage,
        description: "Classifies and filters coverage rows using the project manifest: assigns Section/Subgroup to test columns, filters SrcPackage rows to manifest Library entries only, and sorts into display order."
      );

      pipeline.AddPythonStep(
        label: "GenerateCoverageHeatmap",
        description: "Plotly PNG heatmap: test projects × src packages, colour = CoveragePercent. Sections and ordering driven by the Section column from ClassifyCoverage.",
        module: "Flows.Reporting.Steps.generate_coverage_heatmap",
        function: "generate_coverage_heatmap",
        input: catalog.PivotCoverage,
        output: catalog.CoverageHeatmap,
        executor: executor
      );

      pipeline.AddStep(
        label: "AggregatePackageCoverage",
        description: "Rolls up per-(TestProject, SrcPackage) pivot rows to per-SrcPackage rows with MAX coverage. Avoids the multi-test-project double-count drag (e.g. SourceGenerators reading 0% in Core.Tests AND 74.41% in SourceGenerators.Tests).",
        transform: AggregatePackageCoverageStep.Create(),
        input: catalog.PivotCoverage,
        output: catalog.PackageCoverageMax
      );

      Func<IEnumerable<MethodHitSummaryRow>, IEnumerable<MethodHitSummaryRow>> filterUncovered =
        rows => rows.Where(r => r.TotalHits == 0);

      pipeline.AddStep(
        label: "FilterUncoveredMethodHits",
        description: "Filters the full-signature method hit summary to rows with TotalHits == 0.",
        transform: filterUncovered,
        input: catalog.MethodHitSummary,
        output: catalog.UncoveredMethodHitsRaw
      );

      pipeline.AddStep(
        label: "FilterUncoveredMethodNames",
        description: "Filters the method-name summary (overloads collapsed) to rows with TotalHits == 0.",
        transform: filterUncovered,
        input: catalog.MethodNameSummary,
        output: catalog.UncoveredMethodNamesRaw
      );

      pipeline.AddStep(
        label: "FilterRemoteSourceFilesHits",
        description: "Drops rows whose SourceFile is a remote SourceLink URL (https://...) — those resolve to NuGet-cached commit-pinned snapshots and add no analytical value to the published report.",
        transform: FilterRemoteSourceFilesStep.Create(),
        input: catalog.UncoveredMethodHitsRaw,
        output: catalog.UncoveredMethodHits
      );

      pipeline.AddStep(
        label: "FilterRemoteSourceFilesNames",
        description: "Drops method-name rows whose SourceFile is a remote SourceLink URL — same rationale as FilterRemoteSourceFilesHits.",
        transform: FilterRemoteSourceFilesStep.Create(),
        input: catalog.UncoveredMethodNamesRaw,
        output: catalog.UncoveredMethodNames
      );
    });
  }
}
