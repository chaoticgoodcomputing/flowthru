using Flowthru.Flow;
using StreamingBulkLoad.Data;
using StreamingBulkLoad.Data._01_Raw.Schemas;
using StreamingBulkLoad.Data._03_Primary.Schemas;
using StreamingBulkLoad.Flows.Reporting.Steps;

namespace StreamingBulkLoad.Flows.Reporting;

/// <summary>
/// The example proving its own thesis: read the memory samples the harness
/// measured (a Raw CSV), summarise the eager-vs-streaming verdict, and render it
/// into <c>Data/_04_Reporting/memory_report.md</c>. Pure Flowthru C# Steps — the
/// analytical workload here is the example's own instrumentation.
/// </summary>
public static class ReportingFlow
{
  public static BuiltFlow Create(Catalog catalog) =>
    FlowBuilder.CreateFlow("Reporting", flow =>
    {
      flow.AddStep<IEnumerable<MemorySample>, IEnumerable<MemoryComparison>>(
        label: "SummarizeMemory",
        transform: SummarizeMemoryStep.Create(),
        inputs: catalog.MemorySamples,
        outputs: catalog.MemoryComparisonSummary);

      flow.AddStep<IEnumerable<MemoryComparison>, string, byte[]>(
        label: "RenderMemoryReport",
        transform: RenderMemoryReportStep.Create(),
        inputs: (catalog.MemoryComparisonSummary, catalog.MemoryReportTemplate),
        outputs: catalog.MemoryReport);
    });
}
