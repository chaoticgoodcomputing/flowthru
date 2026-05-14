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
    // over CSV-backed inputs/outputs, so the framework can cache them:
    // CodeVersion comes from the source generator, leaf fingerprints
    // come from FileStorageMedium (Phase 3 of the smart-caching RFC).
    // A second invocation with unchanged Cobertura input reports is
    // a no-op at the framework level.
    return FlowBuilder.CreateFlow("Coverage", pipeline =>
    {
      pipeline.AddStep<DirectoryOf<CoberturaReport>, IEnumerable<LineCoverageRow>>(
        label: "FlattenCobertura",
        transform: FlattenCoberturaStep.Create(),
        inputs: catalog.CoverageXmlFiles,
        outputs: catalog.LineCoverage,
        codeVersion: FlattenCoberturaStep_Metadata.CodeVersion
      );

      pipeline.AddStep<IEnumerable<LineCoverageRow>, IEnumerable<PackageCoverageRow>>(
        label: "AggregateCoverage",
        transform: AggregateCoverageStep.Create(),
        inputs: catalog.LineCoverage,
        outputs: catalog.PackageCoverage,
        codeVersion: AggregateCoverageStep_Metadata.CodeVersion
      );

      pipeline.AddStep<IEnumerable<LineCoverageRow>, IEnumerable<LineCoverageRow>>(
        label: "FilterCompilerGenerated",
        transform: FilterCompilerGeneratedStep.Create(),
        inputs: catalog.LineCoverage,
        outputs: catalog.MethodLineCoverage,
        codeVersion: FilterCompilerGeneratedStep_Metadata.CodeVersion
      );

      pipeline.AddStep<IEnumerable<LineCoverageRow>, IEnumerable<PackageCoverageReport>>(
        label: "BuildMethodCoverage",
        transform: BuildMethodCoverageStep.Create(),
        inputs: catalog.MethodLineCoverage,
        outputs: catalog.MethodCoverage,
        codeVersion: BuildMethodCoverageStep_Metadata.CodeVersion
      );

      pipeline.AddStep<
        IEnumerable<PackageCoverageReport>,
        IEnumerable<ProjectManifestEntry>,
        IEnumerable<MethodHitSummaryRow>
      >(
        label: "BuildMethodHitSummary",
        transform: BuildMethodHitSummaryStep.Create(),
        inputs: (catalog.MethodCoverage, catalog.ProjectManifest),
        outputs: catalog.MethodHitSummary,
        codeVersion: BuildMethodHitSummaryStep_Metadata.CodeVersion
      );

      pipeline.AddStep<
        IEnumerable<PackageCoverageReport>,
        IEnumerable<ProjectManifestEntry>,
        IEnumerable<MethodHitSummaryRow>
      >(
        label: "BuildMethodNameSummary",
        transform: BuildMethodNameSummaryStep.Create(),
        inputs: (catalog.MethodCoverage, catalog.ProjectManifest),
        outputs: catalog.MethodNameSummary,
        codeVersion: BuildMethodNameSummaryStep_Metadata.CodeVersion
      );
    });
  }
}
