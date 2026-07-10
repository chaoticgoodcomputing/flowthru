using Flowthru.Flow;
using WideTransformBenchmark.Data;
using WideTransformBenchmark.Data._01_Raw.Schemas;
using WideTransformBenchmark.Data._04_Reporting.Schemas;
using WideTransformBenchmark.Flows.Analyze.Steps;

namespace WideTransformBenchmark.Flows.Analyze;

/// <summary>
/// The dogfood: an ordinary typed Flowthru Flow whose analytical workload is
/// this example's own profiling data. Reads the measurement rows the harness
/// staged (a Raw CSV), pairs them into per-size comparisons
/// (<c>benchmark_summary.csv</c>), and renders the Markdown report from the
/// checked-in template — the same self-analysing shape as FlowthruCoverage
/// and StreamingBulkLoad.
/// </summary>
public static class AnalyzeFlow
{
  public static BuiltFlow Create(Catalog catalog) =>
    FlowBuilder.CreateFlow("Analyze", flow =>
    {
      flow.AddStep<IEnumerable<BenchmarkMeasurement>, IEnumerable<BenchmarkComparison>>(
        label: "BuildComparison",
        transform: BuildComparisonStep.Create(),
        inputs: catalog.Measurements,
        outputs: catalog.BenchmarkSummary);

      flow.AddStep<IEnumerable<BenchmarkComparison>, string, byte[]>(
        label: "RenderBenchmarkReport",
        transform: RenderBenchmarkReportStep.Create(),
        inputs: (catalog.BenchmarkSummary, catalog.BenchmarkReportTemplate),
        outputs: catalog.BenchmarkReport);
    });
}
