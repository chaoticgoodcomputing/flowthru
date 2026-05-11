using Flowthru.Data.Catalog;
using RetailDataMultipipeline.Data._03_Primary.Schemas;

namespace RetailDataMultipipeline.Data;

/// <summary>
/// Per-country catalog for weekly DTU analysis shards. Each instance is
/// labelled <c>{PascalCase(country)}ShardCatalog</c> so its entries
/// receive distinct qualified identifiers in DAG metadata
/// (e.g., <c>UnitedKingdomShardCatalog.WeeklyDtu</c>).
/// </summary>
public class CountryShardCatalog : CatalogAbstract
{
  private readonly string _basePath;

  public string Country { get; }

  public CountryShardCatalog(string country, string basePath)
    : base($"{ToPascalCase(country)}ShardCatalog")
  {
    Country = country;
    _basePath = basePath;
  }

  public IItem<IEnumerable<WeeklyDtuSchema>> WeeklyDtu =>
    CreateItem(() => Item.Of<IEnumerable<WeeklyDtuSchema>>($"WeeklyDtu_{Slugify(Country)}")
      .Parquet()
      .AtPath($"{_basePath}/_03_Primary/Datasets/weekly_dtu_{Slugify(Country)}.parquet")
      .Build());

  private static string ToPascalCase(string country) =>
    string.Concat(
      country.Split(' ', '.').Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w[1..].ToLower() : w)
    );

  private static string Slugify(string country) =>
    country.ToLowerInvariant().Replace(' ', '_').Replace('.', '_');
}
