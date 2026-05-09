using Flowthru.Data.Catalog;
using RetailDataMultipipeline.Data._02_Intermediate.Schemas;

namespace RetailDataMultipipeline.Data;

public partial class CoreCatalog
{
  public IItem<IEnumerable<RetailTransactionIntermediateSchema>> AllRetailTransactions =>
    CreateItem(() => Item.Of<IEnumerable<RetailTransactionIntermediateSchema>>("AllRetailTransactions")
      .Parquet()
      .AtPath($"{_basePath}/_02_Intermediate/Datasets/all_retail_transactions.parquet")
      .Build());
}
