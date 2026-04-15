using Flowthru.Core.Abstractions;

namespace RetailDataMultipipeline.Data._03_Primary.Schemas;

/// <summary>
/// Weekly Dollars-Transactions-Users (DTU) metrics for a single country,
/// expressed in GBP after currency conversion.
/// </summary>
/// <remarks>
/// All monetary values are in GBP. Non-GBP countries have their <c>UnitPrice</c>
/// values multiplied by the OFX rate for their currency before aggregation.
/// Negative-quantity rows (returns) reduce <c>TotalGbp</c> naturally via signed arithmetic.
/// <c>WeekStartDate</c> is the Monday of each ISO week.
/// </remarks>
[FlowthruSchema]
public partial record WeeklyDtuSchema
{
    public required string Country { get; init; }
    public DateTime WeekStartDate { get; init; }
    public double TotalGbp { get; init; }
    public int TransactionCount { get; init; }
    public int UniqueCustomers { get; init; }
}
