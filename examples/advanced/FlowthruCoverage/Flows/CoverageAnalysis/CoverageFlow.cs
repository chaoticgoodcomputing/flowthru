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
    // Every step here is a [FlowthruStep]-attributed pure transform
    // over CSV-backed inputs/outputs, so the framework caches them
    // automatically: CodeVersion is auto-resolved by the FlowBuilder
    // from the transform delegate's enclosing class (Phase 8), leaf
    // fingerprints come from FileStorageMedium (Phase 3). A second
    // invocation with unchanged Cobertura input reports is a no-op.
    return FlowBuilder.CreateFlow("Coverage", pipeline =>
    {
      pipeline.AddStep<DirectoryOf<CoberturaReport>, IEnumerable<LineCoverageRow>>(
        label: "FlattenCobertura",
        transform: FlattenCoberturaStep.Create(),
        inputs: catalog.CoverageXmlFiles,
        outputs: catalog.LineCoverage
      );

      pipeline.AddStep<IEnumerable<LineCoverageRow>, IEnumerable<PackageCoverageRow>>(
        label: "AggregateCoverage",
        transform: AggregateCoverageStep.Create(),
        inputs: catalog.LineCoverage,
        outputs: catalog.PackageCoverage
      );

      pipeline.AddStep<IEnumerable<LineCoverageRow>, IEnumerable<LineCoverageRow>>(
        label: "FilterCompilerGenerated",
        transform: FilterCompilerGeneratedStep.Create(),
        inputs: catalog.LineCoverage,
        outputs: catalog.MethodLineCoverage
      );

      pipeline.AddStep<IEnumerable<LineCoverageRow>, IEnumerable<PackageCoverageReport>>(
        label: "BuildMethodCoverage",
        transform: BuildMethodCoverageStep.Create(),
        inputs: catalog.MethodLineCoverage,
        outputs: catalog.MethodCoverage
      );

      pipeline.AddStep<
        IEnumerable<PackageCoverageReport>,
        IEnumerable<ProjectManifestEntry>,
        IEnumerable<MethodHitSummaryRow>
      >(
        label: "BuildMethodHitSummary",
        transform: BuildMethodHitSummaryStep.Create(),
        inputs: (catalog.MethodCoverage, catalog.ProjectManifest),
        outputs: catalog.MethodHitSummary
      );

      pipeline.AddStep<
        IEnumerable<PackageCoverageReport>,
        IEnumerable<ProjectManifestEntry>,
        IEnumerable<MethodHitSummaryRow>
      >(
        label: "BuildMethodNameSummary",
        transform: BuildMethodNameSummaryStep.Create(),
        inputs: (catalog.MethodCoverage, catalog.ProjectManifest),
        outputs: catalog.MethodNameSummary
      );
    });
  }
}
