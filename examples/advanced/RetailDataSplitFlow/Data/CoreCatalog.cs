using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;

namespace RetailDataMultipipeline.Data;

public partial class CoreCatalog : CatalogAbstract
{
  private readonly string _basePath;
  private readonly IStorageMediumResolver? _resolver;

  public CoreCatalog(string basePath, IStorageMediumResolver? resolver = null)
  {
    _basePath = basePath;
    _resolver = resolver;
    InitializeCatalogProperties();
  }
}
