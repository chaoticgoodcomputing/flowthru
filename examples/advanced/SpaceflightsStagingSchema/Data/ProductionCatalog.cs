using Flowthru.Data.Catalog;
using Microsoft.EntityFrameworkCore;

namespace SpaceflightsStagingSchema.Data;

/// <summary>
/// Catalog of persistent production tables in PostgreSQL's <c>public</c>
/// schema. Schema lifecycle is migration-style — created once via
/// <c>EnsureCreated</c>, persists across runs (within the Testcontainers
/// session); no <c>FlowResource</c> is declared.
/// </summary>
/// <remarks>
/// <para>
/// All items declare <see cref="Flowthru.Extensions.EFCore.Data.DbScope.Explicit(string)"/>
/// with <see cref="StagingCatalog.SharedScope"/>. The shared scope is what
/// activates server-side <c>INSERT-FROM-SELECT</c> when a promote step
/// outputs a <c>DbQuery</c> from <see cref="StagingCatalog"/> against an
/// item here.
/// </para>
/// <para>
/// DataScience and Reporting read from this catalog and write back to it.
/// Reporting reads from <see cref="Shuttles"/> and
/// <see cref="ModelPredictions"/> only — never staging, never the model
/// input view.
/// </para>
/// </remarks>
public partial class ProductionCatalog : CatalogAbstract
{
  private readonly IDbContextFactory<ProductionDbContext> _contextFactory;
  private readonly string _basePath;

  public ProductionCatalog(
    IDbContextFactory<ProductionDbContext> contextFactory,
    string basePath
  )
  {
    _contextFactory = contextFactory;
    _basePath = basePath;

    using var ctx = contextFactory.CreateDbContext();
    ctx.Database.EnsureCreated();
  }
}
