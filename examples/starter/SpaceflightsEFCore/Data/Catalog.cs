using Flowthru.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Data catalog for the Spaceflights pipeline, providing access to datasets across all data layers.
/// </summary>
public partial class Catalog : DataCatalogBase
{
  private readonly string _basePath;
  private readonly IDbContextFactory<SpaceflightsDbContext> _contextFactory;

  /// <summary>
  /// Initializes a new instance of the <see cref="Catalog"/> class.
  /// </summary>
  /// <param name="basePath">The base path for data storage.</param>
  /// <param name="contextFactory">Factory that creates fresh <see cref="SpaceflightsDbContext"/> instances per operation.</param>
  public Catalog(string basePath, IDbContextFactory<SpaceflightsDbContext> contextFactory)
  {
    _basePath = basePath;
    _contextFactory = contextFactory;

    // Ensure the SQLite database and schema exist before catalog entries are initialized.
    using var ctx = contextFactory.CreateDbContext();
    ctx.Database.EnsureCreated();

    InitializeCatalogProperties();
  }
}
