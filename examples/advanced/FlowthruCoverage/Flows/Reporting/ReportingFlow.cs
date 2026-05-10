using Flowthru.Flow;
using Flowthru.Step.Python;
using FlowthruCoverage.Data;
using FlowthruCoverage.Data._01_Raw.Schemas;
using FlowthruCoverage.Data._02_Intermediate.Schemas;
using FlowthruCoverage.Data._03_Primary.Schemas;
using FlowthruCoverage.Data._04_Reporting.Schemas;
using FlowthruCoverage.Flows.Reporting.Steps;

namespace FlowthruCoverage.Flows.Reporting;

/// <summary>Reporting pipeline: classifies coverage rows, generates heatmap and icicle outputs.</summary>
public static class ReportingFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<PackageCoverageRow>,
        IEnumerable<ProjectManifestEntry>,
        IEnumerable<PivotCoverageRow>
      >(
        label: "ClassifyCoverage",
        transform: ClassifyCoverageStep.Create(),
        inputs: (catalog.PackageCoverage, catalog.ProjectManifest),
        outputs: catalog.PivotCoverage
      );

      pipeline.AddPythonStep(
        label: "GenerateCoverageHeatmap",
        module: "Flows.Reporting.Steps.generate_coverage_heatmap",
        function: "generate_coverage_heatmap",
        input: catalog.PivotCoverage,
        output: catalog.CoverageHeatmap,
        executor: executor
      );

      pipeline.AddStep<
        IEnumerable<LineCoverageRow>,
        IEnumerable<ProjectManifestEntry>,
        IEnumerable<IcicleCoverageNode>
      >(
        label: "BuildIcicleCoverage",
        transform: BuildIcicleCoverageStep.Create(),
        inputs: (catalog.MethodLineCoverage, catalog.ProjectManifest),
        outputs: catalog.IcicleCoverage
      );

      pipeline.AddPythonStep(
        label: "GenerateCoverageIcicle",
        module: "Flows.Reporting.Steps.generate_coverage_icicle",
        function: "generate_coverage_icicle",
        input: catalog.IcicleCoverage,
        output: catalog.CoverageIcicles,
        executor: executor
      );

      pipeline.AddStep<
        IEnumerable<LineCoverageRow>,
        IEnumerable<ProjectManifestEntry>,
        IEnumerable<LineCoverageRow>
      >(
        label: "FilterToExampleLineCoverage",
        transform: FilterLineCoverageByTestProjectTypeStep.Create("Example"),
        inputs: (catalog.MethodLineCoverage, catalog.ProjectManifest),
        outputs: catalog.ExampleMethodLineCoverage
      );

      pipeline.AddStep<
        IEnumerable<LineCoverageRow>,
        IEnumerable<ProjectManifestEntry>,
        IEnumerable<IcicleCoverageNode>
      >(
        label: "BuildExampleIcicleCoverage",
        transform: BuildIcicleCoverageStep.Create(),
        inputs: (catalog.ExampleMethodLineCoverage, catalog.ProjectManifest),
        outputs: catalog.ExampleIcicleCoverage
      );

      pipeline.AddPythonStep(
        label: "GenerateExampleCoverageIcicle",
        module: "Flows.Reporting.Steps.generate_coverage_icicle",
        function: "generate_example_coverage_icicle",
        input: catalog.ExampleIcicleCoverage,
        output: catalog.ExampleCoverageIcicles,
        executor: executor
      );

      pipeline.AddStep<IEnumerable<PivotCoverageRow>, IEnumerable<PackageCoverageMaxRow>>(
        label: "AggregatePackageCoverage",
        transform: AggregatePackageCoverageStep.Create(),
        inputs: catalog.PivotCoverage,
        outputs: catalog.PackageCoverageMax
      );

      Func<IEnumerable<MethodHitSummaryRow>, IEnumerable<MethodHitSummaryRow>> filterUncovered =
        rows => rows.Where(r => r.TotalHits == 0);

      pipeline.AddStep<IEnumerable<MethodHitSummaryRow>, IEnumerable<MethodHitSummaryRow>>(
        label: "FilterUncoveredMethodHits",
        transform: filterUncovered,
        inputs: catalog.MethodHitSummary,
        outputs: catalog.UncoveredMethodHitsRaw
      );

      pipeline.AddStep<IEnumerable<MethodHitSummaryRow>, IEnumerable<MethodHitSummaryRow>>(
        label: "FilterUncoveredMethodNames",
        transform: filterUncovered,
        inputs: catalog.MethodNameSummary,
        outputs: catalog.UncoveredMethodNamesRaw
      );

      pipeline.AddStep<IEnumerable<MethodHitSummaryRow>, IEnumerable<MethodHitSummaryRow>>(
        label: "FilterRemoteSourceFilesHits",
        transform: FilterRemoteSourceFilesStep.Create(),
        inputs: catalog.UncoveredMethodHitsRaw,
        outputs: catalog.UncoveredMethodHits
      );

      pipeline.AddStep<IEnumerable<MethodHitSummaryRow>, IEnumerable<MethodHitSummaryRow>>(
        label: "FilterRemoteSourceFilesNames",
        transform: FilterRemoteSourceFilesStep.Create(),
        inputs: catalog.UncoveredMethodNamesRaw,
        outputs: catalog.UncoveredMethodNames
      );
    });
  }
}
