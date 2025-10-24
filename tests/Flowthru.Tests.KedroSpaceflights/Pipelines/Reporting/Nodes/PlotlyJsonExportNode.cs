using Flowthru.Nodes;
using Microsoft.Extensions.Logging;
using Plotly.NET;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.Reporting.Nodes;

/// <summary>
/// Exports a Plotly GenericChart to JSON format.
/// Reusable node for converting in-memory chart objects to plotly.js-compatible JSON.
/// </summary>
/// <remarks>
/// <para>
/// This node handles the serialization concern separately from chart generation,
/// enabling a clean separation of visualization logic from output format concerns.
/// The JSON output is compatible with plotly.js for browser-based rendering.
/// </para>
/// <para>
/// <strong>Input:</strong> GenericChart object from memory catalog
/// </para>
/// <para>
/// <strong>Output:</strong> JSON string representation (Plotly specification)
/// </para>
/// <para>
/// <strong>Reusability:</strong> This node can be used for any Plotly chart type
/// (bar, scatter, heatmap, etc.) since it operates on the base GenericChart type.
/// </para>
/// </remarks>
public class PlotlyJsonExportNode : NodeBase<GenericChart, string> {
  protected override Task<IEnumerable<string>> Transform(IEnumerable<GenericChart> input) {
    var results = new List<string>();

    foreach (var chart in input) {
      // Serialize to Plotly JSON using GenericChart.toJson()
      // This produces a JSON specification compatible with plotly.js
      var plotlyJson = GenericChart.toJson(chart);

      Logger?.LogInformation(
          "Exported chart to JSON ({Length} characters)",
          plotlyJson.Length);

      results.Add(plotlyJson);
    }

    return Task.FromResult<IEnumerable<string>>(results);
  }
}
