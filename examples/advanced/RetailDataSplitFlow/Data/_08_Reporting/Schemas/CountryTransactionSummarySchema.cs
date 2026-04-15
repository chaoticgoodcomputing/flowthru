using Flowthru.Core.Abstractions;

namespace RetailDataMultipipeline.Data._08_Reporting.Schemas;

/// <summary>
/// Per-country transaction summary: counts of positive-quantity (debit) and
/// negative-quantity (credit/return) line items.
/// </summary>
[FlowthruSchema]
public partial record CountryTransactionSummarySchema
{
    public required string Country { get; init; }
    public required int Debits { get; init; }
    public required int Credits { get; init; }
}
