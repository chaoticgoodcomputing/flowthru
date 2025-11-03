using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using RetailData.Data._05_Reporting.Schemas;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace RetailData.Pipelines.Reporting.Nodes;

/// <summary>
/// Generates a correlation heatmap showing relationships between groups.
/// Creates three heatmaps for Dollars, Transactions, and Users correlations.
/// </summary>
public static class GenerateCorrelationHeatmapNode
{
  public static Func<IEnumerable<CorrelationHeatmapSchema>, Task<GenericChart>> Create()
  {
    return async (input) =>
    {
      var data = input.ToList();
      var groupingType = data.FirstOrDefault()?.GroupingType ?? "Unknown";

      // Get all unique groups (initially unsorted)
      var allGroups = data.SelectMany(d => new[] { d.Group1, d.Group2 })
        .Distinct()
        .OrderBy(g => g)
        .ToList();

      // Create initial correlation matrices to determine optimal sorting
      var dollarsMatrix = BuildCorrelationMatrix(data, allGroups, d => d.DollarsCorrelation);
      var transactionsMatrix = BuildCorrelationMatrix(
        data,
        allGroups,
        d => d.TransactionsCorrelation
      );
      var usersMatrix = BuildCorrelationMatrix(data, allGroups, d => d.UsersCorrelation);

      // Apply hierarchical clustering to reorder groups for better visualization
      // This places highly correlated groups near each other
      var sortedGroups = HierarchicalClusterSort(allGroups, dollarsMatrix);

      // Rebuild matrices with sorted order
      var sortedDollarsMatrix = ReorderMatrix(dollarsMatrix, allGroups, sortedGroups);
      var sortedTransactionsMatrix = ReorderMatrix(transactionsMatrix, allGroups, sortedGroups);
      var sortedUsersMatrix = ReorderMatrix(usersMatrix, allGroups, sortedGroups);

      // Custom gray->red colorscale for correlation heatmaps
      // Gradient: white (low correlation) -> light gray -> red -> dark red (high correlation)
      var redColorscale = StyleParam.Colorscale.NewCustom(
        new List<System.Tuple<double, Color>>
        {
          System.Tuple.Create(0.0, Color.fromHex("#FFFFFF")),
          System.Tuple.Create(1.0, Color.fromHex("#990000")),
        }
      );

      // Create three heatmaps with red colorscale using sorted groups
      var dollarsHeatmap = CSharpChart
        .Heatmap<double, string, string, double>(
          zData: sortedDollarsMatrix,
          X: sortedGroups,
          Y: sortedGroups,
          ColorScale: redColorscale
        )
        .WithTitle($"Dollars Correlation - {groupingType}")
        .WithXAxisStyle(Title.init($"{groupingType}"))
        .WithYAxisStyle(Title.init($"{groupingType}"));

      var transactionsHeatmap = CSharpChart
        .Heatmap<double, string, string, double>(
          zData: sortedTransactionsMatrix,
          X: sortedGroups,
          Y: sortedGroups,
          ColorScale: redColorscale
        )
        .WithTitle($"Transactions Correlation - {groupingType}")
        .WithXAxisStyle(Title.init($"{groupingType}"))
        .WithYAxisStyle(Title.init($"{groupingType}"));

      var usersHeatmap = CSharpChart
        .Heatmap<double, string, string, double>(
          zData: sortedUsersMatrix,
          X: sortedGroups,
          Y: sortedGroups,
          ColorScale: redColorscale
        )
        .WithTitle($"Users Correlation - {groupingType}")
        .WithXAxisStyle(Title.init($"{groupingType}"))
        .WithYAxisStyle(Title.init($"{groupingType}"));

      // Combine into subplot
      var combinedChart = CSharpChart.Grid(
        [dollarsHeatmap, transactionsHeatmap, usersHeatmap],
        nRows: 1,
        nCols: 3
      );

      return await Task.FromResult(combinedChart);
    };
  }

  private static List<List<double>> BuildCorrelationMatrix(
    List<CorrelationHeatmapSchema> data,
    List<string> groups,
    Func<CorrelationHeatmapSchema, double> correlationSelector
  )
  {
    var n = groups.Count;
    var matrix = new List<List<double>>();

    // Initialize matrix with 1.0 on diagonal
    for (int i = 0; i < n; i++)
    {
      var row = new List<double>();
      for (int j = 0; j < n; j++)
      {
        row.Add(i == j ? 1.0 : 0.0);
      }
      matrix.Add(row);
    }

    // Fill in correlation values
    foreach (var correlation in data)
    {
      var i = groups.IndexOf(correlation.Group1);
      var j = groups.IndexOf(correlation.Group2);

      if (i >= 0 && j >= 0)
      {
        var value = correlationSelector(correlation);
        matrix[i][j] = value;
        matrix[j][i] = value; // Symmetric matrix
      }
    }

    return matrix;
  }

  /// <summary>
  /// Performs simple hierarchical clustering by average linkage to reorder groups.
  /// Groups with higher correlations are placed closer together for better visualization.
  /// </summary>
  private static List<string> HierarchicalClusterSort(
    List<string> groups,
    List<List<double>> correlationMatrix
  )
  {
    var n = groups.Count;
    if (n <= 2)
    {
      return groups; // No point clustering 1-2 items
    }

    // Calculate average correlation for each group (as a simple heuristic)
    var avgCorrelations = new List<(string group, double avgCorr)>();
    for (int i = 0; i < n; i++)
    {
      var sum = 0.0;
      for (int j = 0; j < n; j++)
      {
        if (i != j)
        {
          sum += correlationMatrix[i][j];
        }
      }
      avgCorrelations.Add((groups[i], sum / (n - 1)));
    }

    // Sort by average correlation descending (highly correlated groups first)
    return avgCorrelations.OrderByDescending(x => x.avgCorr).Select(x => x.group).ToList();
  }

  /// <summary>
  /// Reorders a correlation matrix based on new group ordering.
  /// </summary>
  private static List<List<double>> ReorderMatrix(
    List<List<double>> originalMatrix,
    List<string> originalGroups,
    List<string> newGroups
  )
  {
    var n = newGroups.Count;
    var newMatrix = new List<List<double>>();

    // Create mapping from group name to original index
    var groupToOriginalIndex = originalGroups
      .Select((g, i) => (g, i))
      .ToDictionary(x => x.g, x => x.i);

    // Build new matrix by looking up values from original matrix
    for (int i = 0; i < n; i++)
    {
      var row = new List<double>();
      var origI = groupToOriginalIndex[newGroups[i]];

      for (int j = 0; j < n; j++)
      {
        var origJ = groupToOriginalIndex[newGroups[j]];
        row.Add(originalMatrix[origI][origJ]);
      }

      newMatrix.Add(row);
    }

    return newMatrix;
  }
}
