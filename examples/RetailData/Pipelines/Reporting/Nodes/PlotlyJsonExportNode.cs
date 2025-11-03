using Plotly.NET;

namespace RetailData.Pipelines.Reporting.Nodes;

/// <summary>
/// Exports a Plotly GenericChart to JSON format.
/// Reusable node for converting in-memory chart objects to plotly.js-compatible JSON.
/// </summary>
public static class PlotlyJsonExportNode
{
  public static Func<GenericChart, Task<string>> Create()
  {
    return async (input) =>
    {
      // Serialize to Plotly JSON using GenericChart.toJson()
      var plotlyJson = GenericChart.toJson(input);
      return await Task.FromResult(plotlyJson);
    };
  }
}
