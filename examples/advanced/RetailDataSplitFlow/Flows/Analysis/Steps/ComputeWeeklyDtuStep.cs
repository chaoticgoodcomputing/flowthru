using Flowthru.Core.Steps;
using RetailDataMultipipeline.Data._01_Raw.Schemas;
using RetailDataMultipipeline.Data._02_Intermediate.Schemas;
using RetailDataMultipipeline.Data._03_Primary.Schemas;

namespace RetailDataMultipipeline.Flows.Analysis.Steps;

/// <summary>
/// Filters transactions for a single country, applies OFX currency conversion,
/// and aggregates into weekly DTU (Dollars-Transactions-Users) metrics.
/// </summary>
/// <remarks>
/// Join chain:
/// <code>
/// transaction.Country
///   → CountryCurrencies  (Country  → Currency)
///   → OfxRates           (Currency → ofxRate)
///   → UnitPrice × Quantity × ofxRate  (GBP result)
/// </code>
/// GBP passes through with <c>ofxRate = 1.0</c> — no special casing.
/// Negative quantities (returns) reduce <c>TotalGbp</c> via signed arithmetic.
/// Weeks are ISO-style: Monday-aligned.
/// </remarks>
[FlowthruStep]
public static class ComputeWeeklyDtuStep
{
  /// <summary>Returns the Monday of the ISO week containing <paramref name="date"/>.</summary>
  private static DateTime WeekStart(DateTime date) =>
    date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

  public static Func<
    (
      IEnumerable<RetailTransactionIntermediateSchema> Transactions,
      IEnumerable<CountryCurrencySchema> CountryCurrencies,
      IEnumerable<OfxRateResponseSchema> OfxRates
    ),
    IEnumerable<WeeklyDtuSchema>
  > Create(string country)
  {
    return input =>
    {
      var (transactions, countryCurrencies, ofxRates) = input;

      // Build lookup tables (materialised once per pipeline execution)
      var currencyByCountry = countryCurrencies.ToDictionary(
        c => c.Country,
        c => c.Currency,
        StringComparer.OrdinalIgnoreCase
      );

      var rateByCurrency = ofxRates.ToDictionary(
        r => r.Currency,
        r => r.ofxRate,
        StringComparer.OrdinalIgnoreCase
      );

      // Resolve this country's OFX rate via the two-hop join
      if (!currencyByCountry.TryGetValue(country, out var currency))
      {
        throw new InvalidOperationException(
          $"No currency mapping found for country '{country}'. "
            + $"Add an entry to country_currencies.json."
        );
      }

      if (!rateByCurrency.TryGetValue(currency, out var ofxRate))
      {
        throw new InvalidOperationException(
          $"No OFX rate found for currency '{currency}' (country: '{country}'). "
            + $"Add an entry to ofx_rates.json."
        );
      }

      return transactions
        .Where(t => string.Equals(t.Country, country, StringComparison.OrdinalIgnoreCase))
        .GroupBy(t => WeekStart(t.InvoiceDate))
        .Select(g => new WeeklyDtuSchema
        {
          Country = country,
          WeekStartDate = g.Key,
          TotalGbp = (double)g.Sum(t => t.UnitPrice * t.Quantity * ofxRate),
          TransactionCount = g.Count(),
          UniqueCustomers = g.Select(t => t.CustomerId).Where(id => id.HasValue).Distinct().Count(),
        })
        .OrderBy(d => d.WeekStartDate);
    };
  }
}
