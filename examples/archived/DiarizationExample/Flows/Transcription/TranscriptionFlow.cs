using DiarizationExample.Data;
using DiarizationExample.Data._03_Primary.Schemas;
using Flowthru.Core.Data;
using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;

namespace DiarizationExample.Flows.Transcription;

public static class TranscriptionFlow
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddPythonStep<Directory<byte[]>, IEnumerable<TranscriptSegmentSchema>>(
        label: "Transcribe",
        description: "Transcribe each normalized clip via Whisper.",
        module: "Flows.Transcription.Steps.transcribe",
        function: "transcribe",
        input: catalog.NormalizedAudio,
        output: catalog.Transcripts,
        executor: executor
      );
    });
  }
}
