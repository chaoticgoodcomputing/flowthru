using Flowthru.Core.Data;
using FlowthruCoverage.Data._03_Primary.Schemas;

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

  /// <summary>
  /// Nested coverage report by package → namespace → class → method → test project.
  /// One <see cref="PackageCoverageReport"/> per source assembly.
  /// Use this as a model input table for per-library coverage-intensity analysis.
  /// </summary>
  public IItem<IEnumerable<PackageCoverageReport>> MethodCoverage =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<PackageCoverageReport>(
          label: "MethodCoverage",
          filePath: $"{_basePath}/_03_Primary/Datasets/method_coverage.json"
        )
    );

  /// <summary>
  /// Flat per-method summary: identifier, total hits across all projects, and number of
  /// projects that hit it. Ordered by <c>TotalHits</c> ascending — least-tested methods first.
  /// </summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> MethodHitSummary =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<MethodHitSummaryRow>(
          label: "MethodHitSummary",
          filePath: $"{_basePath}/_03_Primary/Datasets/method_hit_summary.csv"
        )
    );

  /// <summary>
  /// Variant of <see cref="MethodHitSummary"/> where the ID uses only the method name
  /// (<c>{namespace}.{className}.{methodName}</c>) — overloads are collapsed into one row.
  /// <c>TotalHits</c> is summed across overloads; <c>ProjectHits</c> is the union of
  /// projects that hit any overload. Same sort order: subgroup then <c>TotalHits</c> ascending.
  /// </summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> MethodNameSummary =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<MethodHitSummaryRow>(
          label: "MethodNameSummary",
          filePath: $"{_basePath}/_03_Primary/Datasets/method_name_summary.csv"
        )
    );
}
