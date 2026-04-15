using Flowthru.Core.Data;
using KedroSpaceflightsGQL.Data._02_Intermediate.Schemas;

namespace KedroSpaceflightsGQL.Data;

public partial class Catalog
{
    /// <summary>
    /// Preprocessed company data with validated and strongly-typed fields.
    /// </summary>
    public IItem<IEnumerable<PreprocessedCompanySchema>> PreprocessedCompanies =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Parquet<PreprocessedCompanySchema>(
            label: "PreprocessedCompanies",
            filePath: $"{_basePath}/_02_Intermediate/Datasets/preprocessed_companies.parquet"
          )
      );

    /// <summary>
    /// Preprocessed shuttle data with validated and strongly-typed fields.
    /// </summary>
    public IItem<IEnumerable<PreprocessedShuttleSchema>> PreprocessedShuttles =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Parquet<PreprocessedShuttleSchema>(
            label: "PreprocessedShuttles",
            filePath: $"{_basePath}/_02_Intermediate/Datasets/preprocessed_shuttles.parquet"
          )
      );

    /// <summary>
    /// Preprocessed review data with parsed decimal rating scores.
    /// </summary>
    public IItem<IEnumerable<PreprocessedReviewSchema>> PreprocessedReviews =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Parquet<PreprocessedReviewSchema>(
            label: "PreprocessedReviews",
            filePath: $"{_basePath}/_02_Intermediate/Datasets/preprocessed_reviews.parquet"
          )
      );
}
