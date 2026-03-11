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
  private readonly SpaceflightsDbContext _dbContext;

  /// <summary>
  /// Initializes a new instance of the <see cref="Catalog"/> class.
  /// </summary>
  /// <param name="basePath">The base path for data storage.</param>
  /// <param name="dbContext">Shared DbContext instance for EFCore catalog entries.</param>
  public Catalog(string basePath, SpaceflightsDbContext dbContext)
  {
    _basePath = basePath;
    _dbContext = dbContext;
    InitializeCatalogProperties();
  }
}
