using Flowthru.Core.Data;
using Flowthru.Extensions.Spark;

namespace KedroSpaceflightsSpark.Data;

public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;
  internal readonly SparkFrameProvider frameProvider;

  public Catalog(string basePath, SparkFrameProvider frameProvider)
  {
    _basePath = basePath;
    this.frameProvider = frameProvider;
    InitializeCatalogProperties();
  }
}
