using Flowthru.Step;
using Plotly.NET;
using SpaceflightsStagingSchema.Data._07_ModelOutput.Schemas;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace SpaceflightsStagingSchema.Flows.Reporting.Steps;

[FlowthruStep]
public static class CreateConfusionMatrixStep
{
  public record Options
  {
    public int NumBins { get; init; } = 4;
  }

  public static Func<IEnumerable<ModelPredictions>, GenericChart> Create(Options options) => data =>
  {
    var predictions = data.ToList();

    if (!predictions.Any())
    {
      throw new InvalidOperationException("Cannot create confusion matrix from empty predictions");
    }

    var sortedActuals = predictions.Select(p => p.Actual).OrderBy(v => v).ToList();
    var thresholds = CalculatePercentileThresholds(sortedActuals, options.NumBins);

    var binnedPredictions = predictions
      .Select(p =>
        (Actual: AssignBin(p.Actual, thresholds), Predicted: AssignBin(p.Predicted, thresholds))
      )
      .ToList();

    var matrix = new int[options.NumBins, options.NumBins];
    foreach (var (actual, predicted) in binnedPredictions)
    {
      matrix[actual, predicted]++;
    }

    var zData = new List<List<int>>();
    for (int i = 0; i < options.NumBins; i++)
    {
      var row = new List<int>();
      for (int j = 0; j < options.NumBins; j++)
      {
        row.Add(matrix[i, j]);
      }
      zData.Add(row);
    }

    var labels = GeneratePercentileLabels(options.NumBins);
    var xLabels = labels.Select(l => $"Pred {l}").ToArray();
    var yLabels = labels.Select(l => $"Actual {l}").ToArray();

    var binName = options.NumBins switch
    {
      2 => "Median Split",
      3 => "Tertiles",
      4 => "Quartiles",
      5 => "Quintiles",
      10 => "Deciles",
      _ => $"{options.NumBins} Bins",
    };

    return CSharpChart
      .Heatmap<int, string, string, int>(zData, X: xLabels, Y: yLabels, ShowScale: true)
      .WithTitle($"Confusion Matrix ({binName})")
      .WithSize(Math.Max(600, options.NumBins * 80), Math.Max(600, options.NumBins * 80));
  };

  private static List<double> CalculatePercentileThresholds(List<double> sortedValues, int numBins)
  {
    var thresholds = new List<double>();
    for (int i = 1; i < numBins; i++)
    {
      var percentile = (double)i / numBins;
      var index = (int)(percentile * sortedValues.Count);
      thresholds.Add(sortedValues[Math.Min(index, sortedValues.Count - 1)]);
    }
    return thresholds;
  }

  private static int AssignBin(double value, List<double> thresholds)
  {
    for (int i = 0; i < thresholds.Count; i++)
    {
      if (value < thresholds[i])
      {
        return i;
      }
    }
    return thresholds.Count;
  }

  private static List<string> GeneratePercentileLabels(int numBins)
  {
    var labels = new List<string>();
    for (int i = 0; i < numBins; i++)
    {
      var startPercentile = (i * 100) / numBins;
      var endPercentile = ((i + 1) * 100) / numBins;
      labels.Add($"P{startPercentile}-{endPercentile}");
    }
    return labels;
  }
}
