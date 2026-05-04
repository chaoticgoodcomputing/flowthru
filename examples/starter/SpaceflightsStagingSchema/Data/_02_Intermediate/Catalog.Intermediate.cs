using Flowthru.Core.Data;
using Flowthru.Extensions.EFCore.Data;
using SpaceflightsStagingSchema.Data._02_Intermediate.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class StagingCatalog
{
  /// <summary>
  /// Preprocessed company data, written to staging by DataProcessing and
  /// promoted to production with FK enforcement.
  /// </summary>
  public IItem<IEnumerable<PreprocessedCompanySchema>> Companies =>
    CreateItem(
      () =>
        EFCoreItemFactory.Enumerable.EFCore<PreprocessedCompanySchema, StagingDbContext>(
          label: "StagingCompanies",
          contextFactory: _contextFactory
        )
    );

  /// <summary>
  /// Preprocessed shuttle data, written to staging by DataProcessing.
  /// Promoted under FK constraint <c>Shuttles.CompanyId → Companies.Id</c>.
  /// </summary>
  public IItem<IEnumerable<PreprocessedShuttleSchema>> Shuttles =>
    CreateItem(
      () =>
        EFCoreItemFactory.Enumerable.EFCore<PreprocessedShuttleSchema, StagingDbContext>(
          label: "StagingShuttles",
          contextFactory: _contextFactory
        )
    );

  /// <summary>
  /// Preprocessed review data, written to staging by DataProcessing.
  /// Promoted under FK constraint <c>Reviews.ShuttleId → Shuttles.Id</c>.
  /// </summary>
  public IItem<IEnumerable<PreprocessedReviewSchema>> Reviews =>
    CreateItem(
      () =>
        EFCoreItemFactory.Enumerable.EFCore<PreprocessedReviewSchema, StagingDbContext>(
          label: "StagingReviews",
          contextFactory: _contextFactory
        )
    );
}
