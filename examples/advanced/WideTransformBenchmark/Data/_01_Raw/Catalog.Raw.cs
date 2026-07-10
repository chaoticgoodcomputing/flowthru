using Flowthru.Data.Catalog;
using WideTransformBenchmark.Data._01_Raw.Schemas;

namespace WideTransformBenchmark.Data;

public partial class Catalog
{
  /// <summary>
  /// The measured facts, written by the harness in
  /// <c>Benchmark/BenchmarkRunner.cs</c>: one row per (dataset size ×
  /// transform path) run. A Raw CSV so the Analyze Flow reads the profiling
  /// data back like any other input — the FlowthruCoverage staged-inputs
  /// pattern, except the staging step here is running the benchmark Flows.
  /// </summary>
  public IItem<IEnumerable<BenchmarkMeasurement>> Measurements =>
    CreateItem(() =>
      Item.Of<IEnumerable<BenchmarkMeasurement>>("Measurements")
        .Csv()
        .AtPath($"{_basePath}/_01_Raw/Datasets/benchmark_measurements.csv")
        .Build());

  /// <summary>Markdown template for the benchmark report — <c>{{token}}</c> placeholders filled by the renderer Step.</summary>
  public IItem<string> BenchmarkReportTemplate =>
    CreateItem(() =>
      Item.Of<string>("BenchmarkReportTemplate")
        .Text()
        .AtPath($"{_basePath}/_01_Raw/Templates/benchmark_report.md")
        .Build());
}
