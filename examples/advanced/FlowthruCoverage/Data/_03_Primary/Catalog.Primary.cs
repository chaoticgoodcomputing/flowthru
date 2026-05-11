using Flowthru.Data.Catalog;
using FlowthruCoverage.Data._03_Primary.Schemas;

namespace FlowthruCoverage.Data;

public partial class Catalog
{
  /// <summary>Per-(TestProject, SrcPackage) coverage aggregates — heatmap pivot source.</summary>
  public IItem<IEnumerable<PackageCoverageRow>> PackageCoverage =>
    CreateItem(() =>
      Item.Of<IEnumerable<PackageCoverageRow>>("PackageCoverage")
        .Csv()
        .AtPath($"{_basePath}/_03_Primary/Datasets/package_coverage.csv")
        .Build()
    );

  /// <summary>Nested coverage report by package -> namespace -> class -> method.</summary>
  public IItem<IEnumerable<PackageCoverageReport>> MethodCoverage =>
    CreateItem(() =>
      Item.Of<IEnumerable<PackageCoverageReport>>("MethodCoverage")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/method_coverage.json")
        .Build()
    );

  /// <summary>Flat per-method summary ordered by TotalHits ascending.</summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> MethodHitSummary =>
    CreateItem(() =>
      Item.Of<IEnumerable<MethodHitSummaryRow>>("MethodHitSummary")
        .Csv()
        .AtPath($"{_basePath}/_03_Primary/Datasets/method_hit_summary.csv")
        .Build()
    );

  /// <summary>Method-name-only summary with overloads collapsed.</summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> MethodNameSummary =>
    CreateItem(() =>
      Item.Of<IEnumerable<MethodHitSummaryRow>>("MethodNameSummary")
        .Csv()
        .AtPath($"{_basePath}/_03_Primary/Datasets/method_name_summary.csv")
        .Build()
    );
}
