using System.Collections.Generic;
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

  /// <summary>
  /// Flat project → directory → file → method icicle nodes augmented with
  /// line-level provenance counts (total / any / unit / integration). One
  /// row per node carries everything the downstream renderer needs to
  /// colour each tile by unit + integration + total coverage simultaneously.
  /// </summary>
  public IItem<IEnumerable<ProvenanceIcicleNode>> ProvenanceIcicleCoverage =>
    CreateItem(() =>
      Item.Of<IEnumerable<ProvenanceIcicleNode>>("ProvenanceIcicleCoverage")
        .Csv()
        .AtPath($"{_basePath}/_04_Reporting/Datasets/icicle_coverage_provenance.csv")
        .Build()
    );

  /// <summary>
  /// Per-library Plotly icicle SVGs with provenance-encoded colour:
  /// <c>R = 1 − Integration%</c>, <c>G = Any%</c>, <c>B = 1 − Unit%</c>.
  /// One file per src project.
  /// </summary>
  public IItem<DirectoryOf<byte[]>> ProvenanceCoverageIcicles =>
    CreateItem(() =>
      Item.Of<DirectoryOf<byte[]>>("ProvenanceCoverageIcicles")
        .Directory(file => file.Binary())
        .AtPath($"{_basePath}/_04_Reporting/Datasets/icicles_provenance")
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

  /// <summary>
  /// Markdown report ranking src libraries (and sub-trees within failing
  /// libraries) by unit-coverage gap, plus a per-library method checklist
  /// split into "quick wins" (covered by integration but not unit) and
  /// "cold spots" (no coverage at all).
  /// </summary>
  public IItem<byte[]> UnitCoverageReport =>
    CreateItem(() =>
      Item.Of<byte[]>("UnitCoverageReport")
        .Binary()
        .AtPath($"{_basePath}/_04_Reporting/Datasets/unit_coverage_report.md")
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
