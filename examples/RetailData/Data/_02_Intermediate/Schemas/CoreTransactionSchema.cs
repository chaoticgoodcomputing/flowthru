using Flowthru.Abstractions;

namespace RetailData.Data._02_Intermediate.Schemas;

/// <summary>
/// Core transaction data without descriptions
/// </summary>
public record CoreTransactionSchema : IFlatSchema, IBinarySerializable
{
  [SerializedLabel("InvoiceNo")]
  public string InvoiceNo { get; init; } = null!;

  [SerializedLabel("StockCode")]
  public string StockCode { get; init; } = null!;

  [SerializedLabel("Quantity")]
  public int Quantity { get; init; }

  [SerializedLabel("InvoiceDate")]
  public string InvoiceDate { get; init; } = null!;

  [SerializedLabel("UnitPrice")]
  public decimal UnitPrice { get; init; }

  [SerializedLabel("CustomerID")]
  public string CustomerID { get; init; } = null!;

  [SerializedLabel("Country")]
  public string Country { get; init; } = null!;

  [SerializedLabel("TotalAmount")]
  public decimal TotalAmount { get; init; }
}
