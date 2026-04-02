using Flowthru.Data;

namespace Minimal.Data;

/// <summary>
/// Data catalog providing access to all datasets in the pipeline.
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
