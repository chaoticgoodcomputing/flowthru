using Flowthru.Data.Catalog;
using WideTransformBenchmark.Data._04_Reporting.Schemas;

namespace WideTransformBenchmark.Data;

public partial class Catalog
{
  /// <summary>
  /// The per-size comparison rows — one per fabricated dataset size. A CSV on
  /// disk rather than a memory item because it is both a deliverable in its
  /// own right and the input to the report renderer.
  /// </summary>
  public IItem<IEnumerable<BenchmarkComparison>> BenchmarkSummary =>
    CreateItem(() =>
      Item.Of<IEnumerable<BenchmarkComparison>>("BenchmarkSummary")
        .Csv()
        .AtPath($"{_basePath}/_04_Reporting/Datasets/benchmark_summary.csv")
        .Build());

  /// <summary>
  /// The headline artefact: a Markdown report with the per-size comparison
  /// table and a crossover verdict, rendered from the checked-in template by
  /// <c>RenderBenchmarkReportStep</c>.
  /// </summary>
  public IItem<byte[]> BenchmarkReport =>
    CreateItem(() =>
      Item.Of<byte[]>("BenchmarkReport")
        .Binary()
        .AtPath($"{_basePath}/_04_Reporting/Datasets/benchmark_report.md")
        .Build());
}
