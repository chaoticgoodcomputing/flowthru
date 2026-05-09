using Flowthru.Data.Storage;
using Flowthru.Flow;
using FlowthruCoverage.Data;
using FlowthruCoverage.Data._01_Raw.Schemas;
using FlowthruCoverage.Data._02_Intermediate.Schemas;
using FlowthruCoverage.Data._03_Primary.Schemas;
using FlowthruCoverage.Flows.Coverage.Steps;

namespace FlowthruCoverage.Flows.Coverage;

/// <summary>Processes staged Cobertura XML files into pivot-ready coverage aggregates.</summary>
public static class CoverageAnalysisFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("Coverage", pipeline =>
    {
      pipeline.AddStep<DirectoryOf<CoberturaReport>, IEnumerable<LineCoverageRow>>(
        label: "FlattenCobertura",
        transform: FlattenCoberturaStep.Create(),
        input1: catalog.CoverageXmlFiles,
        output1: catalog.LineCoverage
      );

      pipeline.AddStep<IEnumerable<LineCoverageRow>, IEnumerable<PackageCoverageRow>>(
        label: "AggregateCoverage",
        transform: AggregateCoverageStep.Create(),
        input1: catalog.LineCoverage,
        output1: catalog.PackageCoverage
      );

      pipeline.AddStep<IEnumerable<LineCoverageRow>, IEnumerable<LineCoverageRow>>(
        label: "FilterCompilerGenerated",
        transform: FilterCompilerGeneratedStep.Create(),
        input1: catalog.LineCoverage,
        output1: catalog.MethodLineCoverage
      );

      pipeline.AddStep<IEnumerable<LineCoverageRow>, IEnumerable<PackageCoverageReport>>(
        label: "BuildMethodCoverage",
        transform: BuildMethodCoverageStep.Create(),
        input1: catalog.MethodLineCoverage,
        output1: catalog.MethodCoverage
      );

      pipeline.AddStep<
        IEnumerable<PackageCoverageReport>,
        IEnumerable<ProjectManifestEntry>,
        IEnumerable<MethodHitSummaryRow>
      >(
        label: "BuildMethodHitSummary",
        transform: BuildMethodHitSummaryStep.Create(),
        input1: catalog.MethodCoverage,
        input2: catalog.ProjectManifest,
        output1: catalog.MethodHitSummary
      );

      pipeline.AddStep<
        IEnumerable<PackageCoverageReport>,
        IEnumerable<ProjectManifestEntry>,
        IEnumerable<MethodHitSummaryRow>
      >(
        label: "BuildMethodNameSummary",
        transform: BuildMethodNameSummaryStep.Create(),
        input1: catalog.MethodCoverage,
        input2: catalog.ProjectManifest,
        output1: catalog.MethodNameSummary
      );
    });
  }
}
