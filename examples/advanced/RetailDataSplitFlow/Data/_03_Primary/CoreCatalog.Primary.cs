using Flowthru.Data.Catalog;
using RetailDataMultipipeline.Data._03_Primary.Schemas;

namespace RetailDataMultipipeline.Data;

public partial class CoreCatalog
{
  /// <summary>
  /// All per-country weekly DTU shards concatenated into a single Parquet dataset.
  /// Produced by the variadic-input fan-in step in ConsolidationFlow.
  /// </summary>
  public IItem<IEnumerable<WeeklyDtuSchema>> AllCountriesWeeklyDtu =>
    CreateItem(() => Item.Of<IEnumerable<WeeklyDtuSchema>>("AllCountriesWeeklyDtu")
      .Parquet()
      .AtPath($"{_basePath}/_03_Primary/Datasets/all_countries_weekly_dtu.parquet")
      .Build());
}
