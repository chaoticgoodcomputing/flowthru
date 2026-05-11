using Flowthru.Data.Catalog;

namespace SpaceflightsDistributed.DataProcessing.Data;

/// <summary>
/// Data catalog for the DataProcessing pipeline library.
/// Owns all raw, intermediate, and primary data layers for the Spaceflights domain.
/// </summary>
public partial class DataProcessingCatalog : CatalogAbstract
{
  private readonly string _basePath;

  /// <summary>
  /// Initializes a new DataProcessingCatalog.
  /// </summary>
  /// <param name="basePath">Base path for data file resolution.</param>
  public DataProcessingCatalog(string basePath)
  {
    _basePath = basePath;
  }
}
