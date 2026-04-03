using Flowthru.Data;

namespace SpaceflightsDistributed.DataScience.Data;

/// <summary>
/// Data catalog for the DataScience pipeline library.
/// Owns the model input splits, trained model, and model output layers.
/// </summary>
public partial class DataScienceCatalog : CatalogAbstract
{
  private readonly string _basePath;

  public DataScienceCatalog(string basePath)
  {
    _basePath = basePath;
    InitializeCatalogProperties();
  }
}
