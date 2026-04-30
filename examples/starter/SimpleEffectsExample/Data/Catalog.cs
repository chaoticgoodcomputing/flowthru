using Flowthru.Core.Data;

namespace SimpleEffectsExample.Data;

/// <summary>
/// Data catalog for the SimpleEffectsExample pipeline. Layered partials below add
/// the per-layer entries (raw template, reporting output).
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
