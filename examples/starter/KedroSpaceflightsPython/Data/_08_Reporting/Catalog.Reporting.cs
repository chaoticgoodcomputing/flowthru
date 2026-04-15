using Flowthru.Core.Data;

namespace KedroSpaceflightsPython.Data;

/// <summary>
/// Reporting data layer: Ad hoc descriptive cuts and visualizations.
/// Contains analysis outputs, reports, and visualizations for stakeholders.
/// </summary>
public partial class Catalog
{
    /// <summary>
    /// Passenger capacity comparison visualization (plotly.express format).
    /// Stores JSON representation of plotly figure for shuttle capacity by type.
    /// </summary>
    public IItem<string> CapacityPlotExpress =>
      CreateItem(
        () =>
          ItemFactory.Single.Text(
            label: "CapacityPlotExpress",
            filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_passenger_capacity_plot_exp.json"
          )
      );

    /// <summary>
    /// Passenger capacity comparison visualization (plotly.graph_objects format).
    /// Stores JSON representation of plotly figure for shuttle capacity by type.
    /// </summary>
    public IItem<string> CapacityPlotGraphObj =>
      CreateItem(
        () =>
          ItemFactory.Single.Text(
            label: "CapacityPlotGraphObj",
            filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_passenger_capacity_plot_go.json"
          )
      );

    /// <summary>
    /// Confusion matrix visualization from model predictions.
    /// Shows actual vs predicted price categories (Low/Medium/High) as a heatmap.
    /// Stores PNG image as binary data.
    /// </summary>
    public IItem<byte[]> ConfusionMatrix =>
      CreateItem(
        () =>
          ItemFactory.Single.Binary(
            label: "ConfusionMatrix",
            filePath: $"{_basePath}/_08_Reporting/Images/confusion_matrix.png"
          )
      );
}
