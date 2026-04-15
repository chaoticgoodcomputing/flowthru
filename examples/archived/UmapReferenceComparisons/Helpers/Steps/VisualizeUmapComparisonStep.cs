using Plotly.NET;
using Plotly.NET.LayoutObjects;
using UmapReferenceComparisons.Data._01_Raw.Schemas;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace UmapReferenceComparisons.Helpers.Steps;

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
public static class VisualizeUmapComparisonStep
{
    public record Params
    {
        /// <summary>
        /// Name of the dataset for the plot title.
        /// </summary>
        public required string DatasetName { get; init; }

        /// <summary>
        /// Optional function to map label strings to display names.
        /// If not provided, labels are used as-is.
        /// </summary>
        public Func<string, string>? LabelFormatter { get; init; }
    }

    /// <summary>
    /// Creates a visualization function for comparing UMAP outputs.
    /// </summary>
    public static Func<
      (
        IEnumerable<UmapInput> inputData,
        IEnumerable<UmapOutputRow> pythonOutput,
        IEnumerable<UmapOutputRow> csharpOutput
      ),
      Task<GenericChart>
    > Create(Params options)
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

            Console.WriteLine($"Generating visual comparison for {options.DatasetName}...");

            // Extract labels
            var labels = inputList.Select(i => i.Label).ToArray();

            // Create Python subplot
            var pythonChart = CreateScatterPlot(
          pythonList,
          labels,
          "", // Empty title since we'll use subplot titles instead
          options.LabelFormatter
        );

            // Create C# subplot
            var csharpChart = CreateScatterPlot(
          csharpList,
          labels,
          "", // Empty title since we'll use subplot titles instead
          options.LabelFormatter
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
          .WithTitle($"UMAP Comparison: {options.DatasetName}")
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
      string[] labels,
      string title,
      Func<string, string>? labelFormatter
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

            var displayName = labelFormatter?.Invoke(label) ?? label;

            var trace = CSharpChart
              .Point<float, float, string>(x: xValues, y: yValues)
              .WithTraceInfo(Name: displayName, ShowLegend: true)
              .WithMarkerStyle(Color: Color.fromKeyword(colorMap[label]), Size: 6, Opacity: 0.2);

            traces.Add(trace);
        }

        return CSharpChart
          .Combine(traces)
          .WithXAxisStyle(Title.init("UMAP Component 1"))
          .WithYAxisStyle(Title.init("UMAP Component 2"))
          .WithTitle(title);
    }

    /// <summary>
    /// Creates a color map for labels using a standard color palette.
    /// </summary>
    private static Dictionary<string, ColorKeyword> CreateColorMap(string[] uniqueLabels)
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
      ColorKeyword.Brown,
      ColorKeyword.Pink,
    };

        var colorMap = new Dictionary<string, ColorKeyword>();
        for (int i = 0; i < uniqueLabels.Length; i++)
        {
            colorMap[uniqueLabels[i]] = colors[i % colors.Length];
        }

        return colorMap;
    }
}
