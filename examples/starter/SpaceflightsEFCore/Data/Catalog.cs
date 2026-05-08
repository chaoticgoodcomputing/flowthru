using Flowthru.Data.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Data catalog for the Spaceflights pipeline, providing access to datasets across all data layers.
/// </summary>
public partial class Catalog : CatalogAbstract
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

    // Ensure the SQLite database and schema exist before catalog
    // entries are dereferenced. EFCore items are wired to expect the
    // DB to be present; this gives us a one-shot CREATE TABLE for the
    // demo without depending on migrations.
    using var ctx = contextFactory.CreateDbContext();
    ctx.Database.EnsureCreated();
  }
}
