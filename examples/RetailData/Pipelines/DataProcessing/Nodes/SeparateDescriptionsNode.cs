using RetailData.Data._02_Intermediate.Schemas;

namespace RetailData.Pipelines.DataProcessing.Nodes;

/// <summary>
/// Separates cleaned data into core transactions and stock descriptions
/// </summary>
public static class SeparateDescriptionsNode
{
  public static Func<
    IEnumerable<CleanedRetailSchema>,
    Task<(IEnumerable<CoreTransactionSchema>, IEnumerable<StockDescriptionSchema>)>
  > Create()
  {
    return async (input) =>
    {
      var inputList = input.ToList();

      // Extract core transactions (without descriptions)
      var coreTransactions = inputList.Select(record => new CoreTransactionSchema
      {
        InvoiceNo = record.InvoiceNo,
        StockCode = record.StockCode,
        Quantity = record.Quantity,
        InvoiceDate = record.InvoiceDate,
        UnitPrice = record.UnitPrice,
        CustomerID = record.CustomerID,
        Country = record.Country,
        TotalAmount = record.TotalAmount,
      });

      // Extract unique stock code -> description mappings
      var stockDescriptions = inputList
        .GroupBy(r => r.StockCode)
        .Select(g => new StockDescriptionSchema
        {
          StockCode = g.Key,
          Description =
            g.First().Description // Take first description for each stock code
          ,
        })
        .OrderBy(s => s.StockCode);

      return await Task.FromResult((coreTransactions, stockDescriptions));
    };
  }
}
