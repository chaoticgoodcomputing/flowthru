using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Bulk;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>
  /// Production companies. Promotion target for staging companies; read by
  /// downstream flows as a deferred queryable handle.
  /// </summary>
  public IItem<IEnumerable<PreprocessedCompanySchema>> Companies =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedCompanySchema>>("ProductionCompanies")
      .EFCoreQuery<PreprocessedCompanySchema, ProductionDbContext>()
      .WithContextFactory(_contextFactory)
      .WithSave(BulkSave.Insert<PreprocessedCompanySchema, ProductionDbContext>())
      .WithScope(DbScope.Explicit(StagingCatalog.SharedScope))
      .Build());

  /// <summary>
  /// Production shuttles. FK-constrained to <see cref="Companies"/>.
  /// </summary>
  public IItem<IEnumerable<PreprocessedShuttleSchema>> Shuttles =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("ProductionShuttles")
      .EFCoreQuery<PreprocessedShuttleSchema, ProductionDbContext>()
      .WithContextFactory(_contextFactory)
      .WithSave(BulkSave.Insert<PreprocessedShuttleSchema, ProductionDbContext>())
      .WithScope(DbScope.Explicit(StagingCatalog.SharedScope))
      .Build());

  /// <summary>
  /// Production reviews. FK-constrained to <see cref="Shuttles"/>.
  /// </summary>
  public IItem<IEnumerable<PreprocessedReviewSchema>> Reviews =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedReviewSchema>>("ProductionReviews")
      .EFCoreQuery<PreprocessedReviewSchema, ProductionDbContext>()
      .WithContextFactory(_contextFactory)
      .WithSave(BulkSave.Insert<PreprocessedReviewSchema, ProductionDbContext>())
      .WithScope(DbScope.Explicit(StagingCatalog.SharedScope))
      .Build());
}
