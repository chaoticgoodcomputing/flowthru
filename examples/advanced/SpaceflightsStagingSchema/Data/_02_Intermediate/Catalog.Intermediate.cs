using Flowthru.Core.Data;
using Flowthru.Extensions.EFCore.Bulk;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class StagingCatalog
{
  /// <summary>
  /// Preprocessed company data, written to staging by DataProcessing and
  /// promoted to production with FK enforcement.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Read shape:</strong> <see cref="EFCoreItemFactory.Query"/> returns
  /// a deferred <see cref="DbQuery{T}"/> on load — promote steps cast the
  /// input to <c>DbQuery</c> and call <c>.Project&lt;T&gt;</c> to compose
  /// SQL-side joins.
  /// </para>
  /// <para>
  /// <strong>Write shape:</strong> the <c>BulkSave.Insert</c> saveFunc takes
  /// the IEnumerable produced by <c>PreprocessCompaniesStep</c> through the
  /// provider's bulk-load path (Npgsql binary COPY for PostgreSQL).
  /// </para>
  /// </remarks>
  public IItem<IEnumerable<PreprocessedCompanySchema>> Companies =>
    CreateItem(
      () =>
        EFCoreItemFactory.Query.EFCore<PreprocessedCompanySchema, StagingDbContext>(
          label: "StagingCompanies",
          contextFactory: _contextFactory,
          saveFunc: BulkSave.Insert<PreprocessedCompanySchema, StagingDbContext>(),
          scope: DbScope.Explicit(SharedScope)
        )
    );

  /// <summary>
  /// Preprocessed shuttle data, written to staging by DataProcessing.
  /// Promoted under FK constraint <c>Shuttles.CompanyId → Companies.Id</c>.
  /// </summary>
  public IItem<IEnumerable<PreprocessedShuttleSchema>> Shuttles =>
    CreateItem(
      () =>
        EFCoreItemFactory.Query.EFCore<PreprocessedShuttleSchema, StagingDbContext>(
          label: "StagingShuttles",
          contextFactory: _contextFactory,
          saveFunc: BulkSave.Insert<PreprocessedShuttleSchema, StagingDbContext>(),
          scope: DbScope.Explicit(SharedScope)
        )
    );

  /// <summary>
  /// Preprocessed review data, written to staging by DataProcessing.
  /// Promoted under FK constraint <c>Reviews.ShuttleId → Shuttles.Id</c>.
  /// </summary>
  public IItem<IEnumerable<PreprocessedReviewSchema>> Reviews =>
    CreateItem(
      () =>
        EFCoreItemFactory.Query.EFCore<PreprocessedReviewSchema, StagingDbContext>(
          label: "StagingReviews",
          contextFactory: _contextFactory,
          saveFunc: BulkSave.Insert<PreprocessedReviewSchema, StagingDbContext>(),
          scope: DbScope.Explicit(SharedScope)
        )
    );
}
