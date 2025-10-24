using Flowthru.Nodes;
using Microsoft.Extensions.Logging;
using Plotly.NET;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.Reporting.Nodes;

/// <summary>
/// Parameters for PlotlyImageExportNode defining the output file path.
/// </summary>
public class PlotlyImageExportParams {
  /// <summary>
  /// Absolute file path where the PNG image will be saved (without .png extension).
  /// The .png extension is automatically added by Plotly.NET.ImageExport.
  /// </summary>
  public string OutputPath { get; set; } = string.Empty;
}

/// <summary>
/// Exports a Plotly GenericChart to PNG format.
/// Reusable node for converting in-memory chart objects to static image files.
/// </summary>
/// <remarks>
/// <para>
/// This node handles the PNG export concern separately from chart generation,
/// enabling a clean separation of visualization logic from output format concerns.
/// The PNG output can be used in reports, presentations, or documentation.
/// </para>
/// <para>
/// <strong>Input:</strong> GenericChart object from memory catalog
/// </para>
/// <para>
/// <strong>Output:</strong> Unit (void) - the node saves files as a side effect
/// </para>
/// <para>
/// <strong>Reusability:</strong> This node can be used for any Plotly chart type
/// (bar, scatter, heatmap, etc.) since it operates on the base GenericChart type.
/// </para>
/// <para>
/// <strong>Implementation Note:</strong> Uses Plotly.NET.ImageExport with PuppeteerSharp
/// backend, which downloads Chromium on first execution for browser-based rendering.
/// </para>
/// </remarks>
public class PlotlyImageExportNode : NodeBase<GenericChart, NoData, PlotlyImageExportParams> {
  protected override Task<IEnumerable<NoData>> Transform(IEnumerable<GenericChart> input) {
    var outputPath = Parameters.OutputPath;

    foreach (var chart in input) {
      // Save chart as PNG using Plotly.NET.ImageExport
      // This will use PuppeteerSharp with Chromium for rendering
      // The SavePNG extension method automatically appends .png extension
      Plotly.NET.ImageExport.GenericChartExtensions.SavePNG(chart, outputPath);

      Logger?.LogInformation(
          "Exported chart to PNG at {Path}.png",
          outputPath);
    }

    // Return empty result since this node saves files as side effect
    return Task.FromResult<IEnumerable<NoData>>(Array.Empty<NoData>());
  }
}
