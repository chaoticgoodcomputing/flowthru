using Flowthru.Data;
using RetailDataMultipipeline.Data._02_Intermediate.Schemas;

namespace RetailDataMultipipeline.Data;

public partial class CoreCatalog
{
  /// <summary>
  /// All retail transactions consolidated from the daily CSV files into a single
  /// Parquet dataset with fully-typed columns. This is the canonical "full history" view.
  /// </summary>
  public ICatalogEntry<IEnumerable<RetailTransactionIntermediateSchema>> AllRetailTransactions =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<RetailTransactionIntermediateSchema>(
          label: "AllRetailTransactions",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/all_retail_transactions.parquet"
        )
    );
}
