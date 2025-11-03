using Flowthru.Data;
using RetailData.Data._03_Primary.Schemas;

namespace RetailData.Data;

public partial class Catalog
{
  /// <summary>
  /// Aggregated daily DTU metrics by country
  /// </summary>
  public ICatalogEntry<IEnumerable<DailyDtuSchema>> DailyDtuByCountry =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<DailyDtuSchema>(
          label: "DailyDtuByCountry",
          filePath: $"{_basePath}/_03_Primary/Datasets/daily_dtu_by_country.csv"
        )
    );

  /// <summary>
  /// Aggregated daily DTU metrics by region
  /// </summary>
  public ICatalogEntry<IEnumerable<DailyDtuByRegionSchema>> DailyDtuByRegion =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<DailyDtuByRegionSchema>(
          label: "DailyDtuByRegion",
          filePath: $"{_basePath}/_03_Primary/Datasets/daily_dtu_by_region.csv"
        )
    );

  /// <summary>
  /// Country correlation analysis
  /// </summary>
  public ICatalogEntry<IEnumerable<CountryCorrelationSchema>> CountryCorrelations =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<CountryCorrelationSchema>(
          label: "CountryCorrelations",
          filePath: $"{_basePath}/_03_Primary/Datasets/country_correlations.csv"
        )
    );

  /// <summary>
  /// Region correlation analysis
  /// </summary>
  public ICatalogEntry<IEnumerable<RegionCorrelationSchema>> RegionCorrelations =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<RegionCorrelationSchema>(
          label: "RegionCorrelations",
          filePath: $"{_basePath}/_03_Primary/Datasets/region_correlations.csv"
        )
    );

  // Dynamic per-country DTU entries
  public ICatalogEntry<IEnumerable<DailyDtuSchema>> GetCountryDtu(string country)
  {
    var sanitizedCountry = country.Replace(" ", "_").Replace("/", "_");
    return GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<DailyDtuSchema>(
          label: $"DailyDtu_{sanitizedCountry}",
          filePath: $"{_basePath}/_03_Primary/Datasets/daily_dtu_{sanitizedCountry}.csv"
        )
    );
  }
}
