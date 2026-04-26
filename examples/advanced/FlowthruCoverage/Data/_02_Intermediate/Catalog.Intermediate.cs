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

  /// <summary>
  /// <see cref="LineCoverage"/> with compiler-synthesized rows removed. Consumed by the
  /// method-aggregation path so authored-method reports are uncluttered. The package-aggregation
  /// path stays on the unfiltered <see cref="LineCoverage"/> to preserve true denominators.
  /// </summary>
  public IItem<IEnumerable<LineCoverageRow>> MethodLineCoverage =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<LineCoverageRow>(
          label: "MethodLineCoverage",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/method_line_coverage.csv"
        )
    );
}
