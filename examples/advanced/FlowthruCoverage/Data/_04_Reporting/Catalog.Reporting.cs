using Flowthru.Core.Data;
using FlowthruCoverage.Data._04_Reporting.Schemas;

namespace FlowthruCoverage.Data;

public partial class Catalog
{
  public IItem<IEnumerable<PivotCoverageRow>> PivotCoverage =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<PivotCoverageRow>(
          label: "PivotCoverage",
          filePath: $"{_basePath}/_04_Reporting/Datasets/coverage_heatmap.csv"
        )
    );

  public IItem<byte[]> CoverageHeatmap =>
    CreateItem(
      () =>
        ItemFactory.Single.Binary(
          label: "CoverageHeatmap",
          filePath: $"{_basePath}/_04_Reporting/Datasets/coverage_heatmap.png"
        )
    );
}
