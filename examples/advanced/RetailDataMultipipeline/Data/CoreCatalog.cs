using Flowthru.Data;

namespace RetailDataMultipipeline.Data;

public partial class CoreCatalog : DataCatalogBase
{
  private readonly string _basePath;

  public CoreCatalog(string basePath)
  {
    _basePath = basePath;
    InitializeCatalogProperties();
  }
}
