using Flowthru.Core.Data;

namespace DiarizationExample.Data;

public partial class Catalog
{
  /// <summary>
  /// Audio normalized to 16kHz mono PCM (WAV bytes). Both Whisper and pyannote
  /// expect this format; doing the transcode once here means downstream steps
  /// can run in parallel without each redoing the same work.
  /// </summary>
  public IItem<DirectoryOf<byte[]>> NormalizedAudio =>
    CreateItem(() =>
      ItemFactory.Enumerable.BinaryDirectory(
        label: "NormalizedAudio",
        directoryPath: $"{_basePath}/_02_Intermediate/normalized",
        filePattern: "*.wav"
      )
    );
}
