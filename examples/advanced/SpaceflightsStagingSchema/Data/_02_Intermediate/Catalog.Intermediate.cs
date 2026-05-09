using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Bulk;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class StagingCatalog
{
  /// <summary>
  /// Preprocessed company data, written to staging by DataProcessing and
  /// promoted to production with FK enforcement.
  /// </summary>
  public IItem<IEnumerable<PreprocessedCompanySchema>> Companies =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedCompanySchema>>("StagingCompanies")
      .EFCoreQuery<PreprocessedCompanySchema, StagingDbContext>()
      .WithContextFactory(_contextFactory)
      .WithSave(BulkSave.Insert<PreprocessedCompanySchema, StagingDbContext>())
      .WithScope(DbScope.Explicit(SharedScope))
      .Build());

  /// <summary>
  /// Preprocessed shuttle data, written to staging by DataProcessing.
  /// Promoted under FK constraint <c>Shuttles.CompanyId → Companies.Id</c>.
  /// </summary>
  public IItem<IEnumerable<PreprocessedShuttleSchema>> Shuttles =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("StagingShuttles")
      .EFCoreQuery<PreprocessedShuttleSchema, StagingDbContext>()
      .WithContextFactory(_contextFactory)
      .WithSave(BulkSave.Insert<PreprocessedShuttleSchema, StagingDbContext>())
      .WithScope(DbScope.Explicit(SharedScope))
      .Build());

  /// <summary>
  /// Preprocessed review data, written to staging by DataProcessing.
  /// Promoted under FK constraint <c>Reviews.ShuttleId → Shuttles.Id</c>.
  /// </summary>
  public IItem<IEnumerable<PreprocessedReviewSchema>> Reviews =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedReviewSchema>>("StagingReviews")
      .EFCoreQuery<PreprocessedReviewSchema, StagingDbContext>()
      .WithContextFactory(_contextFactory)
      .WithSave(BulkSave.Insert<PreprocessedReviewSchema, StagingDbContext>())
      .WithScope(DbScope.Explicit(SharedScope))
      .Build());
}
