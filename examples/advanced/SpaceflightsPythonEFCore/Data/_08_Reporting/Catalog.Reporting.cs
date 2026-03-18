using Flowthru.Data;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Reporting data layer: Visualizations produced by the Python Reporting pipeline.
/// </summary>
public partial class Catalog
{
  public ICatalogEntry<string> CapacityPlotExpress =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "CapacityPlotExpress",
          filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_passenger_capacity_plot_exp.json"
        )
    );

  public ICatalogEntry<string> CapacityPlotGraphObj =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "CapacityPlotGraphObj",
          filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_passenger_capacity_plot_go.json"
        )
    );

  public ICatalogEntry<byte[]> ConfusionMatrix =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "ConfusionMatrix",
          filePath: $"{_basePath}/_08_Reporting/Images/confusion_matrix.png"
        )
    );
}
