using Flowthru.Core.Data;

namespace DiarizationExample.Data;

public partial class Catalog
{
  /// <summary>
  /// Batch of raw audio files dropped into <c>_01_Raw/Datasets/</c>. Each entry
  /// in the directory is one independent recording; the key is the full file
  /// path (used downstream as <c>clip_id</c> on every row). Glob covers the
  /// common formats — anything ffmpeg can decode is fine since the
  /// <c>NormalizeAudio</c> step transcodes to 16kHz mono PCM before either
  /// Whisper or pyannote sees it.
  /// </summary>
  public IItem<Directory<byte[]>> AudioClips =>
    CreateItem(() =>
      ItemFactory.Enumerable.BinaryDirectory(
        label: "AudioClips",
        directoryPath: $"{_basePath}/_01_Raw/Datasets",
        filePattern: "*.{wav,mp3,m4a,flac,ogg}"
      )
    );
}
