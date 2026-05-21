using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using Iris.Flows.DataEngineering.Steps;
using Iris.Flows.DataScience.Steps;
using Microsoft.Extensions.Configuration;

namespace Iris.Data;

/// <summary>
/// Data catalog for the Iris classification pipeline, providing access to datasets across all data layers
/// plus configuration-bound option records that flow into steps as ordinary inputs.
/// </summary>
public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;
  private readonly IConfiguration _configuration;

  /// <summary>
  /// Initializes a new instance of the <see cref="Catalog"/> class.
  /// </summary>
  /// <param name="basePath">The base path for data storage.</param>
  /// <param name="configuration">
  /// The host's <see cref="IConfiguration"/>, sourced from <c>appsettings.json</c>.
  /// Option records are bound from the <c>Flowthru:Flows:*</c> sections via
  /// <see cref="ConfigurationItem{T}"/> so a change in the file invalidates the
  /// affected downstream cache automatically.
  /// </param>
  public Catalog(string basePath, IConfiguration configuration)
  {
    _basePath = basePath;
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
  }

  /// <summary>Train/test split options sourced from <c>Flowthru:Flows:DataEngineering:SplitOptions</c>.</summary>
  public IItem<SplitAndEncodeStep.Options> SplitOptions =>
    CreateItem(() =>
      Item.Of<SplitAndEncodeStep.Options>("SplitOptions")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:DataEngineering:SplitOptions")
        .Build());

  /// <summary>Model training options sourced from <c>Flowthru:Flows:DataScience:TrainModelOptions</c>.</summary>
  public IItem<TrainModelStep.Options> TrainModelOptions =>
    CreateItem(() =>
      Item.Of<TrainModelStep.Options>("TrainModelOptions")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:DataScience:TrainModelOptions")
        .Build());
}
