using Flowthru.Core.Data;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Reporting data layer: Visualizations produced by the Python Reporting pipeline.
/// </summary>
public partial class Catalog
{
    public IItem<string> CapacityPlotExpress =>
      CreateItem(
        () =>
          ItemFactory.Single.Text(
            label: "CapacityPlotExpress",
            filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_passenger_capacity_plot_exp.json"
          )
      );

    public IItem<string> CapacityPlotGraphObj =>
      CreateItem(
        () =>
          ItemFactory.Single.Text(
            label: "CapacityPlotGraphObj",
            filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_passenger_capacity_plot_go.json"
          )
      );

    public IItem<byte[]> ConfusionMatrix =>
      CreateItem(
        () =>
          ItemFactory.Single.Binary(
            label: "ConfusionMatrix",
            filePath: $"{_basePath}/_08_Reporting/Images/confusion_matrix.png"
          )
      );
}
