using Flowthru.Data;
using SpaceflightsPythonEFCore.Data._01_Raw.Schemas;

namespace SpaceflightsPythonEFCore.Data;

public partial class Catalog
{
  public ICatalogEntry<IEnumerable<CompanySchema>> Companies =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<CompanySchema>(
          label: "Companies",
          filePath: $"{_basePath}/_01_Raw/Datasets/companies.csv"
        )
    );

  public ICatalogEntry<IEnumerable<ReviewSchema>> Reviews =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<ReviewSchema>(
          label: "Reviews",
          filePath: $"{_basePath}/_01_Raw/Datasets/reviews.csv"
        )
    );

  public ICatalogEntry<IEnumerable<ShuttleSchema>> Shuttles =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Excel<ShuttleSchema>(
          label: "Shuttles",
          filePath: $"{_basePath}/_01_Raw/Datasets/shuttles.xlsx",
          sheetName: "Sheet1"
        )
    );
}
