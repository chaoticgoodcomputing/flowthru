using Flowthru.Nodes;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.Reporting.Nodes;

/// <summary>
/// Creates a confusion matrix heatmap for model evaluation visualization.
/// Matches Kedro spaceflights reporting pipeline's create_confusion_matrix function.
/// </summary>
/// <remarks>
/// <para>
/// This node generates a dummy confusion matrix (matching the Kedro example) and visualizes
/// it as a Plotly heatmap. In a real scenario, this would use actual model predictions.
/// The output is a 2x2 confusion matrix showing true positives, false positives, true negatives,
/// and false negatives.
/// </para>
/// <para>
/// <strong>Input:</strong> Company data (used as dependency trigger, not for actual computation)
/// </para>
/// <para>
/// <strong>Output:</strong> GenericChart object stored in memory for downstream processing
/// </para>
/// <para>
/// <strong>Architecture:</strong> Chart generation is separated from export format concerns.
/// The same GenericChart can be exported to JSON, PNG, PDF, or other formats by downstream nodes.
/// </para>
/// </remarks>
public class CreateConfusionMatrixNode : NodeBase<CompanySchema, GenericChart> {
  protected override Task<IEnumerable<GenericChart>> Transform(IEnumerable<CompanySchema> input) {
    // Dummy confusion matrix data (matches Kedro spaceflights example)
    // In production, this would come from actual model predictions
    int[] actuals = [0, 1, 0, 0, 1, 1, 1, 0, 1, 0, 1];
    int[] predicted = [1, 1, 0, 1, 0, 1, 0, 0, 0, 1, 1];

    Logger?.LogInformation(
        "Generating confusion matrix from {Count} predictions",
        actuals.Length);

    // Build 2x2 confusion matrix
    var matrix = new int[2, 2];
    for (int i = 0; i < actuals.Length; i++) {
      matrix[actuals[i], predicted[i]]++;
    }

    // Convert to format for Plotly heatmap (list of lists)
    var zData = new List<List<int>>
    {
            new() { matrix[0, 0], matrix[0, 1] },  // Actual 0: [TN, FP]
            new() { matrix[1, 0], matrix[1, 1] }   // Actual 1: [FN, TP]
        };

    var xLabels = new[] { "Predicted 0", "Predicted 1" };
    var yLabels = new[] { "Actual 0", "Actual 1" };

    Logger?.LogInformation(
        "Confusion Matrix: TN={TN}, FP={FP}, FN={FN}, TP={TP}",
        matrix[0, 0], matrix[0, 1], matrix[1, 0], matrix[1, 1]);

    // Create heatmap using Plotly.NET.CSharp API
    var chart = CSharpChart.Heatmap<int, string, string, int>(
        zData,
        X: xLabels,
        Y: yLabels,
        ShowScale: true
    )
    .WithTitle("Confusion Matrix");

    Logger?.LogInformation(
        "Generated GenericChart heatmap for confusion matrix");

    return Task.FromResult<IEnumerable<GenericChart>>(new[] { chart });
  }
}
