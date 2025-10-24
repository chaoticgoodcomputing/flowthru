using System.Text;
using Flowthru.Nodes;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.Reporting.Nodes;

/// <summary>
/// Generates a human-readable Markdown report from cross-validation results.
/// </summary>
/// <remarks>
/// <para>
/// This node demonstrates using <see cref="FileCatalogObject"/> to generate
/// unstructured text output (Markdown reports) from structured data.
/// </para>
/// <para>
/// <strong>Report Contents:</strong>
/// - Summary statistics (mean, std dev, min, max R²)
/// - Comparison to Kedro reference implementation
/// - Per-fold detailed metrics
/// - Visual representation using ASCII charts
/// </para>
/// </remarks>
public class GenerateCrossValidationReportNode : NodeBase<CrossValidationResults, string> {
  protected override Task<IEnumerable<string>> Transform(IEnumerable<CrossValidationResults> input) {
    var results = input.Single();

    var report = new StringBuilder();

    // Header
    report.AppendLine("# Cross-Validation Report");
    report.AppendLine();
    report.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    report.AppendLine();

    // Summary Statistics
    report.AppendLine("## Summary Statistics");
    report.AppendLine();
    report.AppendLine($"- **Number of Folds:** {results.NumFolds}");
    report.AppendLine($"- **Mean R² Score:** {results.MeanR2Score:F4} ± {results.StdDevR2Score:F4}");
    report.AppendLine($"- **Min R² Score:** {results.MinR2Score:F4}");
    report.AppendLine($"- **Max R² Score:** {results.MaxR2Score:F4}");
    report.AppendLine();

    // Kedro Comparison
    report.AppendLine("## Comparison with Kedro");
    report.AppendLine();
    report.AppendLine($"- **Kedro Reference R²:** {results.KedroR2Score:F4}");
    report.AppendLine($"- **Flowthru Mean R²:** {results.MeanR2Score:F4}");
    report.AppendLine($"- **Absolute Difference:** {results.DifferenceFromKedro:F4}");

    var percentDiff = results.KedroR2Score != 0
      ? (results.DifferenceFromKedro / Math.Abs(results.KedroR2Score)) * 100
      : 0;
    report.AppendLine($"- **Relative Difference:** {percentDiff:F2}%");
    report.AppendLine();

    // Interpretation
    if (results.DifferenceFromKedro < 0.01) {
      report.AppendLine("✅ **Interpretation:** Excellent alignment with Kedro reference implementation.");
    } else if (results.DifferenceFromKedro < 0.05) {
      report.AppendLine("⚠️ **Interpretation:** Minor deviation from Kedro reference. This is likely due to implementation differences or random variation.");
    } else {
      report.AppendLine("❌ **Interpretation:** Significant deviation from Kedro reference. Review data processing and model training steps.");
    }
    report.AppendLine();

    // R² Distribution Visualization
    report.AppendLine("## R² Score Distribution");
    report.AppendLine();
    report.AppendLine("```");

    // Simple ASCII bar chart
    var minR2 = results.FoldMetrics.Min(f => f.R2Score);
    var maxR2 = results.FoldMetrics.Max(f => f.R2Score);
    var range = maxR2 - minR2;

    foreach (var fold in results.FoldMetrics.OrderBy(f => f.FoldNumber)) {
      var normalizedScore = range > 0 ? (fold.R2Score - minR2) / range : 0.5;
      var barLength = (int)(normalizedScore * 40);
      var bar = new string('█', barLength);
      report.AppendLine($"Fold {fold.FoldNumber,2}: {bar} {fold.R2Score:F4}");
    }

    // Add markers for mean and Kedro score
    var meanNormalized = range > 0 ? (results.MeanR2Score - minR2) / range : 0.5;
    var meanMarker = new string(' ', (int)(meanNormalized * 40)) + "↑ Mean";
    report.AppendLine($"        {meanMarker}");

    if (results.KedroR2Score >= minR2 && results.KedroR2Score <= maxR2) {
      var kedroNormalized = range > 0 ? (results.KedroR2Score - minR2) / range : 0.5;
      var kedroMarker = new string(' ', (int)(kedroNormalized * 40)) + "↑ Kedro";
      report.AppendLine($"        {kedroMarker}");
    }

    report.AppendLine("```");
    report.AppendLine();

    // Detailed Fold Metrics
    report.AppendLine("## Detailed Fold Metrics");
    report.AppendLine();
    report.AppendLine("| Fold | R² Score | MAE | RMSE | Loss Function |");
    report.AppendLine("|------|----------|-----|------|---------------|");

    foreach (var fold in results.FoldMetrics.OrderBy(f => f.FoldNumber)) {
      report.AppendLine($"| {fold.FoldNumber} | {fold.R2Score:F4} | {fold.MeanAbsoluteError:F2} | {fold.RootMeanSquaredError:F2} | {fold.LossFunctionValue:F4} |");
    }

    report.AppendLine();

    // Variability Analysis
    report.AppendLine("## Variability Analysis");
    report.AppendLine();
    var coefficientOfVariation = results.MeanR2Score != 0
      ? (results.StdDevR2Score / Math.Abs(results.MeanR2Score)) * 100
      : 0;
    report.AppendLine($"- **Coefficient of Variation:** {coefficientOfVariation:F2}%");

    if (coefficientOfVariation < 5) {
      report.AppendLine("- **Model Stability:** Excellent - Low variance across folds indicates robust model");
    } else if (coefficientOfVariation < 10) {
      report.AppendLine("- **Model Stability:** Good - Acceptable variance across folds");
    } else if (coefficientOfVariation < 20) {
      report.AppendLine("- **Model Stability:** Moderate - Consider feature engineering or hyperparameter tuning");
    } else {
      report.AppendLine("- **Model Stability:** Poor - High variance suggests overfitting or data quality issues");
    }
    report.AppendLine();

    // Footer
    report.AppendLine("---");
    report.AppendLine();
    report.AppendLine("*This report was automatically generated by the Flowthru DataValidation pipeline.*");

    return Task.FromResult(new[] { report.ToString() }.AsEnumerable());
  }
}
