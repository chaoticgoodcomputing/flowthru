using Flowthru.Core.Abstractions;

namespace DiarizationExample.Data._03_Primary.Schemas;

/// <summary>
/// One pyannote speaker turn for one input clip. <c>SpeakerId</c> is local
/// to the clip (e.g., <c>speaker_0</c>, <c>speaker_1</c>); cross-clip speaker
/// identity is out of scope for this example.
/// </summary>
[FlowthruSchema]
public partial record DiarizationSegmentSchema
{
  [SerializedLabel("clip_id")]
  public string ClipId { get; init; } = null!;

  [SerializedLabel("start")]
  public double Start { get; init; }

  [SerializedLabel("end")]
  public double End { get; init; }

  [SerializedLabel("speaker_id")]
  public string SpeakerId { get; init; } = null!;
}
