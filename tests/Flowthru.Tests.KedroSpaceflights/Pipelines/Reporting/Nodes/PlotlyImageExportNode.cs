using Flowthru.Nodes;
using Microsoft.Extensions.Logging;
using Plotly.NET;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.Reporting.Nodes;

/// <summary>
/// Exports a Plotly GenericChart to PNG binary data.
/// Reusable node for converting in-memory chart objects to storable PNG images.
/// </summary>
/// <remarks>
/// <para>
/// This node handles the PNG export concern separately from chart generation,
/// enabling a clean separation of visualization logic from output format concerns.
/// The PNG binary data can be stored in a BinaryFileCatalogObject&lt;byte[]&gt;.
/// </para>
/// <para>
/// <strong>Input:</strong> GenericChart object from memory catalog
/// </para>
/// <para>
/// <strong>Output:</strong> PNG binary data as byte[] (can be stored in BinaryFileCatalogObject&lt;byte[]&gt;)
/// </para>
/// <para>
/// <strong>Reusability:</strong> This node can be used for any Plotly chart type
/// (bar, scatter, heatmap, etc.) since it operates on the base GenericChart type.
/// </para>
/// <para>
/// <strong>Implementation Note:</strong> Uses Plotly.NET.ImageExport's ToBase64PNGStringAsync
/// with PuppeteerSharp backend (downloads Chromium on first execution for browser-based rendering),
/// then strips the data URI prefix and decodes the base64 string to raw PNG bytes.
/// </para>
/// </remarks>
public class PlotlyImageExportNode : NodeBase<GenericChart, byte[]> {
  protected override async Task<IEnumerable<byte[]>> Transform(IEnumerable<GenericChart> input) {
    var results = new List<byte[]>();

    foreach (var chart in input) {
      Logger?.LogInformation("Converting chart to PNG binary data");

      // Use Plotly.NET.ImageExport to convert the chart to a base64 PNG string
      // This uses a headless browser (Chromium via PuppeteerSharp) to render the chart
      var base64DataUri = await Plotly.NET.ImageExport.GenericChartExtensions.ToBase64PNGStringAsync(
        chart,
        Width: 600,
        Height: 600);

      // Strip the data URI prefix "data:image/png;base64," to get pure base64
      const string dataUriPrefix = "data:image/png;base64,";
      var base64String = base64DataUri.StartsWith(dataUriPrefix)
          ? base64DataUri.Substring(dataUriPrefix.Length)
          : base64DataUri;

      // Decode base64 to raw PNG bytes
      var pngBytes = Convert.FromBase64String(base64String);

      Logger?.LogInformation(
          "Successfully converted chart to PNG ({Size:N0} bytes)",
          pngBytes.Length);

      results.Add(pngBytes);
    }

    return results;
  }
}
