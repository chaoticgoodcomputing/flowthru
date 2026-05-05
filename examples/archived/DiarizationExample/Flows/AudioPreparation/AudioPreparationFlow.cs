using DiarizationExample.Data;
using Flowthru.Core.Data;
using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;

namespace DiarizationExample.Flows.AudioPreparation;

/// <summary>
/// Resamples raw audio to 16kHz mono PCM. The single Python step depends on
/// the <c>FfmpegNormalizer</c> service; the C# side does not need to know
/// the service exists — the executor parses the <c>@step(services=[...])</c>
/// decorator at registration time and emits a service node into the DAG.
/// </summary>
public static class AudioPreparationFlow
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddPythonStep<Directory<byte[]>, Directory<byte[]>>(
        label: "NormalizeAudio",
        description: "Transcode each clip to 16kHz mono PCM via ffmpeg.",
        module: "Flows.AudioPreparation.Steps.normalize_audio",
        function: "normalize_audio",
        input: catalog.AudioClips,
        output: catalog.NormalizedAudio,
        executor: executor
      );
    });
  }
}
