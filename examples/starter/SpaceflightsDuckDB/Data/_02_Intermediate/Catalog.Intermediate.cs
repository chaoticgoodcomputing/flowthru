using Flowthru.Data.Catalog;
using SpaceflightsDuckDB.Data._02_Intermediate.Schemas;

namespace SpaceflightsDuckDB.Data;

public partial class Catalog
{
  /// <summary>Preprocessed company data with validated and strongly-typed fields.</summary>
  public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedCompanySchema>>("PreprocessedCompanies")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_companies.parquet")
      .Build());

  /// <summary>Preprocessed shuttle data with validated and strongly-typed fields.</summary>
  public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("PreprocessedShuttles")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_shuttles.parquet")
      .Build());

  /// <summary>Preprocessed review data with a validated, strongly-typed score.</summary>
  public IItem<IEnumerable<PreprocessedReviewSchema>> PreprocessedReviews =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedReviewSchema>>("PreprocessedReviews")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_reviews.parquet")
      .Build());
}
