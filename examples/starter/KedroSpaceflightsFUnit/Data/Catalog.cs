using Flowthru.Core.Data;

namespace KedroSpaceflightsFUnit.Data;

/// <summary>
/// Data catalog for the Spaceflights pipeline, providing access to datasets across all data layers.
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
