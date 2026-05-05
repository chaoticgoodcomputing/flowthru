using DiarizationExample.Data;
using DiarizationExample.Flows.Alignment.Steps;
using Flowthru.Core.Flows;

namespace DiarizationExample.Flows.Alignment;

/// <summary>
/// Pure-C# flow joining the two upstream Python outputs. The transcripts and
/// diarization turns are produced in parallel by independent Python flows;
/// the alignment step pulls them together using only standard library
/// interval math. No Python dependency on this side of the boundary.
/// </summary>
public static class AlignmentFlow
{
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "AlignTranscriptToSpeakers",
        description:
          "Attach the dominant speaker to each transcript segment via "
          + "maximal time-overlap. Pure C# — no external dependencies.",
        transform: AlignTranscriptStep.Create(),
        input: (catalog.Transcripts, catalog.DiarizationTurns),
        output: catalog.AttributedTranscript
      );
    });
  }
}
