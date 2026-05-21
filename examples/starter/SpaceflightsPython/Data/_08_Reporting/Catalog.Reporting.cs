using Flowthru.Data.Catalog;

namespace SpaceflightsPython.Data;

/// <summary>
/// Reporting data layer: Ad hoc descriptive cuts and visualizations.
/// </summary>
public partial class Catalog
{
  public IItem<string> CapacityPlotExpress =>
    CreateItem(() => Item.Of<string>("CapacityPlotExpress")
      .Text()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/shuttle_passenger_capacity_plot_exp.json")
      .Build());

  public IItem<string> CapacityPlotGraphObj =>
    CreateItem(() => Item.Of<string>("CapacityPlotGraphObj")
      .Text()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/shuttle_passenger_capacity_plot_go.json")
      .Build());

  public IItem<byte[]> ConfusionMatrix =>
    CreateItem(() => Item.Of<byte[]>("ConfusionMatrix")
      .Binary()
      .AtPath($"{_basePath}/_08_Reporting/Images/confusion_matrix.png")
      .Build());
}
