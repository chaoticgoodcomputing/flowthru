using FlowthruCoverage.Data._03_Primary.Schemas;
using Flowthru.Core.Data;

namespace FlowthruCoverage.Data;

public partial class Catalog
{
  /// <summary>
  /// Per-(TestProject, SrcPackage) coverage aggregates.
  /// Pivot this CSV on TestProject vs SrcPackage to produce the coverage heatmap.
  /// </summary>
  public IItem<IEnumerable<PackageCoverageRow>> PackageCoverage =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<PackageCoverageRow>(
          label: "PackageCoverage",
          filePath: $"{_basePath}/_03_Primary/Datasets/package_coverage.csv"
        )
    );
}
