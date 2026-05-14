using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using Microsoft.Extensions.Configuration;
using SpaceflightsDistributed.DataScience.Flows.DataScience.Steps;

namespace SpaceflightsDistributed.DataScience.Data;

/// <summary>
/// Data catalog for the DataScience pipeline library.
/// Owns the model input splits, trained model, and model output layers,
/// plus configuration-bound option records that flow into steps as
/// ordinary inputs.
/// </summary>
public partial class DataScienceCatalog : CatalogAbstract
{
  private readonly string _basePath;
  private readonly IConfiguration _configuration;

  public DataScienceCatalog(string basePath, IConfiguration configuration)
  {
    _basePath = basePath;
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
  }

  /// <summary>Train/test split options sourced from <c>Flowthru:Flows:DataScience:ModelOptions</c>.</summary>
  public IItem<SplitDataStep.ModelOptions> ModelOptions =>
    CreateItem(() =>
      Item.Of<SplitDataStep.ModelOptions>("ModelOptions")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:DataScience:ModelOptions")
        .Build());
}
