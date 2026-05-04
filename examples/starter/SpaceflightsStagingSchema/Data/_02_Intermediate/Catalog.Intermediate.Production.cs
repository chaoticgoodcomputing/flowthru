using Flowthru.Core.Data;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>
  /// Production companies. Promotion target for staging companies; read by
  /// downstream flows as a deferred queryable handle.
  /// </summary>
  public IItem<IEnumerable<PreprocessedCompanySchema>> Companies =>
    CreateItem(
      () =>
        EFCoreItemFactory.Query.EFCore<PreprocessedCompanySchema, ProductionDbContext>(
          label: "ProductionCompanies",
          contextFactory: _contextFactory
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
          contextFactory: _contextFactory
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
          contextFactory: _contextFactory
        )
    );
}
