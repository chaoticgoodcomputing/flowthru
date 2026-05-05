using Flowthru.Core.Abstractions;

namespace DiarizationExample.Data._04_Feature.Schemas;

/// <summary>
/// A transcript segment with its dominant speaker attached. Produced by the
/// alignment step — for each transcript span, the speaker is whichever
/// diarization turn covers the largest fraction of the segment.
/// </summary>
[FlowthruSchema]
public partial record AttributedSegmentSchema
{
  [SerializedLabel("clip_id")]
  public string ClipId { get; init; } = null!;

  [SerializedLabel("start")]
  public double Start { get; init; }

  [SerializedLabel("end")]
  public double End { get; init; }

  [SerializedLabel("speaker_id")]
  public string SpeakerId { get; init; } = null!;

  [SerializedLabel("text")]
  public string Text { get; init; } = null!;
}
