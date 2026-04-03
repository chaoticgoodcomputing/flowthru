using Flowthru.Data;

namespace UmapReferenceComparisons.Data;

/// <summary>
/// Data catalog for UMAP reference comparison project.
/// </summary>
/// <remarks>
/// Manages reference data from Python UMAP implementation and comparison results.
/// The catalog is split across partial classes organized by data layer.
/// </remarks>
public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;

  public Catalog(string basePath)
  {
    _basePath = basePath;
    InitializeCatalogProperties();
  }
}
