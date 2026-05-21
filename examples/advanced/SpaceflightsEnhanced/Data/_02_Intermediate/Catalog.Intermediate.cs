using Flowthru.Data.Catalog;
using SpaceflightsEnhanced.Data._02_Intermediate.Schemas;

namespace SpaceflightsEnhanced.Data;

public partial class Catalog
{
  /// <summary>Preprocessed company data in Parquet format.</summary>
  public IItem<IEnumerable<CompanySchema>> CleanedCompanies =>
    CreateItem(() => Item.Of<IEnumerable<CompanySchema>>("CleanedCompanies")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/cleaned_companies.parquet")
      .Build());

  /// <summary>Preprocessed shuttle data in Parquet format.</summary>
  public IItem<IEnumerable<ShuttleSchema>> CleanedShuttles =>
    CreateItem(() => Item.Of<IEnumerable<ShuttleSchema>>("CleanedShuttles")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/cleaned_shuttles.parquet")
      .Build());

  /// <summary>Preprocessed review data in Parquet format.</summary>
  public IItem<IEnumerable<ReviewSchema>> CleanedReviews =>
    CreateItem(() => Item.Of<IEnumerable<ReviewSchema>>("CleanedReviews")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/cleaned_reviews.parquet")
      .Build());

  /// <summary>Preprocessed companies exported as CSV (for debugging).</summary>
  public IItem<IEnumerable<CompanySchema>> CleanedCompaniesCsv =>
    CreateItem(() => Item.Of<IEnumerable<CompanySchema>>("CleanedCompaniesCsv")
      .Csv()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/cleaned_companies.csv")
      .Build());

  /// <summary>Preprocessed shuttles exported as CSV (for debugging).</summary>
  public IItem<IEnumerable<ShuttleSchema>> CleanedShuttlesCsv =>
    CreateItem(() => Item.Of<IEnumerable<ShuttleSchema>>("CleanedShuttlesCsv")
      .Csv()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/cleaned_shuttles.csv")
      .Build());
}
