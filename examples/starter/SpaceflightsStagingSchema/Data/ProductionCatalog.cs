using Flowthru.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace SpaceflightsStagingSchema.Data;

/// <summary>
/// Catalog of persistent production tables. Schema lifecycle is migration-style
/// (created once via <c>EnsureCreated</c>, persists across runs); no
/// <c>FlowResource</c> is declared.
/// </summary>
/// <remarks>
/// All tables in this catalog survive flow execution. DataScience writes train/test
/// splits, the trained model, metrics, and predictions here. Reporting reads
/// from <see cref="ModelInputTable"/> and <see cref="ModelPredictions"/> only —
/// it never touches staging.
/// </remarks>
public partial class ProductionCatalog : CatalogAbstract
{
  private readonly IDbContextFactory<ProductionDbContext> _contextFactory;

  public ProductionCatalog(IDbContextFactory<ProductionDbContext> contextFactory)
  {
    _contextFactory = contextFactory;

    using var ctx = contextFactory.CreateDbContext();
    ctx.Database.EnsureCreated();

    InitializeCatalogProperties();
  }
}
