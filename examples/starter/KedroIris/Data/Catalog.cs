using Flowthru.Core.Data;

namespace KedroIris.Data;

/// <summary>
/// Data catalog for the Iris classification pipeline, providing access to datasets across all data layers.
/// </summary>
public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;

  /// <summary>
  /// Initializes a new instance of the <see cref="Catalog"/> class.
  /// </summary>
  /// <param name="basePath">The base path for data storage.</param>
  public Catalog(string basePath)
  {
    _basePath = basePath;
    InitializeCatalogProperties();
  }
}
