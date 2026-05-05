using Flowthru.Core.Data;
using Flowthru.Extensions.EFCore.Bulk;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>
  /// Production companies. Promotion target for staging companies; read by
  /// downstream flows as a deferred queryable handle.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <strong>Save shape:</strong> <c>BulkSave.Insert</c> takes the promote
  /// step's filtered IEnumerable through Npgsql's binary <c>COPY</c> path —
  /// orders of magnitude faster than the change-tracker default for any
  /// non-trivial row count.
  /// </para>
  /// <para>
  /// <strong>Why not the fused <c>INSERT-FROM-SELECT</c> path?</strong>
  /// The framework's fused dispatch requires the source's <c>BuildQuery</c>
  /// to resolve against the destination's <c>DbContext</c>. Our two-DbContext
  /// design (one for each schema) means that resolution can't see staging
  /// tables from the production context. A single <c>DbContext</c> with
  /// shared-type entities for both schemas would unlock fused; that
  /// refactor is out of scope for this iteration. Bulk insert is the
  /// pragmatic optimization.
  /// </para>
  /// <para>
  /// <strong>Read shape:</strong> deferred <see cref="DbQuery{T}"/> via
  /// <see cref="EFCoreItemFactory.Query"/>, with the shared
  /// <see cref="DbScope.Explicit(string)"/> for forward-compatibility.
  /// </para>
  /// </remarks>
  public IItem<IEnumerable<PreprocessedCompanySchema>> Companies =>
    CreateItem(
      () =>
        EFCoreItemFactory.Query.EFCore<PreprocessedCompanySchema, ProductionDbContext>(
          label: "ProductionCompanies",
          contextFactory: _contextFactory,
          saveFunc: BulkSave.Insert<PreprocessedCompanySchema, ProductionDbContext>(),
          scope: DbScope.Explicit(StagingCatalog.SharedScope)
        )
    );

  /// <summary>
  /// Production shuttles. FK-constrained to <see cref="Companies"/>; promoting a
  /// shuttle whose <c>CompanyId</c> is not in <see cref="Companies"/> fails at
  /// the database layer.
  /// </summary>
  public IItem<IEnumerable<PreprocessedShuttleSchema>> Shuttles =>
    CreateItem(
      () =>
        EFCoreItemFactory.Query.EFCore<PreprocessedShuttleSchema, ProductionDbContext>(
          label: "ProductionShuttles",
          contextFactory: _contextFactory,
          saveFunc: BulkSave.Insert<PreprocessedShuttleSchema, ProductionDbContext>(),
          scope: DbScope.Explicit(StagingCatalog.SharedScope)
        )
    );

  /// <summary>
  /// Production reviews. FK-constrained to <see cref="Shuttles"/>; promoting a
  /// review whose <c>ShuttleId</c> is not in <see cref="Shuttles"/> fails at
  /// the database layer.
  /// </summary>
  public IItem<IEnumerable<PreprocessedReviewSchema>> Reviews =>
    CreateItem(
      () =>
        EFCoreItemFactory.Query.EFCore<PreprocessedReviewSchema, ProductionDbContext>(
          label: "ProductionReviews",
          contextFactory: _contextFactory,
          saveFunc: BulkSave.Insert<PreprocessedReviewSchema, ProductionDbContext>(),
          scope: DbScope.Explicit(StagingCatalog.SharedScope)
        )
    );
}
