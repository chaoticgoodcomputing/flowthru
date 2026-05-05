using DiarizationExample.Data;
using DiarizationExample.Data._04_Feature.Schemas;
using Flowthru.Core.Data;
using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;

namespace DiarizationExample.Flows.Reporting;

public static class ReportingFlow
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddPythonStep<IEnumerable<AttributedSegmentSchema>, Directory<byte[]>>(
        label: "RenderTranscript",
        description: "Render one Markdown transcript per clip.",
        module: "Flows.Reporting.Steps.render_transcript",
        function: "render_transcript",
        input: catalog.AttributedTranscript,
        output: catalog.RenderedTranscripts,
        executor: executor
      );
    });
  }
}
