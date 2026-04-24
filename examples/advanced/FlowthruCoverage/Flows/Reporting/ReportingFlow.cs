using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;
using FlowthruCoverage.Data;
using FlowthruCoverage.Flows.Reporting.Steps;

namespace FlowthruCoverage.Flows.Reporting;

/// <summary>
/// Two-step reporting pipeline:
/// 1. <see cref="ClassifyCoverageStep"/> (C#) annotates each aggregate row with its heatmap
///    section (Library Tests / Integration Tests / Examples) and writes the ordered CSV.
/// 2. <see cref="generate_coverage_heatmap"/> (Python/Plotly) reads that CSV and produces
///    the PNG heatmap.
/// </summary>
public static class ReportingFlow
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "ClassifyCoverage",
        transform: ClassifyCoverageStep.Create(),
        input: catalog.PackageCoverage,
        output: catalog.PivotCoverage,
        description: "Annotates coverage rows with heatmap section (Library / Integration / Examples) and sorts into display order."
      );

      pipeline.AddPythonStep(
        label: "GenerateCoverageHeatmap",
        description: "Plotly PNG heatmap: test projects × src packages, colour = CoveragePercent. Sections and ordering driven by the Section column from ClassifyCoverage.",
        module: "Flows.Reporting.Steps.generate_coverage_heatmap",
        function: "generate_coverage_heatmap",
        input: catalog.PivotCoverage,
        output: catalog.CoverageHeatmap,
        executor: executor
      );
    });
  }
}
