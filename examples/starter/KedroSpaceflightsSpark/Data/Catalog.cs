using Flowthru.Core.Data;
using Flowthru.Extensions.Spark;

namespace KedroSpaceflightsSpark.Data;

public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;
  internal readonly SparkFrameProvider _provider;

  public Catalog(string basePath, SparkFrameProvider provider)
  {
    _basePath = basePath;
    _provider = provider;
    InitializeCatalogProperties();
  }
}
