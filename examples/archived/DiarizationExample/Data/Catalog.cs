using Flowthru.Core.Data;

namespace DiarizationExample.Data;

/// <summary>
/// Data catalog for the Diarization pipeline. Inputs are batch audio files
/// (one <see cref="Directory{T}"/> of <c>byte[]</c>); intermediate and output
/// items are flat row schemas keyed by <c>clip_id</c> (= the source file path).
/// </summary>
public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;

  public Catalog(string basePath)
  {
    _basePath = basePath;
    InitializeCatalogProperties();
  }
}
