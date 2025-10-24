using Flowthru.Nodes;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.Reporting.Nodes;

/// <summary>
/// Generates a comprehensive cross-validation visualization showing R² distribution analysis.
/// Creates a layered chart with scatter plot, normal curve, and reference lines.
/// </summary>
/// <remarks>
/// <para>
/// This node creates a multi-layer visualization to analyze cross-validation results:
/// 1. Scatter plot of R² values from each fold
/// 2. Normal distribution curve fitted to the R² values
/// 3. Vertical line showing mean cross-validated R²
/// 4. Vertical line showing Kedro reference R²
/// </para>
/// <para>
/// All elements are overlaid on the same XY axis for comprehensive comparison.
/// </para>
/// <para>
/// <strong>Input:</strong> CrossValidationResults from DataScience pipeline
/// </para>
/// <para>
/// <strong>Output:</strong> GenericChart object stored in memory for downstream processing
/// </para>
/// <para>
/// <strong>Architecture:</strong> This node focuses purely on chart generation logic.
/// Serialization to JSON or image export is handled by downstream nodes, enabling
/// separation of concerns and reusable export pipelines.
/// </para>
/// </remarks>
public class VisualizeCrossValidationNode : NodeBase<CrossValidationResults, GenericChart> {
  protected override Task<IEnumerable<GenericChart>> Transform(IEnumerable<CrossValidationResults> input) {
    var results = input.First();

    Logger?.LogInformation(
        "Generating cross-validation visualization for {NumFolds} folds",
        results.NumFolds);

    // Extract R² scores from fold metrics
    var r2Scores = results.FoldMetrics.Select(f => f.R2Score).ToList();

    // Calculate normal distribution curve points (10x the number of folds)
    var curvePoints = GenerateNormalCurvePoints(
        mean: results.MeanR2Score,
        stdDev: results.StdDevR2Score,
        numPoints: results.NumFolds * 10,
        stdDevRange: 5);

    Logger?.LogInformation(
        "Generated {CurvePoints} points for normal distribution curve",
        curvePoints.Count);

    // Calculate Y values for scatter points (their corresponding probability density)
    var scatterX = r2Scores.ToList();
    var scatterY = r2Scores.Select(r2 => NormalProbabilityDensity(r2, results.MeanR2Score, results.StdDevR2Score)).ToList();

    // Find max probability density for vertical line height
    var maxProbDensity = curvePoints.Max(p => p.Y);

    // Create normal distribution curve
    var curveTrace = CSharpChart.Line<double, double, double>(
        curvePoints.Select(p => p.X).ToList(),
        curvePoints.Select(p => p.Y).ToList()
    )
    .WithTraceInfo(Name: "Normal Distribution", ShowLegend: true)
    .WithLineStyle(Color: Color.fromKeyword(ColorKeyword.Red), Width: 2.0);

    // Create scatter plot of fold R² scores positioned on the curve
    var scatterTrace = CSharpChart.Point<double, double, double>(
        scatterX,
        scatterY
    )
    .WithTraceInfo(Name: "Fold R² Scores", ShowLegend: true)
    .WithMarkerStyle(Size: 10, Color: Color.fromKeyword(ColorKeyword.Blue));

    // Create vertical line for mean R²
    var meanLineTrace = CreateVerticalLine(
        xValue: results.MeanR2Score,
        yMin: 0,
        yMax: maxProbDensity * 1.1,  // 10% higher than max for visibility
        name: $"Mean R² ({results.MeanR2Score:F4})",
        color: ColorKeyword.Green);

    // Create vertical line for Kedro reference R²
    var kedroLineTrace = CreateVerticalLine(
        xValue: results.KedroR2Score,
        yMin: 0,
        yMax: maxProbDensity * 1.1,
        name: $"Kedro R² ({results.KedroR2Score:F4})",
        color: ColorKeyword.Orange);

    // Combine all traces into a single chart
    var chart = Plotly.NET.Chart.Combine(new[] {
      curveTrace,
      scatterTrace,
      meanLineTrace,
      kedroLineTrace
    })
    .WithXAxisStyle(Title.init("R² Value"))
    .WithYAxisStyle(Title.init("Probability Density"))
    .WithTitle($"Cross-Validation Results (n={results.NumFolds} folds)")
    .WithLegend(true);

    Logger?.LogInformation(
        "Generated cross-validation visualization with mean R²={MeanR2:F4}, Kedro R²={KedroR2:F4}",
        results.MeanR2Score,
        results.KedroR2Score);

    return Task.FromResult<IEnumerable<GenericChart>>(new[] { chart });
  }

  /// <summary>
  /// Generates points for a normal distribution curve
  /// </summary>
  private List<(double X, double Y)> GenerateNormalCurvePoints(
      double mean,
      double stdDev,
      int numPoints,
      int stdDevRange) {
    var points = new List<(double X, double Y)>();

    // Generate points spanning ±5 standard deviations
    var rangeMin = mean - stdDevRange * stdDev;
    var rangeMax = mean + stdDevRange * stdDev;
    var step = (rangeMax - rangeMin) / (numPoints - 1);

    for (int i = 0; i < numPoints; i++) {
      var x = rangeMin + i * step;
      var y = NormalProbabilityDensity(x, mean, stdDev);
      points.Add((x, y));
    }

    return points;
  }

  /// <summary>
  /// Calculates the probability density function for a normal distribution
  /// </summary>
  private double NormalProbabilityDensity(double x, double mean, double stdDev) {
    var coefficient = 1.0 / (stdDev * Math.Sqrt(2 * Math.PI));
    var exponent = -Math.Pow(x - mean, 2) / (2 * Math.Pow(stdDev, 2));
    return coefficient * Math.Exp(exponent);
  }

  /// <summary>
  /// Creates a vertical line trace for reference values
  /// </summary>
  private GenericChart CreateVerticalLine(
      double xValue,
      double yMin,
      double yMax,
      string name,
      ColorKeyword color) {
    return CSharpChart.Line<double, double, double>(
        new[] { xValue, xValue },
        new[] { yMin, yMax }
    )
    .WithTraceInfo(Name: name, ShowLegend: true)
    .WithLineStyle(Color: Color.fromKeyword(color), Width: 2.0, Dash: StyleParam.DrawingStyle.Dash);
  }
}
