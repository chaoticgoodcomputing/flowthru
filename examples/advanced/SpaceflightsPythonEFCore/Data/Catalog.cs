using Flowthru.Data.Catalog;
using Flowthru.Data.Catalog.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SpaceflightsPythonEFCore.Flows.DataScience.Schemas;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Data catalog for the SpaceflightsPythonEFCore pipeline.
/// Combines EFCore storage (DataProcessing and ModelPredictions) with Python-consumed
/// file and memory entries, plus configuration-bound options records that flow into
/// Python steps as catalog inputs (Phase 9 singleton path).
/// </summary>
public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;
  private readonly IDbContextFactory<SpaceflightsDbContext> _contextFactory;
  private readonly IConfiguration _configuration;

  public Catalog(
    string basePath,
    IDbContextFactory<SpaceflightsDbContext> contextFactory,
    IConfiguration configuration
  )
  {
    _basePath = basePath;
    _contextFactory = contextFactory;
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    using var ctx = contextFactory.CreateDbContext();
    ctx.Database.EnsureCreated();
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
