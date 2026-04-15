using Flowthru.Core.Data;
using RetailDataMultipipeline.Data._03_Primary.Schemas;

namespace RetailDataMultipipeline.Data;

/// <summary>
/// Primary-layer catalog entries for CoreCatalog.
/// </summary>
public partial class CoreCatalog
{
    /// <summary>
    /// All per-country weekly DTU shards concatenated into a single Parquet dataset.
    /// Produced by the Consolidation pipeline as the fan-in of all shard outputs.
    /// </summary>
    public IItem<IEnumerable<WeeklyDtuSchema>> AllCountriesWeeklyDtu =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Parquet<WeeklyDtuSchema>(
            label: "AllCountriesWeeklyDtu",
            filePath: $"{_basePath}/_03_Primary/Datasets/all_countries_weekly_dtu.parquet"
          )
      );
}
