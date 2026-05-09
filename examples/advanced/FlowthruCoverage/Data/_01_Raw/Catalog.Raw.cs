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
}
