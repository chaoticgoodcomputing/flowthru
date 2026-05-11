using Flowthru.Data.Catalog;

namespace SpaceflightsStagingSchema.Data;

/// <summary>
/// Catalog of raw filesystem inputs (CSV/Excel). No resource lifecycle —
/// these files are external prerequisites supplied by upstream systems.
/// </summary>
public partial class RawCatalog : CatalogAbstract
{
  private readonly string _basePath;

  public RawCatalog(string basePath)
  {
    _basePath = basePath;
  }
}
