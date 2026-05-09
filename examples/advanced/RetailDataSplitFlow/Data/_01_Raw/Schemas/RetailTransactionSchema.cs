using Flowthru.Data.Schema;

namespace RetailDataMultipipeline.Data._01_Raw.Schemas;

/// <summary>
/// Raw retail transaction record as it appears in the Spark: The Definitive Guide
/// by-day CSV files. All fields are strings — type coercion is deferred to the
/// Intermediate layer.
/// </summary>
[FlowthruSchema]
public partial record RetailTransactionSchema
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
