using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using Microsoft.Extensions.Configuration;
using SpaceflightsStagingSchema.Flows.DataProcessing;

namespace SpaceflightsStagingSchema.Data;

/// <summary>
/// Catalog of raw filesystem inputs (CSV/Excel). No resource lifecycle —
/// these files are external prerequisites supplied by upstream systems.
/// Also exposes <see cref="SeedingOptions"/> bound from configuration so
/// DataProcessing steps can pull the synthetic-row knobs as an ordinary
/// catalog input.
/// </summary>
public partial class RawCatalog : CatalogAbstract
{
  private readonly string _basePath;
  private readonly IConfiguration _configuration;

  public RawCatalog(string basePath, IConfiguration configuration)
  {
    _basePath = basePath;
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
  }

  /// <summary>
  /// Synthetic-data seeding options sourced from
  /// <c>Flowthru:Flows:DataProcessing:Seeding</c>.
  /// </summary>
  public IItem<SeedingOptions> SeedingOptions =>
    CreateItem(() =>
      Item.Of<SeedingOptions>("SeedingOptions")
        .FromConfiguration(_configuration)
        .AtSection("Flowthru:Flows:DataProcessing:Seeding")
        .Build());
}
