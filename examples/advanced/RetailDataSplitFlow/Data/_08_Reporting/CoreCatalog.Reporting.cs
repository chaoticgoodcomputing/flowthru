using Flowthru.Data.Catalog;
using RetailDataMultipipeline.Data._08_Reporting.Schemas;

namespace RetailDataMultipipeline.Data;

public partial class CoreCatalog
{
  public IItem<IEnumerable<CountryTransactionSummarySchema>> CountryTransactionSummary =>
    CreateItem(() => Item.Of<IEnumerable<CountryTransactionSummarySchema>>("CountryTransactionSummary")
      .Csv()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/country_transaction_summary.csv")
      .Build());

  public IItem<byte[]> DollarsChart =>
    CreateItem(() => Item.Of<byte[]>("DollarsChart")
      .Binary()
      .AtPath($"{_basePath}/_08_Reporting/Charts/dollars_chart.png")
      .Build());

  public IItem<byte[]> TransactionsChart =>
    CreateItem(() => Item.Of<byte[]>("TransactionsChart")
      .Binary()
      .AtPath($"{_basePath}/_08_Reporting/Charts/transactions_chart.png")
      .Build());

  public IItem<byte[]> UsersChart =>
    CreateItem(() => Item.Of<byte[]>("UsersChart")
      .Binary()
      .AtPath($"{_basePath}/_08_Reporting/Charts/users_chart.png")
      .Build());
}
