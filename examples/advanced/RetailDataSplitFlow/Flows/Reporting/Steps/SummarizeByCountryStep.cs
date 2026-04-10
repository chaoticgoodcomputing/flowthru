using Flowthru.Core.Steps;
using RetailDataMultipipeline.Data._02_Intermediate.Schemas;
using RetailDataMultipipeline.Data._08_Reporting.Schemas;

namespace RetailDataMultipipeline.Flows.Reporting.Steps;

/// <summary>
/// Groups all transactions by country and counts positive-quantity rows (debits)
/// and negative-quantity rows (credits/returns) for each.
/// </summary>
[FlowthruStep]
public static class SummarizeByCountryStep
{
  public static Func<
    IEnumerable<RetailTransactionIntermediateSchema>,
    IEnumerable<CountryTransactionSummarySchema>
  > Create()
  {
    return transactions =>
      transactions
        .GroupBy(t => t.Country)
        .Select(g => new CountryTransactionSummarySchema
        {
          Country = g.Key,
          Debits = g.Count(t => t.Quantity > 0),
          Credits = g.Count(t => t.Quantity < 0),
        })
        .OrderByDescending(s => s.Debits);
  }
}
