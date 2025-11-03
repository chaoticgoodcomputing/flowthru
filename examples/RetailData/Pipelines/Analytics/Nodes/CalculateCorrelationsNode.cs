using RetailData.Data._03_Primary.Schemas;

namespace RetailData.Pipelines.Analytics.Nodes;

/// <summary>
/// Calculates correlation coefficients between countries' DTU metrics
/// </summary>
public static class CalculateCorrelationsNode
{
  public static Func<
    IEnumerable<DailyDtuSchema>,
    Task<IEnumerable<CountryCorrelationSchema>>
  > Create()
  {
    return async (input) =>
    {
      var dataByCountry = input
        .GroupBy(d => d.Country)
        .ToDictionary(g => g.Key, g => g.OrderBy(d => d.Date).ToList());

      var countries = dataByCountry.Keys.OrderBy(c => c).ToList();
      var correlations = new List<CountryCorrelationSchema>();

      // Calculate pairwise correlations
      for (int i = 0; i < countries.Count; i++)
      {
        for (int j = i + 1; j < countries.Count; j++)
        {
          var country1 = countries[i];
          var country2 = countries[j];

          var data1 = dataByCountry[country1];
          var data2 = dataByCountry[country2];

          // Find common dates
          var commonDates = data1
            .Select(d => d.Date)
            .Intersect(data2.Select(d => d.Date))
            .ToHashSet();

          if (commonDates.Count < 2)
            continue; // Need at least 2 points for correlation

          var pairs1 = data1.Where(d => commonDates.Contains(d.Date)).OrderBy(d => d.Date).ToList();
          var pairs2 = data2.Where(d => commonDates.Contains(d.Date)).OrderBy(d => d.Date).ToList();

          correlations.Add(
            new CountryCorrelationSchema
            {
              Country1 = country1,
              Country2 = country2,
              DollarsCorrelation = CalculatePearsonCorrelation(
                pairs1.Select(p => (double)p.Dollars),
                pairs2.Select(p => (double)p.Dollars)
              ),
              TransactionsCorrelation = CalculatePearsonCorrelation(
                pairs1.Select(p => (double)p.Transactions),
                pairs2.Select(p => (double)p.Transactions)
              ),
              UsersCorrelation = CalculatePearsonCorrelation(
                pairs1.Select(p => (double)p.Users),
                pairs2.Select(p => (double)p.Users)
              ),
            }
          );
        }
      }

      return await Task.FromResult(correlations.OrderBy(c => c.Country1).ThenBy(c => c.Country2));
    };
  }

  private static double CalculatePearsonCorrelation(IEnumerable<double> x, IEnumerable<double> y)
  {
    var xList = x.ToList();
    var yList = y.ToList();

    if (xList.Count != yList.Count || xList.Count == 0)
      return 0.0;

    var n = xList.Count;
    var xMean = xList.Average();
    var yMean = yList.Average();

    var numerator = 0.0;
    var xVariance = 0.0;
    var yVariance = 0.0;

    for (int i = 0; i < n; i++)
    {
      var xDiff = xList[i] - xMean;
      var yDiff = yList[i] - yMean;

      numerator += xDiff * yDiff;
      xVariance += xDiff * xDiff;
      yVariance += yDiff * yDiff;
    }

    if (xVariance == 0 || yVariance == 0)
      return 0.0;

    return numerator / Math.Sqrt(xVariance * yVariance);
  }
}
