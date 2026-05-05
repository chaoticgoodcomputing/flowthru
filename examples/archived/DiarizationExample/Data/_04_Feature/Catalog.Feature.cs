using DiarizationExample.Data._04_Feature.Schemas;
using Flowthru.Core.Data;

namespace DiarizationExample.Data;

public partial class Catalog
{
  /// <summary>
  /// Transcript segments joined to diarization turns by maximal time-overlap.
  /// Each row carries text + speaker_id + clip_id + timestamps — the canonical
  /// "speaker-attributed transcript" output.
  /// </summary>
  public IItem<IEnumerable<AttributedSegmentSchema>> AttributedTranscript =>
    CreateItem(() =>
      ItemFactory.Enumerable.Parquet<AttributedSegmentSchema>(
        label: "AttributedTranscript",
        filePath: $"{_basePath}/_04_Feature/attributed_transcript.parquet"
      )
    );
}
