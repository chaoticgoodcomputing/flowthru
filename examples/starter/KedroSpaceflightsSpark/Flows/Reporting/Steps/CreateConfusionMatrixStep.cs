using Flowthru.Core.Steps;
using KedroSpaceflightsSpark.Data._07_ModelOutput.Schemas;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace KedroSpaceflightsSpark.Flows.Reporting.Steps;

[FlowthruStep]
public static class CreateConfusionMatrixStep
{
  public record Options
  {
    public int NumBins { get; init; } = 4;
  }

  public static Func<IEnumerable<ModelPredictions>, GenericChart> Create(
    Options? options = null,
    ILogger? logger = null
  )
  {
    var opts = options ?? new Options();

    return input =>
    {
      var predictions = input.ToList();

      if (!predictions.Any())
      {
        throw new InvalidOperationException(
          "Cannot create confusion matrix from empty predictions"
        );
      }

      logger?.LogInformation(
        "Generating confusion matrix from {Count} predictions using {NumBins} bins",
        predictions.Count,
        opts.NumBins
      );

      var sortedActuals = predictions.Select(p => p.Actual).OrderBy(v => v).ToList();
      var thresholds = CalculatePercentileThresholds(sortedActuals, opts.NumBins);

      var binnedPredictions = predictions
        .Select(p =>
          (Actual: AssignBin(p.Actual, thresholds), Predicted: AssignBin(p.Predicted, thresholds))
        )
        .ToList();

      var matrix = new int[opts.NumBins, opts.NumBins];
      foreach (var (actual, predicted) in binnedPredictions)
      {
        matrix[actual, predicted]++;
      }

      var zValues = new List<List<int>>();
      for (int i = 0; i < opts.NumBins; i++)
      {
        var row = new List<int>();
        for (int j = 0; j < opts.NumBins; j++)
        {
          row.Add(matrix[i, j]);
        }

        zValues.Add(row);
      }

      var binLabels = Enumerable.Range(1, opts.NumBins).Select(i => $"Q{i}").ToList();

      return CSharpChart
        .Heatmap<int, string, string, int>(zValues, X: binLabels, Y: binLabels, ShowScale: true)
        .WithXAxisStyle(Title.init("Predicted"))
        .WithYAxisStyle(Title.init("Actual"))
        .WithTitle($"Prediction Confusion Matrix ({opts.NumBins} bins)")
        .WithSize(Math.Max(600, opts.NumBins * 80), Math.Max(600, opts.NumBins * 80));
    };
  }

  private static List<double> CalculatePercentileThresholds(List<double> sortedValues, int numBins)
  {
    var thresholds = new List<double>();
    for (int i = 1; i < numBins; i++)
    {
      var index = (int)Math.Round((double)i / numBins * sortedValues.Count) - 1;
      index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
      thresholds.Add(sortedValues[index]);
    }
    return thresholds;
  }

  private static int AssignBin(double value, List<double> thresholds)
  {
    for (int i = 0; i < thresholds.Count; i++)
    {
      if (value <= thresholds[i])
      {
        return i;
      }
    }
    return thresholds.Count;
  }
}
