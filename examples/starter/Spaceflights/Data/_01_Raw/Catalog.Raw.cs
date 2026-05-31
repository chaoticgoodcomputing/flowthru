#region docs:catalog-raw-all
using Flowthru.Data.Catalog;
using Spaceflights.Data._01_Raw.Schemas;

namespace Spaceflights.Data;

public partial class Catalog
{
  #region docs:catalog-raw-companies
  /// <summary>Raw company data imported from external sources.</summary>
  public IItem<IEnumerable<CompanySchema>> Companies =>
    CreateItem(() => Item.Of<IEnumerable<CompanySchema>>("Companies")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/companies.csv")
      .Build());
  #endregion

  /// <summary>Raw review data imported from external sources.</summary>
  public IItem<IEnumerable<ReviewSchema>> Reviews =>
    CreateItem(() => Item.Of<IEnumerable<ReviewSchema>>("Reviews")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/reviews.csv")
      .Build());

  /// <summary>Raw shuttle data imported from external sources (Excel).</summary>
  public IItem<IEnumerable<ShuttleSchema>> Shuttles =>
    CreateItem(() => Item.Of<IEnumerable<ShuttleSchema>>("Shuttles")
      .Excel()
      .AtPath($"{_basePath}/_01_Raw/Datasets/shuttles.xlsx")
      .WithSheet("Sheet1")
      .Build());
}
#endregion
