using Flowthru.Data.Catalog;
using SpaceflightsGQL.Data._02_Intermediate.Schemas;

namespace SpaceflightsGQL.Data;

public partial class Catalog
{
  /// <summary>
  /// Preprocessed company data with validated and strongly-typed fields.
  /// </summary>
  public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedCompanySchema>>("PreprocessedCompanies")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_companies.parquet")
      .Build());

  /// <summary>
  /// Preprocessed shuttle data with validated and strongly-typed fields.
  /// </summary>
  public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedShuttleSchema>>("PreprocessedShuttles")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_shuttles.parquet")
      .Build());

  /// <summary>
  /// Preprocessed review data with parsed decimal rating scores.
  /// </summary>
  public IItem<IEnumerable<PreprocessedReviewSchema>> PreprocessedReviews =>
    CreateItem(() => Item.Of<IEnumerable<PreprocessedReviewSchema>>("PreprocessedReviews")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/preprocessed_reviews.parquet")
      .Build());
}
