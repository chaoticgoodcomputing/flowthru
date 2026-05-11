using Flowthru.Data.Catalog;
using SpaceflightsNewTypes.Data._08_Reporting.Schemas;
using Plotly.NET;

namespace SpaceflightsNewTypes.Data;

/// <summary>
/// Reporting data layer: Ad hoc descriptive cuts and visualizations.
/// </summary>
public partial class Catalog
{
  /// <summary>Passenger capacity analysis report grouped by shuttle type.</summary>
  public IItem<IEnumerable<ShuttleCapacityReport>> ShuttleCapacityReport =>
    CreateItem(() => Item.Of<IEnumerable<ShuttleCapacityReport>>("ShuttleCapacityReport")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/shuttle_capacity_report.json")
      .Build());

  /// <summary>Shuttle passenger capacity bar chart (in-memory GenericChart).</summary>
  public IItem<GenericChart> ShuttlePassengerCapacityChart =>
    CreateItem(() => Item.Of<GenericChart>("ShuttlePassengerCapacityChart")
      .Memory()
      .Build());

  /// <summary>Shuttle passenger capacity bar chart (PNG image).</summary>
  public IItem<byte[]> ShuttlePassengerCapacityPlotPng =>
    CreateItem(() => Item.Of<byte[]>("ShuttlePassengerCapacityPlotPng")
      .Binary()
      .AtPath($"{_basePath}/_08_Reporting/Images/shuttle_passenger_capacity_plot.png")
      .Build());

  /// <summary>Confusion matrix heatmap (in-memory GenericChart).</summary>
  public IItem<GenericChart> ConfusionMatrixChart =>
    CreateItem(() => Item.Of<GenericChart>("ConfusionMatrixChart")
      .Memory()
      .Build());

  /// <summary>Confusion matrix heatmap (PNG image).</summary>
  public IItem<byte[]> ConfusionMatrixPlotPng =>
    CreateItem(() => Item.Of<byte[]>("ConfusionMatrixPlotPng")
      .Binary()
      .AtPath($"{_basePath}/_08_Reporting/Images/confusion_matrix_plot.png")
      .Build());
}
