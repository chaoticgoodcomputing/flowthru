using Flowthru.Core.Data;
using FlowthruCoverage.Data._03_Primary.Schemas;
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

  /// <summary>
  /// Methods with zero total hits across all test projects — full-signature variant.
  /// Subset of <see cref="MethodHitSummary"/>, same sort order (subgroup → TotalHits → Id).
  /// </summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> UncoveredMethodHits =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<MethodHitSummaryRow>(
          label: "UncoveredMethodHits",
          filePath: $"{_basePath}/_04_Reporting/Datasets/uncovered_method_hits.csv"
        )
    );

  /// <summary>
  /// Methods with zero total hits across all test projects — method-name variant (overloads collapsed).
  /// Subset of <see cref="MethodNameSummary"/>, same sort order (subgroup → TotalHits → Id).
  /// </summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> UncoveredMethodNames =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<MethodHitSummaryRow>(
          label: "UncoveredMethodNames",
          filePath: $"{_basePath}/_04_Reporting/Datasets/uncovered_method_names.csv"
        )
    );
}
