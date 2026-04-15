using Flowthru.Core.Data;
using KedroSpaceflights.Data._01_Raw.Schemas;

namespace KedroSpaceflights.Data;

public partial class Catalog
{
    /// <summary>
    /// Raw company data imported from external sources.
    /// </summary>
    public IItem<IEnumerable<CompanySchema>> Companies =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Csv<CompanySchema>(
            label: "Companies",
            filePath: $"{_basePath}/_01_Raw/Datasets/companies.csv"
          )
      );

    /// <summary>
    /// Raw review data imported from external sources.
    /// </summary>
    public IItem<IEnumerable<ReviewSchema>> Reviews =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Csv<ReviewSchema>(
            label: "Reviews",
            filePath: $"{_basePath}/_01_Raw/Datasets/reviews.csv"
          )
      );

    /// <summary>
    /// Raw shuttle data imported from external sources.
    /// </summary>
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
