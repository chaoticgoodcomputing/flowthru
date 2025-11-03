using Flowthru.Data;

namespace RetailData.Data;

public partial class Catalog : DataCatalogBase
{
  private readonly string _basePath;

  public Catalog(string basePath)
  {
    _basePath = basePath;
    InitializeCatalogProperties();
  }
}
