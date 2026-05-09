using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using FlowthruCoverage.Data._03_Primary.Schemas;
using FlowthruCoverage.Data._04_Reporting.Schemas;

namespace FlowthruCoverage.Data;

public partial class Catalog
{
  public IItem<IEnumerable<PivotCoverageRow>> PivotCoverage =>
    CreateItem(() =>
      Item.Of<IEnumerable<PivotCoverageRow>>("PivotCoverage")
        .Csv()
        .AtPath($"{_basePath}/_04_Reporting/Datasets/coverage_heatmap.csv")
        .Build()
    );

  public IItem<byte[]> CoverageHeatmap =>
    CreateItem(() =>
      Item.Of<byte[]>("CoverageHeatmap")
        .Binary()
        .AtPath($"{_basePath}/_04_Reporting/Datasets/coverage_heatmap.png")
        .Build()
    );

  /// <summary>Flat project -> file -> method icicle nodes for src libraries.</summary>
  public IItem<IEnumerable<IcicleCoverageNode>> IcicleCoverage =>
    CreateItem(() =>
      Item.Of<IEnumerable<IcicleCoverageNode>>("IcicleCoverage")
        .Csv()
        .AtPath($"{_basePath}/_04_Reporting/Datasets/icicle_coverage.csv")
        .Build()
    );

  /// <summary>Per-library Plotly icicle SVGs, one file per src project.</summary>
  public IItem<DirectoryOf<byte[]>> CoverageIcicles =>
    CreateItem(() =>
      Item.Of<DirectoryOf<byte[]>>("CoverageIcicles")
        .Directory(file => file.Binary())
        .AtPath($"{_basePath}/_04_Reporting/Datasets/icicles")
        .WithFilePattern("*.svg")
        .Build()
    );

  /// <summary>Flat icicle nodes computed from ExampleMethodLineCoverage only.</summary>
  public IItem<IEnumerable<IcicleCoverageNode>> ExampleIcicleCoverage =>
    CreateItem(() =>
      Item.Of<IEnumerable<IcicleCoverageNode>>("ExampleIcicleCoverage")
        .Csv()
        .AtPath($"{_basePath}/_04_Reporting/Datasets/icicle_coverage_examples.csv")
        .Build()
    );

  /// <summary>Per-library Plotly icicle SVGs derived from example-only coverage.</summary>
  public IItem<DirectoryOf<byte[]>> ExampleCoverageIcicles =>
    CreateItem(() =>
      Item.Of<DirectoryOf<byte[]>>("ExampleCoverageIcicles")
        .Directory(file => file.Binary())
        .AtPath($"{_basePath}/_04_Reporting/Datasets/icicles_examples")
        .WithFilePattern("*.svg")
        .Build()
    );

  /// <summary>Methods with zero total hits — full-signature variant — pre remote-source filter.</summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> UncoveredMethodHitsRaw =>
    CreateItem(() =>
      Item.Of<IEnumerable<MethodHitSummaryRow>>("UncoveredMethodHitsRaw").Memory().Build()
    );

  /// <summary>Methods with zero total hits — method-name variant — pre remote-source filter.</summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> UncoveredMethodNamesRaw =>
    CreateItem(() =>
      Item.Of<IEnumerable<MethodHitSummaryRow>>("UncoveredMethodNamesRaw").Memory().Build()
    );

  /// <summary>Methods with zero total hits — full-signature variant.</summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> UncoveredMethodHits =>
    CreateItem(() =>
      Item.Of<IEnumerable<MethodHitSummaryRow>>("UncoveredMethodHits")
        .Csv()
        .AtPath($"{_basePath}/_04_Reporting/Datasets/uncovered_method_hits.csv")
        .Build()
    );

  /// <summary>Methods with zero total hits — method-name variant (overloads collapsed).</summary>
  public IItem<IEnumerable<MethodHitSummaryRow>> UncoveredMethodNames =>
    CreateItem(() =>
      Item.Of<IEnumerable<MethodHitSummaryRow>>("UncoveredMethodNames")
        .Csv()
        .AtPath($"{_basePath}/_04_Reporting/Datasets/uncovered_method_names.csv")
        .Build()
    );

  /// <summary>Per-source-package coverage rolled up to the maximum across test projects.</summary>
  public IItem<IEnumerable<PackageCoverageMaxRow>> PackageCoverageMax =>
    CreateItem(() =>
      Item.Of<IEnumerable<PackageCoverageMaxRow>>("PackageCoverageMax")
        .Csv()
        .AtPath($"{_basePath}/_04_Reporting/Datasets/package_coverage_max.csv")
        .Build()
    );
}
