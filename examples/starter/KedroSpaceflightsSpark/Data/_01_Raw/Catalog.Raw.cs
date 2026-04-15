using Flowthru.Core.Data;
using KedroSpaceflightsSpark.Data._01_Raw.Schemas;

namespace KedroSpaceflightsSpark.Data;

public partial class Catalog
{
  public IItem<IEnumerable<CompanySchema>> Companies =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<CompanySchema>(
          label: "Companies",
          filePath: $"{_basePath}/_01_Raw/Datasets/companies.csv"
        )
    );

  public IItem<IEnumerable<ReviewSchema>> Reviews =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<ReviewSchema>(
          label: "Reviews",
          filePath: $"{_basePath}/_01_Raw/Datasets/reviews.csv"
        )
    );

  public IItem<IEnumerable<ShuttleSchema>> Shuttles =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Excel<ShuttleSchema>(
          label: "Shuttles",
          filePath: $"{_basePath}/_01_Raw/Datasets/shuttles.xlsx",
          sheetName: "Sheet1"
        )
    );
}
