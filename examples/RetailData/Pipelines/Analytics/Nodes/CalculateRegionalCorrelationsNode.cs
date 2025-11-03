using RetailData.Data._03_Primary.Schemas;

namespace RetailData.Pipelines.Analytics.Nodes;

/// <summary>
/// Calculates correlation coefficients between regions' DTU metrics
/// </summary>
public static class CalculateRegionalCorrelationsNode
{
  public static Func<
    IEnumerable<DailyDtuByRegionSchema>,
    Task<IEnumerable<RegionCorrelationSchema>>
  > Create()
  {
    return async (input) =>
    {
      var dataByRegion = input
        .GroupBy(d => d.Region)
        .ToDictionary(g => g.Key, g => g.OrderBy(d => d.Date).ToList());

      var regions = dataByRegion.Keys.OrderBy(r => r).ToList();
      var correlations = new List<RegionCorrelationSchema>();

      // Calculate pairwise correlations
      for (int i = 0; i < regions.Count; i++)
      {
        for (int j = i + 1; j < regions.Count; j++)
        {
          var region1 = regions[i];
          var region2 = regions[j];

          var data1 = dataByRegion[region1];
          var data2 = dataByRegion[region2];

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
            new RegionCorrelationSchema
            {
              Region1 = region1,
              Region2 = region2,
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

      return await Task.FromResult(correlations.OrderBy(c => c.Region1).ThenBy(c => c.Region2));
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
