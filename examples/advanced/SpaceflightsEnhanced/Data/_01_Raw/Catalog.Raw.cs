using Flowthru.Data.Catalog;
using SpaceflightsEnhanced.Data._01_Raw.Schemas;
using SpaceflightsEnhanced.Data._03_Primary.Schemas;

namespace SpaceflightsEnhanced.Data;

public partial class Catalog
{
  /// <summary>Raw company data from CSV file.</summary>
  public IItem<IEnumerable<CompanyRawSchema>> Companies =>
    CreateItem(() => Item.Of<IEnumerable<CompanyRawSchema>>("RawCompanies")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/companies.csv")
      .Build());

  /// <summary>Raw review data from CSV file.</summary>
  public IItem<IEnumerable<ReviewRawSchema>> Reviews =>
    CreateItem(() => Item.Of<IEnumerable<ReviewRawSchema>>("RawReviews")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/reviews.csv")
      .Build());

  /// <summary>Raw shuttle data from Excel file (read-only).</summary>
  public IItem<IEnumerable<ShuttleRawSchema>> Shuttles =>
    CreateItem(() => Item.Of<IEnumerable<ShuttleRawSchema>>("RawShuttles")
      .Excel()
      .AtPath($"{_basePath}/_01_Raw/Datasets/shuttles.xlsx")
      .WithSheet("Sheet1")
      .Build());

  /// <summary>Reference model input table from Kedro pipeline (for validation).</summary>
  public IItem<IEnumerable<KedroModelInputSchema>> KedroModelInputTable =>
    CreateItem(() => Item.Of<IEnumerable<KedroModelInputSchema>>("KedroModelInputTable")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/kedro_model_input_table.csv")
      .Build());
}
