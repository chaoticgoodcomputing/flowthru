using FlowthruCoverage.Data._02_Intermediate.Schemas;
using Flowthru.Core.Data;

namespace FlowthruCoverage.Data;

public partial class Catalog
{
  /// <summary>Flat line-level coverage rows, one row per instrumented line per test project.</summary>
  public IItem<IEnumerable<LineCoverageRow>> LineCoverage =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<LineCoverageRow>(
          label: "LineCoverage",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/line_coverage.csv"
        )
    );
}
