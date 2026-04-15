using Flowthru.Core.Abstractions;

namespace RetailDataMultipipeline.Data._02_Intermediate.Schemas;

/// <summary>
/// Typed retail transaction record with proper .NET types, produced by parsing the
/// all-string <c>RetailTransactionSchema</c> raw layer.
/// </summary>
/// <remarks>
/// Type notes:
/// <list type="bullet">
/// <item><c>Quantity</c> is <c>int</c> and can be negative — negative quantities represent returns.</item>
/// <item><c>CustomerId</c> is nullable — guest transactions have no customer identifier.</item>
/// <item><c>Description</c> is nullable — a small number of rows have a blank description.</item>
/// <item>CustomerID arrives in the raw CSV as <c>"17850.0"</c> (float string); the consolidation
///   node casts via <c>double → int</c> to strip the spurious decimal.</item>
/// </list>
/// </remarks>
[FlowthruSchema]
public partial record RetailTransactionIntermediateSchema
{
    public required string InvoiceNo { get; init; }
    public required string StockCode { get; init; }
    public string? Description { get; init; }
    public int Quantity { get; init; }
    public DateTime InvoiceDate { get; init; }
    public decimal UnitPrice { get; init; }
    public int? CustomerId { get; init; }
    public required string Country { get; init; }
}
