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
  /// Methods with zero total hits — full-signature variant — BEFORE the remote-source filter.
  /// In-memory intermediate consumed by <see cref="Flows.Reporting.Steps.FilterRemoteSourceFilesStep"/>.
  /// </summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> UncoveredMethodHitsRaw =>
    CreateItem(() => ItemFactory.Enumerable.Memory<MethodHitSummaryRow>(label: "UncoveredMethodHitsRaw"));

  /// <summary>
  /// Methods with zero total hits — method-name variant — BEFORE the remote-source filter.
  /// In-memory intermediate consumed by <see cref="Flows.Reporting.Steps.FilterRemoteSourceFilesStep"/>.
  /// </summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> UncoveredMethodNamesRaw =>
    CreateItem(() => ItemFactory.Enumerable.Memory<MethodHitSummaryRow>(label: "UncoveredMethodNamesRaw"));

  /// <summary>
  /// Methods with zero total hits across all test projects — full-signature variant.
  /// Subset of <see cref="MethodHitSummary"/> with rows whose <c>SourceFile</c> is a
  /// remote SourceLink URL filtered out (see <see cref="Flows.Reporting.Steps.FilterRemoteSourceFilesStep"/>).
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
  /// Subset of <see cref="MethodNameSummary"/> with rows whose <c>SourceFile</c> is a
  /// remote SourceLink URL filtered out (see <see cref="Flows.Reporting.Steps.FilterRemoteSourceFilesStep"/>).
  /// </summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> UncoveredMethodNames =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<MethodHitSummaryRow>(
          label: "UncoveredMethodNames",
          filePath: $"{_basePath}/_04_Reporting/Datasets/uncovered_method_names.csv"
        )
    );

  /// <summary>
  /// Per-source-package coverage rolled up to the maximum across test projects. Flattens the
  /// double-counted shape of <see cref="PivotCoverage"/> (e.g. <c>Flowthru.Core.SourceGenerators</c>
  /// reading 0% in <c>Core.Tests</c> AND 74.41% in <c>SourceGenerators.Tests</c>) into one row
  /// per package showing the authoritative best reading. See
  /// <see cref="Flows.Reporting.Steps.AggregatePackageCoverageStep"/>.
  /// </summary>
  public IItem<IEnumerable<PackageCoverageMaxRow>> PackageCoverageMax =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<PackageCoverageMaxRow>(
          label: "PackageCoverageMax",
          filePath: $"{_basePath}/_04_Reporting/Datasets/package_coverage_max.csv"
        )
    );
}
