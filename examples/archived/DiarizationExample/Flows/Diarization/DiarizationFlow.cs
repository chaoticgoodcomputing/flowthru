using DiarizationExample.Data;
using DiarizationExample.Data._03_Primary.Schemas;
using Flowthru.Core.Data;
using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;

namespace DiarizationExample.Flows.Diarization;

/// <summary>
/// The motivating flow for Python-side service preflight. The
/// <c>diarize.py</c> step declares <c>services=[PyannoteDiarizer]</c>;
/// at flow registration the executor parses that decorator and records
/// the service dependency. At preflight, the framework instantiates the
/// service in the same venv that will run the step and calls its
/// <c>inspect()</c> method — exactly mirroring the C# pattern of
/// <c>AddFlowthruInspect&lt;T&gt;((svc, ct) =&gt; ...)</c>.
/// </summary>
public static class DiarizationFlow
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddPythonStep<DirectoryOf<byte[]>, IEnumerable<DiarizationSegmentSchema>>(
        label: "Diarize",
        description:
          "Identify speaker turns in each clip via pyannote. Service "
          + "dependencies (PyannoteDiarizer) are auto-discovered from the "
          + "Python decorator; no extra C# wiring needed.",
        module: "Flows.Diarization.Steps.diarize",
        function: "diarize",
        input: catalog.NormalizedAudio,
        output: catalog.DiarizationTurns,
        executor: executor
      );
    });
  }
}
