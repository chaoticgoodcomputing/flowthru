using Flowthru.Data;
using RetailDataMultipipeline.Data._03_Primary.Schemas;

namespace RetailDataMultipipeline.Data;

/// <summary>
/// Per-country catalog for weekly DTU analysis shards.
/// </summary>
/// <remarks>
/// Each instance is labelled <c>{PascalCase(country)}ShardCatalog</c> so that its entries
/// receive distinct qualified identifiers in the DAG metadata:
/// e.g., <c>UnitedKingdomShardCatalog.WeeklyDtu</c>.
/// </remarks>
public class CountryShardCatalog : CatalogAbstract
{
  private readonly string _basePath;

  public string Country { get; }

  public CountryShardCatalog(string country, string basePath)
    : base($"{ToPascalCase(country)}ShardCatalog")
  {
    Country = country;
    _basePath = basePath;
    InitializeCatalogProperties();
  }

  /// <summary>
  /// Per-country weekly DTU Parquet shard, currency-converted to GBP.
  /// </summary>
  public IItem<IEnumerable<WeeklyDtuSchema>> WeeklyDtu =>
    CreateItem(
      () =>
        Items.Enumerable.Parquet<WeeklyDtuSchema>(
          label: $"WeeklyDtu_{Slugify(Country)}",
          filePath: $"{_basePath}/_03_Primary/Datasets/weekly_dtu_{Slugify(Country)}.parquet"
        )
    );

  private static string ToPascalCase(string country) =>
    string.Concat(
      country.Split(' ', '.').Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w[1..].ToLower() : w)
    );

  private static string Slugify(string country) =>
    country.ToLowerInvariant().Replace(' ', '_').Replace('.', '_');
}
