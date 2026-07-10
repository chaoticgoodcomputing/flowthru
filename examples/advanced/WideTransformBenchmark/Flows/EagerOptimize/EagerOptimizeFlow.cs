using Flowthru.Flow;
using WideTransformBenchmark.Data;
using WideTransformBenchmark.Data._01_Raw.Schemas;
using WideTransformBenchmark.Data._02_Intermediate.Schemas;
using WideTransformBenchmark.Flows.EagerOptimize.Steps;

namespace WideTransformBenchmark.Flows.EagerOptimize;

/// <summary>
/// The eager path: one C# Step running the optimize pass over one fabricated
/// dataset size, rows materialised in the CLR. Built once per size in the
/// run's size list — the flow label carries the size so the per-size runs are
/// distinguishable on the DAG and in the CLI.
/// </summary>
public static class EagerOptimizeFlow
{
  public static BuiltFlow Create(SizedBenchmarkCatalog catalog) =>
    FlowBuilder.CreateFlow($"EagerOptimize_{catalog.RowCount}", flow =>
      flow.AddStep<IEnumerable<RawReadingRow>, IEnumerable<OptimizedReadingRow>>(
        label: $"OptimizeReadingsEager_{catalog.RowCount}",
        transform: OptimizeReadingsEagerStep.Create(),
        inputs: catalog.RawReadings,
        outputs: catalog.EagerOptimized));
}
