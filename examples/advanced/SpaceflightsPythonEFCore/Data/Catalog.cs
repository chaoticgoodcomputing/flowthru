using Flowthru.Data;
using Microsoft.EntityFrameworkCore;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Data catalog for the SpaceflightsPythonEFCore pipeline.
/// Combines EFCore storage (DataProcessing and ModelPredictions) with Python-consumed file and memory entries.
/// </summary>
public partial class Catalog : CatalogAbstract
{
  private readonly string _basePath;
  private readonly IDbContextFactory<SpaceflightsDbContext> _contextFactory;

  public Catalog(string basePath, IDbContextFactory<SpaceflightsDbContext> contextFactory)
  {
    _basePath = basePath;
    _contextFactory = contextFactory;

    using var ctx = contextFactory.CreateDbContext();
    ctx.Database.EnsureCreated();

    InitializeCatalogProperties();
  }
}
