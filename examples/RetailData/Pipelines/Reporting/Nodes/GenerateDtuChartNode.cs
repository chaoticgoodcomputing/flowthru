using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using RetailData.Data._05_Reporting.Schemas;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace RetailData.Pipelines.Reporting.Nodes;

/// <summary>
/// Generates a multi-line time series chart showing DTU metrics over time.
/// Creates a subplot with three charts: Dollars, Transactions, and Users.
/// </summary>
public static class GenerateDtuChartNode
{
  // Plotly's default color palette for consistent coloring across subplots
  private static readonly string[] ColorPalette =
  [
    "#1f77b4",
    "#ff7f0e",
    "#2ca02c",
    "#d62728",
    "#9467bd",
    "#8c564b",
    "#e377c2",
    "#7f7f7f",
    "#bcbd22",
    "#17becf",
    "#aec7e8",
    "#ffbb78",
    "#98df8a",
    "#ff9896",
    "#c5b0d5",
    "#c49c94",
    "#f7b6d2",
    "#c7c7c7",
    "#dbdb8d",
    "#9edae5",
    "#393b79",
    "#637939",
    "#8c6d31",
    "#843c39",
    "#7b4173",
    "#5254a3",
    "#8ca252",
    "#bd9e39",
    "#ad494a",
    "#a55194",
    "#6b6ecf",
    "#b5cf6b",
    "#e7ba52",
    "#d6616b",
    "#ce6dbd",
    "#9c9ede",
    "#cedb9c",
    "#e7cb94",
    "#e7969c",
    "#de9ed6",
  ];

  public static Func<IEnumerable<DtuTimeSeriesSchema>, Task<GenericChart>> Create()
  {
    return async (input) =>
    {
      var data = input.ToList();
      var groupingType = data.FirstOrDefault()?.GroupingType ?? "Unknown";

      // Group by GroupingValue (Country or Region) and sort for consistent ordering
      var groupedData = data.GroupBy(d => d.GroupingValue).OrderBy(g => g.Key).ToList();

      // Create a color mapping for consistent colors across all subplots
      var colorMap = groupedData
        .Select(
          (group, index) =>
            new { GroupName = group.Key, Color = ColorPalette[index % ColorPalette.Length] }
        )
        .ToDictionary(x => x.GroupName, x => x.Color);

      // Create three separate charts for Dollars, Transactions, and Users
      var dollarsCharts = new List<GenericChart>();
      var transactionsCharts = new List<GenericChart>();
      var usersCharts = new List<GenericChart>();

      foreach (var group in groupedData)
      {
        var groupName = group.Key;
        var color = colorMap[groupName];
        var orderedData = group.OrderBy(d => d.Date).ToList();
        var dates = orderedData.Select(d => d.Date).ToList();

        // Dollars chart
        dollarsCharts.Add(
          CSharpChart
            .Line<string, decimal, string>(
              x: dates,
              y: orderedData.Select(d => d.Dollars).ToList(),
              Name: groupName
            )
            .WithLineStyle(Color: Plotly.NET.Color.fromHex(color))
        );

        // Transactions chart
        transactionsCharts.Add(
          CSharpChart
            .Line<string, int, string>(
              x: dates,
              y: orderedData.Select(d => d.Transactions).ToList(),
              Name: groupName
            )
            .WithLineStyle(Color: Plotly.NET.Color.fromHex(color))
        );

        // Users chart
        usersCharts.Add(
          CSharpChart
            .Line<string, int, string>(
              x: dates,
              y: orderedData.Select(d => d.Users).ToList(),
              Name: groupName
            )
            .WithLineStyle(Color: Plotly.NET.Color.fromHex(color))
        );
      }

      // Combine charts within each metric
      var dollarsChart = CSharpChart
        .Combine(dollarsCharts)
        .WithTitle($"Dollars by {groupingType}")
        .WithXAxisStyle(Title.init("Date"))
        .WithYAxisStyle(Title.init("Dollars"));

      var transactionsChart = CSharpChart
        .Combine(transactionsCharts)
        .WithTitle($"Transactions by {groupingType}")
        .WithXAxisStyle(Title.init("Date"))
        .WithYAxisStyle(Title.init("Transactions"));

      var usersChart = CSharpChart
        .Combine(usersCharts)
        .WithTitle($"Users by {groupingType}")
        .WithXAxisStyle(Title.init("Date"))
        .WithYAxisStyle(Title.init("Users"));

      // Create subplot with 3 rows
      var combinedChart = CSharpChart.Grid(
        [dollarsChart, transactionsChart, usersChart],
        nRows: 3,
        nCols: 1
      );

      return await Task.FromResult(combinedChart);
    };
  }
}
