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
        input1: catalog.PackageCoverage,
        input2: catalog.ProjectManifest,
        output1: catalog.PivotCoverage
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
        input1: catalog.MethodLineCoverage,
        input2: catalog.ProjectManifest,
        output1: catalog.IcicleCoverage
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
        input1: catalog.MethodLineCoverage,
        input2: catalog.ProjectManifest,
        output1: catalog.ExampleMethodLineCoverage
      );

      pipeline.AddStep<
        IEnumerable<LineCoverageRow>,
        IEnumerable<ProjectManifestEntry>,
        IEnumerable<IcicleCoverageNode>
      >(
        label: "BuildExampleIcicleCoverage",
        transform: BuildIcicleCoverageStep.Create(),
        input1: catalog.ExampleMethodLineCoverage,
        input2: catalog.ProjectManifest,
        output1: catalog.ExampleIcicleCoverage
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
        input1: catalog.PivotCoverage,
        output1: catalog.PackageCoverageMax
      );

      Func<IEnumerable<MethodHitSummaryRow>, IEnumerable<MethodHitSummaryRow>> filterUncovered =
        rows => rows.Where(r => r.TotalHits == 0);

      pipeline.AddStep<IEnumerable<MethodHitSummaryRow>, IEnumerable<MethodHitSummaryRow>>(
        label: "FilterUncoveredMethodHits",
        transform: filterUncovered,
        input1: catalog.MethodHitSummary,
        output1: catalog.UncoveredMethodHitsRaw
      );

      pipeline.AddStep<IEnumerable<MethodHitSummaryRow>, IEnumerable<MethodHitSummaryRow>>(
        label: "FilterUncoveredMethodNames",
        transform: filterUncovered,
        input1: catalog.MethodNameSummary,
        output1: catalog.UncoveredMethodNamesRaw
      );

      pipeline.AddStep<IEnumerable<MethodHitSummaryRow>, IEnumerable<MethodHitSummaryRow>>(
        label: "FilterRemoteSourceFilesHits",
        transform: FilterRemoteSourceFilesStep.Create(),
        input1: catalog.UncoveredMethodHitsRaw,
        output1: catalog.UncoveredMethodHits
      );

      pipeline.AddStep<IEnumerable<MethodHitSummaryRow>, IEnumerable<MethodHitSummaryRow>>(
        label: "FilterRemoteSourceFilesNames",
        transform: FilterRemoteSourceFilesStep.Create(),
        input1: catalog.UncoveredMethodNamesRaw,
        output1: catalog.UncoveredMethodNames
      );
    });
  }
}
