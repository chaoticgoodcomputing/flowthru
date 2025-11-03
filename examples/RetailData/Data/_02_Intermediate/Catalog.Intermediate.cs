using Flowthru.Data;
using RetailData.Data._02_Intermediate.Schemas;

namespace RetailData.Data;

public partial class Catalog
{
  /// <summary>
  /// Cleaned retail transaction data with proper types
  /// </summary>
  public ICatalogEntry<IEnumerable<CleanedRetailSchema>> CleanedRetailData =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<CleanedRetailSchema>(
          label: "CleanedRetailData",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/cleaned_retail.parquet"
        )
    );

  /// <summary>
  /// Stock code to description lookup table
  /// </summary>
  public ICatalogEntry<IEnumerable<StockDescriptionSchema>> StockDescriptions =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<StockDescriptionSchema>(
          label: "StockDescriptions",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/stock_descriptions.parquet"
        )
    );

  /// <summary>
  /// Core transaction data without descriptions
  /// </summary>
  public ICatalogEntry<IEnumerable<CoreTransactionSchema>> CoreTransactions =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<CoreTransactionSchema>(
          label: "CoreTransactions",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/core_transactions.parquet"
        )
    );
}
