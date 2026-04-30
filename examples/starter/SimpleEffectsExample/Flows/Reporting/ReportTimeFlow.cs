using Flowthru.Core.Flows;
using SimpleEffectsExample.Data;
using SimpleEffectsExample.Flows.Reporting.Steps;

namespace SimpleEffectsExample.Flows.Reporting;

/// <summary>
/// Single-step pipeline: read a format template, call the injected time service,
/// write the formatted line out as a text file. The simplest end-to-end demonstration
/// of an effect-bearing flow in Flowthru.
/// </summary>
public static class ReportTimeFlow
{
  public static Flow Create(Catalog catalog, Services.IRemoteTimeService timeService)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "ReportTime",
        description: "Fetches current UTC time from the remote service and writes a formatted report.",
        transform: ReportTimeStep.Create(timeService),
        input: catalog.ReportTemplate,
        output: catalog.CurrentTimeReport
      );
    });
  }
}
