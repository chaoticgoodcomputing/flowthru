using Flowthru.Core.Data;

namespace RetailDataMultipipeline.Data;

public partial class CoreCatalog : CatalogAbstract
{
  private readonly string _basePath;

  public CoreCatalog(string basePath)
  {
    _basePath = basePath;
    InitializeCatalogProperties();
  }
}
