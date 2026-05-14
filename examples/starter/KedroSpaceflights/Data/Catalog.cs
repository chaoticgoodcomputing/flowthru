using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using KedroSpaceflights.Flows.DataScience.Steps;
using KedroSpaceflights.Flows.Reporting.Steps;
using Microsoft.Extensions.Configuration;

namespace KedroSpaceflights.Data;

/// <summary>
/// Data catalog for the Spaceflights pipeline, providing access to datasets across all data layers
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

  /// <summary>Train/test split options sourced from <c>Flowthru:Flows:DataScience:ModelOptions</c>.</summary>
  public IItem<SplitDataStep.ModelOptions> ModelOptions =>
    CreateItem(() =>
      Item.Of<SplitDataStep.ModelOptions>("ModelOptions")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:DataScience:ModelOptions")
        .Build());

  /// <summary>Confusion-matrix options sourced from <c>Flowthru:Flows:Reporting:ConfusionMatrixOptions</c>.</summary>
  public IItem<CreateConfusionMatrixStep.Options> ConfusionMatrixOptions =>
    CreateItem(() =>
      Item.Of<CreateConfusionMatrixStep.Options>("ConfusionMatrixOptions")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:Reporting:ConfusionMatrixOptions")
        .Build());
}
