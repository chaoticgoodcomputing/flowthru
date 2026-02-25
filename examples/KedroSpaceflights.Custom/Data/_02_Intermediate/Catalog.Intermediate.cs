using Flowthru.Data;
using KedroSpaceflights.Custom.Data._02_Intermediate.Schemas;

namespace KedroSpaceflights.Custom.Data;

public partial class Catalog
{
  /// <summary>
  /// Preprocessed company data in Parquet format.
  /// Cleaned and validated company records.
  /// </summary>
  public ICatalogEntry<IEnumerable<CompanySchema>> CleanedCompanies =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<CompanySchema>(
          label: "CleanedCompanies",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/cleaned_companies.parquet"
        )
    );

  /// <summary>
  /// Preprocessed shuttle data in Parquet format.
  /// Cleaned and validated shuttle records.
  /// </summary>
  public ICatalogEntry<IEnumerable<ShuttleSchema>> CleanedShuttles =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<ShuttleSchema>(
          label: "CleanedShuttles",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/cleaned_shuttles.parquet"
        )
    );

  /// <summary>
  /// Preprocessed review data in Parquet format.
  /// Cleaned and validated review records with parsed numeric scores.
  /// </summary>
  public ICatalogEntry<IEnumerable<ReviewSchema>> CleanedReviews =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<ReviewSchema>(
          label: "CleanedReviews",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/cleaned_reviews.parquet"
        )
    );

  /// <summary>
  /// Preprocessed companies exported as CSV (for debugging).
  /// </summary>
  public ICatalogEntry<IEnumerable<CompanySchema>> CleanedCompaniesCsv =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<CompanySchema>(
          label: "CleanedCompaniesCsv",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/cleaned_companies.csv"
        )
    );

  /// <summary>
  /// Preprocessed shuttles exported as CSV (for debugging).
  /// </summary>
  public ICatalogEntry<IEnumerable<ShuttleSchema>> CleanedShuttlesCsv =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<ShuttleSchema>(
          label: "CleanedShuttlesCsv",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/cleaned_shuttles.csv"
        )
    );
}
