using Flowthru.Core.Data;
using RetailDataMultipipeline.Data._08_Reporting.Schemas;

namespace RetailDataMultipipeline.Data;

public partial class CoreCatalog
{
  public IItem<IEnumerable<CountryTransactionSummarySchema>> CountryTransactionSummary =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<CountryTransactionSummarySchema>(
          label: "CountryTransactionSummary",
          filePath: $"{_basePath}/_08_Reporting/Datasets/country_transaction_summary.csv"
        )
    );

  /// <summary>Daily GBP revenue per country — line chart (PNG).</summary>
  public IItem<byte[]> DollarsChart =>
    CreateItem(
      () =>
        ItemFactory.Single.Binary(
          label: "DollarsChart",
          filePath: $"{_basePath}/_08_Reporting/Charts/dollars_chart.png"
        )
    );

  /// <summary>Daily transaction count per country — line chart (PNG).</summary>
  public IItem<byte[]> TransactionsChart =>
    CreateItem(
      () =>
        ItemFactory.Single.Binary(
          label: "TransactionsChart",
          filePath: $"{_basePath}/_08_Reporting/Charts/transactions_chart.png"
        )
    );

  /// <summary>Daily unique customers per country — line chart (PNG).</summary>
  public IItem<byte[]> UsersChart =>
    CreateItem(
      () =>
        ItemFactory.Single.Binary(
          label: "UsersChart",
          filePath: $"{_basePath}/_08_Reporting/Charts/users_chart.png"
        )
    );
}
