using Plotly.NET;
using Plotly.NET.LayoutObjects;
using UmapReferenceComparisons.Data._01_Raw.Schemas;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace UmapReferenceComparisons.Pipelines.IrisComparison.Nodes;

/// <summary>
/// Creates a side-by-side visual comparison of Python and C# UMAP outputs.
/// </summary>
/// <remarks>
/// Generates a Plotly figure with two subplots:
/// - Left: Python reference UMAP embedding
/// - Right: C# UMAP embedding
///
/// Points are colored by class labels to enable visual validation that both
/// implementations preserve similar clustering patterns.
/// </remarks>
public static class VisualizeComparisonNode
{
  /// <summary>
  /// Creates a visualization function for comparing UMAP outputs.
  /// </summary>
  /// <param name="datasetName">Name of the dataset for the plot title.</param>
  /// <returns>
  /// A function that generates a side-by-side comparison chart.
  /// </returns>
  public static Func<
    (
      IEnumerable<IrisInputRow> inputData,
      IEnumerable<UmapOutputRow> pythonOutput,
      IEnumerable<UmapOutputRow> csharpOutput
    ),
    Task<GenericChart>
  > Create(string datasetName)
  {
    return async (input) =>
    {
      var (inputData, pythonOutput, csharpOutput) = input;

      var inputList = inputData.ToList();
      var pythonList = pythonOutput.ToList();
      var csharpList = csharpOutput.ToList();

      if (inputList.Count != pythonList.Count || inputList.Count != csharpList.Count)
      {
        throw new InvalidOperationException(
          $"Sample count mismatch: input={inputList.Count}, python={pythonList.Count}, csharp={csharpList.Count}"
        );
      }

      Console.WriteLine($"Generating visual comparison for {datasetName}...");

      // Create Python subplot
      var pythonChart = CreateScatterPlot(
        pythonList,
        inputList.Select(i => i.Label).ToArray(),
        "" // Empty title since we'll use subplot titles instead
      );

      // Create C# subplot
      var csharpChart = CreateScatterPlot(
        csharpList,
        inputList.Select(i => i.Label).ToArray(),
        "" // Empty title since we'll use subplot titles instead
      );

      // Combine into side-by-side subplots (1 row, 2 columns) with subplot titles
      var combined = CSharpChart
        .Grid(
          new[] { pythonChart, csharpChart },
          1, // nRows
          2, // nCols
          SubPlotTitles: new[]
          {
            $"Python UMAP ({pythonList.Count} samples)",
            $"C# UMAP ({csharpList.Count} samples)",
          }
        )
        .WithTitle($"UMAP Comparison: {datasetName}")
        .WithSize(1600, 700);

      Console.WriteLine("Visual comparison generated successfully");

      return await Task.FromResult(combined);
    };
  }

  /// <summary>
  /// Creates a scatter plot for a single UMAP embedding.
  /// </summary>
  private static GenericChart CreateScatterPlot(
    List<UmapOutputRow> embeddings,
    int[] labels,
    string title
  )
  {
    // Get unique labels and create color mapping
    var uniqueLabels = labels.Distinct().OrderBy(l => l).ToArray();
    var colorMap = CreateColorMap(uniqueLabels);

    // Group embeddings by label
    var traces = new List<GenericChart>();

    foreach (var label in uniqueLabels)
    {
      var indices = Enumerable.Range(0, labels.Length).Where(i => labels[i] == label).ToArray();

      var xValues = indices.Select(i => embeddings[i].Component0).ToArray();
      var yValues = indices.Select(i => embeddings[i].Component1).ToArray();

      var trace = CSharpChart
        .Point<float, float, string>(x: xValues, y: yValues)
        .WithTraceInfo(Name: GetLabelName(label), ShowLegend: true)
        .WithMarkerStyle(Color: Color.fromKeyword(colorMap[label]), Size: 8, Opacity: 0.7);

      traces.Add(trace);
    }

    return CSharpChart
      .Combine(traces)
      .WithXAxisStyle(Title.init("UMAP Component 1"))
      .WithYAxisStyle(Title.init("UMAP Component 2"))
      .WithTitle(title);
  }

  /// <summary>
  /// Creates a color map for iris labels (0=setosa, 1=versicolor, 2=virginica).
  /// </summary>
  private static Dictionary<int, ColorKeyword> CreateColorMap(int[] uniqueLabels)
  {
    var colors = new[]
    {
      ColorKeyword.Blue,
      ColorKeyword.Green,
      ColorKeyword.Red,
      ColorKeyword.Purple,
      ColorKeyword.Orange,
      ColorKeyword.Cyan,
      ColorKeyword.Magenta,
      ColorKeyword.Yellow,
    };

    var colorMap = new Dictionary<int, ColorKeyword>();
    for (int i = 0; i < uniqueLabels.Length; i++)
    {
      colorMap[uniqueLabels[i]] = colors[i % colors.Length];
    }

    return colorMap;
  }

  /// <summary>
  /// Gets display name for iris labels.
  /// </summary>
  private static string GetLabelName(int label)
  {
    return label switch
    {
      0 => "Setosa",
      1 => "Versicolor",
      2 => "Virginica",
      _ => $"Class {label}",
    };
  }
}
