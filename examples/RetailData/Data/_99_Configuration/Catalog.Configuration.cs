using Flowthru.Data;
using RetailData.Data._99_Configuration.Schemas;

namespace RetailData.Data;

public partial class Catalog
{
  /// <summary>
  /// Configuration mapping countries to business regions
  /// </summary>
  public ICatalogEntry<CountryRegionMapping> CountryRegionMapping =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<CountryRegionMapping>(
          label: "CountryRegionMapping",
          filePath: $"{_basePath}/_99_Configuration/Datasets/country_region_mapping.json"
        )
    );
}
