using DiarizationExample.Data._03_Primary.Schemas;
using DiarizationExample.Data._04_Feature.Schemas;
using Flowthru.Core.Steps;

namespace DiarizationExample.Flows.Alignment.Steps;

/// <summary>
/// Joins each transcript segment to the speaker turn that maximally overlaps
/// it. Pure interval logic — no ML, no external services, fits naturally in
/// C#. Demonstrates that the example pipeline is genuinely mixed-language:
/// the heavy ML work is in Python where the ecosystem lives, but pure
/// data manipulation stays in C# where the type system pulls its weight.
/// </summary>
[FlowthruStep(IsIdempotent = true)]
public static class AlignTranscriptStep
{
  public static Func<
    (IEnumerable<TranscriptSegmentSchema>, IEnumerable<DiarizationSegmentSchema>),
    IEnumerable<AttributedSegmentSchema>
  > Create()
  {
    return (input) =>
    {
      var (transcripts, diarization) = input;
      var turnsByClip = diarization
        .GroupBy(t => t.ClipId)
        .ToDictionary(g => g.Key, g => g.ToList());

      return transcripts.Select(segment =>
      {
        var turns = turnsByClip.GetValueOrDefault(segment.ClipId);
        var speaker = turns is null
          ? "unknown"
          : DominantSpeaker(turns, segment.Start, segment.End);

        return new AttributedSegmentSchema
        {
          ClipId = segment.ClipId,
          Start = segment.Start,
          End = segment.End,
          SpeakerId = speaker,
          Text = segment.Text,
        };
      });
    };
  }

  private static string DominantSpeaker(
    List<DiarizationSegmentSchema> turns,
    double segStart,
    double segEnd
  )
  {
    var bestSpeaker = "unknown";
    var bestOverlap = 0.0;
    foreach (var turn in turns)
    {
      var overlap = Math.Max(0, Math.Min(segEnd, turn.End) - Math.Max(segStart, turn.Start));
      if (overlap > bestOverlap)
      {
        bestOverlap = overlap;
        bestSpeaker = turn.SpeakerId;
      }
    }
    return bestSpeaker;
  }
}
