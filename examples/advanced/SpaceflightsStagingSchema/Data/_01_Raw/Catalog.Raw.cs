using Flowthru.Data.Catalog;
using SpaceflightsStagingSchema.Data._01_Raw.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class RawCatalog
{
  /// <summary>Raw company data imported from external sources.</summary>
  public IItem<IEnumerable<CompanySchema>> Companies =>
    CreateItem(() => Item.Of<IEnumerable<CompanySchema>>("Companies")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/companies.csv")
      .Build());

  /// <summary>Raw review data imported from external sources.</summary>
  public IItem<IEnumerable<ReviewSchema>> Reviews =>
    CreateItem(() => Item.Of<IEnumerable<ReviewSchema>>("Reviews")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/reviews.csv")
      .Build());

  /// <summary>Raw shuttle data imported from external sources.</summary>
  public IItem<IEnumerable<ShuttleSchema>> Shuttles =>
    CreateItem(() => Item.Of<IEnumerable<ShuttleSchema>>("Shuttles")
      .Excel()
      .AtPath($"{_basePath}/_01_Raw/Datasets/shuttles.xlsx")
      .WithSheet("Sheet1")
      .Build());
}
