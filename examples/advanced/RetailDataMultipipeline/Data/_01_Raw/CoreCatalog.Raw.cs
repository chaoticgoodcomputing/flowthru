using Flowthru.Data;
using RetailDataMultipipeline.Data._01_Raw.Schemas;

namespace RetailDataMultipipeline.Data;

public partial class CoreCatalog
{
  /// <summary>
  /// All daily retail transaction CSV files from the by-day directory, read as a
  /// single concatenated sequence. Read-only — immutable raw source data.
  /// </summary>
  public ICatalogEntry<IEnumerable<RetailTransactionSchema>> RetailTransactionsRaw =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.CsvDirectory<RetailTransactionSchema>(
          label: "RetailTransactionsRaw",
          directoryPath: $"{_basePath}/_01_Raw/Datasets"
        )
    );

  /// <summary>
  /// Country-to-currency mapping. Maintained independently of the OFX feed.
  /// </summary>
  public ICatalogEntry<IEnumerable<CountryCurrencySchema>> CountryCurrencies =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<CountryCurrencySchema>(
          label: "CountryCurrencies",
          filePath: $"{_basePath}/_01_Raw/Datasets/country_currencies.json"
        )
    );

  /// <summary>
  /// Stubbed OFX XXX/GBP/1000 responses — one per source currency.
  /// </summary>
  public ICatalogEntry<IEnumerable<OfxRateResponseSchema>> OfxRates =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<OfxRateResponseSchema>(
          label: "OfxRates",
          filePath: $"{_basePath}/_01_Raw/Datasets/ofx_rates.json"
        )
    );
}
