using Flowthru.Data.Catalog;
using FlowthruCoverage.Data._02_Intermediate.Schemas;

namespace FlowthruCoverage.Data;

public partial class Catalog
{
  /// <summary>Flat line-level coverage rows, one row per instrumented line per test project.</summary>
  public IItem<IEnumerable<LineCoverageRow>> LineCoverage =>
    CreateItem(() =>
      Item.Of<IEnumerable<LineCoverageRow>>("LineCoverage")
        .Csv()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/line_coverage.csv")
        .Build()
    );

  /// <summary>LineCoverage with compiler-synthesized rows removed.</summary>
  public IItem<IEnumerable<LineCoverageRow>> MethodLineCoverage =>
    CreateItem(() =>
      Item.Of<IEnumerable<LineCoverageRow>>("MethodLineCoverage")
        .Csv()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/method_line_coverage.csv")
        .Build()
    );

  /// <summary>MethodLineCoverage filtered to manifest Example test-project rows.</summary>
  public IItem<IEnumerable<LineCoverageRow>> ExampleMethodLineCoverage =>
    CreateItem(() =>
      Item.Of<IEnumerable<LineCoverageRow>>("ExampleMethodLineCoverage")
        .Csv()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/example_method_line_coverage.csv")
        .Build()
    );
}
