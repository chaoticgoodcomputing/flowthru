using Flowthru.Abstractions;

namespace RetailData.Data._01_Raw.Schemas;

/// <summary>
/// Raw retail transaction data with all fields as strings
/// </summary>
public record RawRetailSchema : IFlatSchema, ITextSerializable
{
  [SerializedLabel("InvoiceNo")]
  public string InvoiceNo { get; init; } = null!;

  [SerializedLabel("StockCode")]
  public string StockCode { get; init; } = null!;

  [SerializedLabel("Description")]
  public string Description { get; init; } = null!;

  [SerializedLabel("Quantity")]
  public string Quantity { get; init; } = null!;

  [SerializedLabel("InvoiceDate")]
  public string InvoiceDate { get; init; } = null!;

  [SerializedLabel("UnitPrice")]
  public string UnitPrice { get; init; } = null!;

  [SerializedLabel("CustomerID")]
  public string CustomerID { get; init; } = null!;

  [SerializedLabel("Country")]
  public string Country { get; init; } = null!;
}
