using KedroSpaceflights.Custom.Data._05_ModelOutput.Schemas;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace KedroSpaceflights.Custom.Pipelines.Reporting.Nodes;

/// <summary>
/// Generates a scatter plot comparing actual vs predicted values from model predictions.
/// Uses color-coded dots to indicate over-estimation (yellow) and under-estimation (red).
/// </summary>
/// <remarks>
/// <para>
/// This node visualizes model prediction accuracy by plotting actual values on the X-axis
/// and predicted values on the Y-axis. A 1:1 identity reference line (dotted) shows perfect
/// prediction alignment. Points above the line represent over-estimates (yellow), while
/// points below represent under-estimates (red). The chart title includes the R² score
/// from model evaluation metrics.
/// </para>
/// <para>
/// <strong>Input:</strong> Tuple of (ModelMetrics, ModelPredictions) with evaluation metrics and predictions
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
public static class GeneratePredictionScatterNode
{
  public static Func<
    (ModelMetrics Metrics, IEnumerable<ModelPredictions> Predictions),
    Task<GenericChart>
  > Create(ILogger? logger = null)
  {
    return async (input) =>
    {
      var metrics = input.Metrics;
      var predictions = input.Predictions.ToList();

      logger?.LogInformation(
        "Generating prediction scatter plot for {Count} data points",
        predictions.Count
      );

      // Separate over-estimates and under-estimates
      var overEstimates = predictions.Where(p => p.Predicted > p.Actual).ToList();
      var underEstimates = predictions.Where(p => p.Predicted <= p.Actual).ToList();

      // Calculate range for 1:1 reference line (in log space)
      var allValues = predictions.SelectMany(p => new[] { p.Actual, p.Predicted }).ToList();
      var minValue = Math.Log(allValues.Min());
      var maxValue = Math.Log(allValues.Max());

      // Create scatter traces with log-transformed data
      var overEstimateTrace = CSharpChart
        .Point<double, double, string>(
          x: overEstimates.Select(p => Math.Log(p.Actual)),
          y: overEstimates.Select(p => Math.Log(p.Predicted))
        )
        .WithTraceInfo(Name: "Over-estimate", ShowLegend: true)
        .WithMarkerStyle(Color: Color.fromKeyword(ColorKeyword.Orange), Size: 8, Opacity: 0.25);

      var underEstimateTrace = CSharpChart
        .Point<double, double, string>(
          x: underEstimates.Select(p => Math.Log(p.Actual)),
          y: underEstimates.Select(p => Math.Log(p.Predicted))
        )
        .WithTraceInfo(Name: "Under-estimate", ShowLegend: true)
        .WithMarkerStyle(Color: Color.fromKeyword(ColorKeyword.Red), Size: 8, Opacity: 0.25);

      // Create 1:1 identity reference line (dotted) in log space
      var referenceLine = CSharpChart
        .Line<double, double, string>(
          x: new[] { minValue, maxValue },
          y: new[] { minValue, maxValue }
        )
        .WithTraceInfo(Name: "Perfect Prediction (1:1)", ShowLegend: true)
        .WithLineStyle(
          Color: Color.fromKeyword(ColorKeyword.Gray),
          Dash: StyleParam.DrawingStyle.Dot,
          Width: 2.0
        );

      // Combine all traces
      var chart = Plotly
        .NET.Chart.Combine(new[] { underEstimateTrace, overEstimateTrace, referenceLine })
        .WithTitle($"OLS Model Results (R² = {metrics.R2Score:F2})")
        .WithSize(600, 600);

      // Configure axes with log scale, title, and no grid
      chart = chart.WithXAxis(
        LinearAxis.init<
          IConvertible,
          IConvertible,
          IConvertible,
          IConvertible,
          IConvertible,
          IConvertible,
          IConvertible,
          IConvertible
        >(Title: Title.init("Actual Price (ln($))"), ShowGrid: false)
      );

      chart = chart.WithYAxis(
        LinearAxis.init<
          IConvertible,
          IConvertible,
          IConvertible,
          IConvertible,
          IConvertible,
          IConvertible,
          IConvertible,
          IConvertible
        >(Title: Title.init("Predicted Price (ln($))"), ShowGrid: false)
      );

      logger?.LogInformation(
        "Generated scatter plot with {OverCount} over-estimates and {UnderCount} under-estimates",
        overEstimates.Count,
        underEstimates.Count
      );

      return chart;
    };
  }
}
