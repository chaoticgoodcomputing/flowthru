using Flowthru.Core.Data;

namespace DiarizationExample.Data;

public partial class Catalog
{
  /// <summary>
  /// Rendered transcripts (one Markdown file per clip) suitable for review.
  /// </summary>
  public IItem<Directory<byte[]>> RenderedTranscripts =>
    CreateItem(() =>
      ItemFactory.Enumerable.BinaryDirectory(
        label: "RenderedTranscripts",
        directoryPath: $"{_basePath}/_08_Reporting/transcripts",
        filePattern: "*.md"
      )
    );
}
