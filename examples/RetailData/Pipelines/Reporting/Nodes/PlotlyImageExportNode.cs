using Plotly.NET;

namespace RetailData.Pipelines.Reporting.Nodes;

/// <summary>
/// Exports a Plotly GenericChart to PNG binary data.
/// Reusable node for converting in-memory chart objects to storable PNG images.
/// </summary>
public static class PlotlyImageExportNode
{
  public static Func<GenericChart, Task<byte[]>> Create()
  {
    // Configure PuppeteerSharp for CI environment (no sandbox)
    if (Environment.GetEnvironmentVariable("CI") == "true")
    {
      var currentArgs = Plotly.NET.ImageExport.PuppeteerSharpRendererOptions.launchOptions.Args ?? Array.Empty<string>();
      if (!currentArgs.Contains("--no-sandbox"))
      {
        Plotly.NET.ImageExport.PuppeteerSharpRendererOptions.launchOptions.Args =
          currentArgs.Concat(new[] { "--no-sandbox" }).ToArray();
      }
    }

    return async (input) =>
    {
      // Use Plotly.NET.ImageExport to convert the chart to a base64 PNG string
      // This uses a headless browser (Chromium via PuppeteerSharp) to render the chart
      var base64DataUri =
        await Plotly.NET.ImageExport.GenericChartExtensions.ToBase64PNGStringAsync(
          input,
          Width: 1200,
          Height: 800
        );

      // Strip the data URI prefix "data:image/png;base64," to get pure base64
      const string dataUriPrefix = "data:image/png;base64,";
      var base64String = base64DataUri.StartsWith(dataUriPrefix)
        ? base64DataUri.Substring(dataUriPrefix.Length)
        : base64DataUri;

      // Decode base64 to raw PNG bytes
      var pngBytes = Convert.FromBase64String(base64String);

      return pngBytes;
    };
  }
}
