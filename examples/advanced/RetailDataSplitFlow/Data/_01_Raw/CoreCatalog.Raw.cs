using Flowthru.Data.Catalog;
using RetailDataMultipipeline.Data._01_Raw.Schemas;

namespace RetailDataMultipipeline.Data;

public partial class CoreCatalog
{
  /// <summary>
  /// Full online-retail dataset fetched over HTTP. The resolver routes
  /// the https:// URI through the HTTP storage medium at runtime.
  /// </summary>
  public IItem<IEnumerable<RetailTransactionSchema>> RetailTransactionsRaw =>
    CreateItem(() =>
    {
      var b = Item.Of<IEnumerable<RetailTransactionSchema>>("RetailTransactionsRaw")
        .Csv()
        .AtPath("https://raw.githubusercontent.com/databricks/Spark-The-Definitive-Guide/refs/heads/master/data/retail-data/all/online-retail-dataset.csv");
      if (_resolver is not null) b = b.WithResolver(_resolver);
      return b.Build();
    });

  public IItem<IEnumerable<CountryCurrencySchema>> CountryCurrencies =>
    CreateItem(() => Item.Of<IEnumerable<CountryCurrencySchema>>("CountryCurrencies")
      .Json()
      .AtPath($"{_basePath}/_01_Raw/Datasets/country_currencies.json")
      .Build());

  public IItem<IEnumerable<OfxRateResponseSchema>> OfxRates =>
    CreateItem(() => Item.Of<IEnumerable<OfxRateResponseSchema>>("OfxRates")
      .Json()
      .AtPath($"{_basePath}/_01_Raw/Datasets/ofx_rates.json")
      .Build());
}
