using Flowthru.Core.Data;
using Plotly.NET;
using SpaceflightsEFCore.Data._08_Reporting.Schemas;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Reporting data layer: Ad hoc descriptive cuts and visualizations.
/// Contains analysis outputs, reports, and visualizations for stakeholders.
/// </summary>
public partial class Catalog
{
    /// <summary>
    /// Passenger capacity analysis report grouped by shuttle type.
    /// </summary>
    public IItem<IEnumerable<ShuttleCapacityReport>> ShuttleCapacityReport =>
      CreateItem(
        () =>
          ItemFactory.Enumerable.Json<ShuttleCapacityReport>(
            label: "ShuttleCapacityReport",
            filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_capacity_report.json"
          )
      );

    /// <summary>
    /// Shuttle passenger capacity bar chart (in-memory GenericChart).
    /// Intermediate chart object stored in memory for downstream export to PNG.
    /// </summary>
    public IItem<GenericChart> ShuttlePassengerCapacityChart =>
      CreateItem(
        () => ItemFactory.Single.Memory<GenericChart>(label: "ShuttlePassengerCapacityChart")
      );

    /// <summary>
    /// Shuttle passenger capacity bar chart (PNG image).
    /// Static image representation of the passenger capacity visualization.
    /// Stored as binary PNG file.
    /// </summary>
    public IItem<byte[]> ShuttlePassengerCapacityPlotPng =>
      CreateItem(
        () =>
          ItemFactory.Single.Binary(
            label: "ShuttlePassengerCapacityPlotPng",
            filePath: $"{_basePath}/_08_Reporting/Images/shuttle_passenger_capacity_plot.png"
          )
      );

    /// <summary>
    /// Confusion matrix heatmap (in-memory GenericChart).
    /// Intermediate chart object stored in memory for downstream export to PNG.
    /// </summary>
    public IItem<GenericChart> ConfusionMatrixChart =>
      CreateItem(() => ItemFactory.Single.Memory<GenericChart>(label: "ConfusionMatrixChart"));

    /// <summary>
    /// Confusion matrix heatmap (PNG image).
    /// Static image representation of the confusion matrix visualization.
    /// Stored as binary PNG file.
    /// </summary>
    public IItem<byte[]> ConfusionMatrixPlotPng =>
      CreateItem(
        () =>
          ItemFactory.Single.Binary(
            label: "ConfusionMatrixPlotPng",
            filePath: $"{_basePath}/_08_Reporting/Images/confusion_matrix_plot.png"
          )
      );
}
