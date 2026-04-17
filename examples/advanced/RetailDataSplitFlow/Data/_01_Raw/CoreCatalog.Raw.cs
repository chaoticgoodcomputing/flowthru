using Flowthru.Core.Data;
using RetailDataMultipipeline.Data._01_Raw.Schemas;

namespace RetailDataMultipipeline.Data;

public partial class CoreCatalog
{
  /// <summary>
  /// Full online-retail dataset downloaded from the Spark: The Definitive Guide
  /// GitHub repository. The resolver routes this https:// URI through
  /// HttpStorageMedium at runtime; local file paths fall back to FileStorageMedium.
  /// Read-only — immutable raw source data.
  /// </summary>
  public IItem<IEnumerable<RetailTransactionSchema>> RetailTransactionsRaw =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<RetailTransactionSchema>(
          label: "RetailTransactionsRaw",
          filePath: "https://raw.githubusercontent.com/databricks/Spark-The-Definitive-Guide/refs/heads/master/data/retail-data/all/online-retail-dataset.csv",
          resolver: _resolver
        )
    );

  /// <summary>
  /// Country-to-currency mapping. Maintained independently of the OFX feed.
  /// </summary>
  public IItem<IEnumerable<CountryCurrencySchema>> CountryCurrencies =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<CountryCurrencySchema>(
          label: "CountryCurrencies",
          filePath: $"{_basePath}/_01_Raw/Datasets/country_currencies.json"
        )
    );

  /// <summary>
  /// Stubbed OFX XXX/GBP/1000 responses — one per source currency.
  /// </summary>
  public IItem<IEnumerable<OfxRateResponseSchema>> OfxRates =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<OfxRateResponseSchema>(
          label: "OfxRates",
          filePath: $"{_basePath}/_01_Raw/Datasets/ofx_rates.json"
        )
    );
}
