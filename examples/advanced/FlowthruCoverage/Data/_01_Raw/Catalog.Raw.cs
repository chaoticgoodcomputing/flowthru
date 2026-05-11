using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using FlowthruCoverage.Data._01_Raw.Schemas;

namespace FlowthruCoverage.Data;

public partial class Catalog
{
  /// <summary>Staged Cobertura XML files, one per test or example project.</summary>
  public IItem<DirectoryOf<CoberturaReport>> CoverageXmlFiles =>
    CreateItem(() =>
      Item.Of<DirectoryOf<CoberturaReport>>("CoverageXmlFiles")
        .Directory(file => file.Xml())
        .AtPath($"{_basePath}/_01_Raw/Datasets")
        .Build()
    );

  /// <summary>Repository project manifest mapping assemblies to ProjectType and Subgroup.</summary>
  public IItem<IEnumerable<ProjectManifestEntry>> ProjectManifest =>
    CreateItem(() =>
      Item.Of<IEnumerable<ProjectManifestEntry>>("ProjectManifest")
        .Csv()
        .AtPath($"{_basePath}/_01_Raw/Datasets/project_manifest.csv")
        .Build()
    );

  /// <summary>
  /// Markdown template for the unit-coverage report — agent instructions and
  /// report structure live here, separate from the pipeline source. The step
  /// substitutes <c>{{token}}</c> placeholders with computed data fragments.
  /// </summary>
  public IItem<string> UnitCoverageReportTemplate =>
    CreateItem(() =>
      Item.Of<string>("UnitCoverageReportTemplate")
        .Text()
        .AtPath($"{_basePath}/_01_Raw/Templates/unit_coverage_report.md")
        .Build()
    );
}
