using Flowthru.Abstractions;

namespace RetailData.Data._02_Intermediate.Schemas;

/// <summary>
/// Cleaned retail transaction data with proper types
/// </summary>
public record CleanedRetailSchema : IFlatSchema, IBinarySerializable
{
  [SerializedLabel("InvoiceNo")]
  public string InvoiceNo { get; init; } = null!;

  [SerializedLabel("StockCode")]
  public string StockCode { get; init; } = null!;

  [SerializedLabel("Description")]
  public string Description { get; init; } = null!;

  [SerializedLabel("Quantity")]
  public int Quantity { get; init; }

  [SerializedLabel("InvoiceDate")]
  public string InvoiceDate { get; init; } = null!; // Date only (no time)

  [SerializedLabel("UnitPrice")]
  public decimal UnitPrice { get; init; }

  [SerializedLabel("CustomerID")]
  public string CustomerID { get; init; } = null!;

  [SerializedLabel("Country")]
  public string Country { get; init; } = null!;

  [SerializedLabel("TotalAmount")]
  public decimal TotalAmount { get; init; } // Quantity * UnitPrice
}
