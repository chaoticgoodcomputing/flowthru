using Flowthru.Data;
using SpaceflightsDistributed.DataProcessing.Data._01_Raw.Schemas;

namespace SpaceflightsDistributed.DataProcessing.Data;

public partial class DataProcessingCatalog
{
  public IItem<IEnumerable<CompanySchema>> Companies =>
    CreateItem(
      () =>
        Items.Enumerable.Csv<CompanySchema>(
          label: "Companies",
          filePath: $"{_basePath}/_01_Raw/Datasets/companies.csv"
        )
    );

  public IItem<IEnumerable<ReviewSchema>> Reviews =>
    CreateItem(
      () =>
        Items.Enumerable.Csv<ReviewSchema>(
          label: "Reviews",
          filePath: $"{_basePath}/_01_Raw/Datasets/reviews.csv"
        )
    );

  public IItem<IEnumerable<ShuttleSchema>> Shuttles =>
    CreateItem(
      () =>
        Items.Enumerable.Excel<ShuttleSchema>(
          label: "Shuttles",
          filePath: $"{_basePath}/_01_Raw/Datasets/shuttles.xlsx",
          sheetName: "Sheet1"
        )
    );
}
