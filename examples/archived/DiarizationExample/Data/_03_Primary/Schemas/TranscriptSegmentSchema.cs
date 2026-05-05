using Flowthru.Core.Abstractions;

namespace DiarizationExample.Data._03_Primary.Schemas;

/// <summary>
/// One Whisper transcript segment for one input clip. <c>ClipId</c> is the
/// source audio's file path (the <c>Directory&lt;byte[]&gt;</c> key); a single
/// clip produces many rows.
/// </summary>
[FlowthruSchema]
public partial record TranscriptSegmentSchema
{
  [SerializedLabel("clip_id")]
  public string ClipId { get; init; } = null!;

  [SerializedLabel("start")]
  public double Start { get; init; }

  [SerializedLabel("end")]
  public double End { get; init; }

  [SerializedLabel("text")]
  public string Text { get; init; } = null!;
}
