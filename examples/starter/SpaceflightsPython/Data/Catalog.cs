using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using SpaceflightsPython.Flows.DataScience.Schemas;
using Microsoft.Extensions.Configuration;

namespace SpaceflightsPython.Data;

/// <summary>
/// Data catalog for the Spaceflights pipeline. Bridges raw/intermediate/
/// primary CSV datasets with configuration-bound options records that
/// flow into Python steps as catalog inputs (Phase 9 singleton path).
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
  /// Host configuration. Options records bind from <c>Flowthru:Flows:*</c>
  /// sections via <see cref="ConfigurationItem{T}"/>; changes invalidate
  /// the downstream cache automatically.
  /// </param>
  public Catalog(string basePath, IConfiguration configuration)
  {
    _basePath = basePath;
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
  }

  /// <summary>
  /// Train/test split options sourced from
  /// <c>Flowthru:Flows:DataScience:SplitDataOptions</c>. Flows into the
  /// Python <c>split_data</c> step as a JSON scalar.
  /// </summary>
  public IItem<SplitDataOptions> SplitDataOptions =>
    CreateItem(() =>
      Item.Of<SplitDataOptions>("SplitDataOptions")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:DataScience:SplitDataOptions")
        .Build());
}
