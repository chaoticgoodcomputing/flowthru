using FlowthruCoverage.Data._01_Raw.Schemas;
using Flowthru.Core.Data;

namespace FlowthruCoverage.Data;

public partial class Catalog
{
  /// <summary>
  /// Staged Cobertura XML files, one per test or example project.
  /// Run the <c>_stage-coverage-xml</c> NX target before executing the pipeline.
  /// Each file is named <c>{ProjectName}.xml</c>.
  /// </summary>
  public IItem<IEnumerable<XmlDocument<CoberturaReport>>> CoverageXmlFiles =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.XmlDocuments<CoberturaReport>(
          label: "CoverageXmlFiles",
          directoryPath: $"{_basePath}/_01_Raw/Datasets"
        )
    );
}
