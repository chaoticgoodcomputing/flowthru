using Flowthru.Nodes;
using Microsoft.Extensions.Logging;
using Plotly.NET;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.Reporting.Nodes;

/// <summary>
/// Exports a Plotly GenericChart to base64-encoded PNG string.
/// Reusable node for converting in-memory chart objects to storable base64 strings.
/// </summary>
/// <remarks>
/// <para>
/// This node handles the PNG export concern separately from chart generation,
/// enabling a clean separation of visualization logic from output format concerns.
/// By using base64 encoding, the PNG data can be stored in a FileCatalogObject&lt;string&gt;
/// without requiring binary file support in Flowthru's catalog system.
/// </para>
/// <para>
/// <strong>Input:</strong> GenericChart object from memory catalog
/// </para>
/// <para>
/// <strong>Output:</strong> Base64-encoded PNG string (can be stored in FileCatalogObject&lt;string&gt;)
/// </para>
/// <para>
/// <strong>Reusability:</strong> This node can be used for any Plotly chart type
/// (bar, scatter, heatmap, etc.) since it operates on the base GenericChart type.
/// </para>
/// <para>
/// <strong>Implementation Note:</strong> Uses Plotly.NET.ImageExport's ToBase64PNGStringAsync
/// with PuppeteerSharp backend, which downloads Chromium on first execution for browser-based rendering.
/// The base64 string can be decoded to raw PNG bytes if needed using Convert.FromBase64String().
/// </para>
/// </remarks>
public class PlotlyImageExportNode : NodeBase<GenericChart, string> {
  protected override async Task<IEnumerable<string>> Transform(IEnumerable<GenericChart> input) {
    var results = new List<string>();

    foreach (var chart in input) {
      Logger?.LogInformation("Converting chart to base64-encoded PNG");

      // Use Plotly.NET.ImageExport to convert the chart to a base64 PNG string
      // This uses a headless browser (Chromium via PuppeteerSharp) to render the chart
      var base64Png = await Plotly.NET.ImageExport.GenericChartExtensions.ToBase64PNGStringAsync(
        chart,
        Width: 600,
        Height: 600);

      Logger?.LogInformation(
          "Successfully converted chart to base64 PNG (length: {Length} characters)",
          base64Png.Length);

      results.Add(base64Png);
    }

    return results;
  }
}
