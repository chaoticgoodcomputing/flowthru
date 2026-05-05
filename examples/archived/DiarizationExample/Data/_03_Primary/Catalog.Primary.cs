using DiarizationExample.Data._03_Primary.Schemas;
using Flowthru.Core.Data;

namespace DiarizationExample.Data;

public partial class Catalog
{
  /// <summary>
  /// Whisper transcript segments — one row per (clip_id, start, end) span.
  /// Stored as Parquet so re-runs can skip transcription if the upstream
  /// audio hasn't changed.
  /// </summary>
  public IItem<IEnumerable<TranscriptSegmentSchema>> Transcripts =>
    CreateItem(() =>
      ItemFactory.Enumerable.Parquet<TranscriptSegmentSchema>(
        label: "Transcripts",
        filePath: $"{_basePath}/_03_Primary/transcripts.parquet"
      )
    );

  /// <summary>
  /// pyannote diarization turns — one row per (clip_id, start, end, speaker).
  /// Speaker indices are local to each clip (speaker_0, speaker_1, ...);
  /// cross-clip speaker identity is out of scope for this example.
  /// </summary>
  public IItem<IEnumerable<DiarizationSegmentSchema>> DiarizationTurns =>
    CreateItem(() =>
      ItemFactory.Enumerable.Parquet<DiarizationSegmentSchema>(
        label: "DiarizationTurns",
        filePath: $"{_basePath}/_03_Primary/diarization.parquet"
      )
    );
}
