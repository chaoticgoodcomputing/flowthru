using Flowthru.Data.Catalog;
using SpaceflightsPythonEFCore.Data._01_Raw.Schemas;

namespace SpaceflightsPythonEFCore.Data;

public partial class Catalog
{
  public IItem<IEnumerable<CompanySchema>> Companies =>
    CreateItem(() => Item.Of<IEnumerable<CompanySchema>>("Companies")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/companies.csv")
      .Build());

  public IItem<IEnumerable<ReviewSchema>> Reviews =>
    CreateItem(() => Item.Of<IEnumerable<ReviewSchema>>("Reviews")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/reviews.csv")
      .Build());

  public IItem<IEnumerable<ShuttleSchema>> Shuttles =>
    CreateItem(() => Item.Of<IEnumerable<ShuttleSchema>>("Shuttles")
      .Excel()
      .AtPath($"{_basePath}/_01_Raw/Datasets/shuttles.xlsx")
      .WithSheet("Sheet1")
      .Build());
}
