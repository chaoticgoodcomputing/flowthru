using Flowthru.Core.Data;
using KedroSpaceflights.Custom.Data._06_Reporting.Schemas;
using Plotly.NET;

namespace KedroSpaceflights.Custom.Data;

public partial class Catalog
{
    /// <summary>
    /// Cross-validation results with R² distribution analysis.
    /// Contains metrics for each fold, mean, std dev, and comparison to Kedro.
    /// Stored as JSON to preserve nested List&lt;FoldMetric&gt; structure.
    /// </summary>
    public IItem<CrossValidationResults> CrossValidationResults =>
      CreateItem(
        () =>
          ItemFactory.Single.Json<CrossValidationResults>(
            label: "CrossValidationResults",
            filePath: $"{_basePath}/_06_Reporting/Datasets/cross_validation_results.json"
          )
      );

    /// <summary>
    /// Cross-validation summary report in Markdown format.
    /// Human-readable report summarizing model performance and validation results.
    /// </summary>
    public IItem<string> CrossValidationReport =>
      CreateItem(
        () =>
          ItemFactory.Single.Text(
            label: "CrossValidationReport",
            filePath: $"{_basePath}/_06_Reporting/Datasets/cross_validation_report.md"
          )
      );

    /// <summary>
    /// Shuttle passenger capacity bar chart (in-memory GenericChart).
    /// Intermediate chart object stored in memory for downstream export to multiple formats.
    /// </summary>
    public IItem<GenericChart> ShuttlePassengerCapacityChart =>
      CreateItem(
        () => ItemFactory.Single.Memory<GenericChart>(label: "ShuttlePassengerCapacityChart")
      );

    /// <summary>
    /// Shuttle passenger capacity visualization (Plotly JSON).
    /// Bar chart showing average passenger capacity grouped by shuttle type.
    /// Stored as Plotly JSON specification, compatible with plotly.js rendering.
    /// </summary>
    /// <remarks>
    /// Output format matches Kedro's plotly.JSONDataset. The JSON contains a complete Plotly
    /// figure specification with data traces and layout configuration. Can be rendered in browsers
    /// using plotly.js or converted to static images using Plotly.NET.ImageExport.
    /// </remarks>
    public IItem<string> ShuttlePassengerCapacityPlot =>
      CreateItem(
        () =>
          ItemFactory.Single.Text(
            label: "ShuttlePassengerCapacityPlot",
            filePath: $"{_basePath}/_06_Reporting/Datasets/shuttle_passenger_capacity_plot.json"
          )
      );

    /// <summary>
    /// Confusion matrix heatmap (in-memory GenericChart).
    /// Intermediate chart object stored in memory for downstream export to multiple formats.
    /// </summary>
    public IItem<GenericChart> ConfusionMatrixChart =>
      CreateItem(() => ItemFactory.Single.Memory<GenericChart>(label: "ConfusionMatrixChart"));

    /// <summary>
    /// Confusion matrix heatmap visualization (Plotly JSON).
    /// Shows model prediction accuracy with actual vs predicted classification matrix.
    /// Stored as Plotly JSON specification for interactive visualization.
    /// </summary>
    /// <remarks>
    /// Matches Kedro's matplotlib.MatplotlibWriter output but using Plotly for interactivity.
    /// The heatmap displays a 2x2 confusion matrix with color-coded cells showing classification
    /// performance. JSON format allows browser-based rendering and potential conversion to PNG.
    /// </remarks>
    public IItem<string> ConfusionMatrixPlot =>
      CreateItem(
        () =>
          ItemFactory.Single.Text(
            label: "ConfusionMatrixPlot",
            filePath: $"{_basePath}/_06_Reporting/Datasets/confusion_matrix_plot.json"
          )
      );

    /// <summary>
    /// Shuttle passenger capacity bar chart (PNG image).
    /// Static image representation of the passenger capacity visualization.
    /// Stored as binary PNG file.
    /// </summary>
    /// <remarks>
    /// Uses ItemFactory.Binary factory method to store actual PNG binary data with proper file format.
    /// The PNG file can be opened directly in image viewers or embedded in reports.
    /// </remarks>
    public IItem<byte[]> ShuttlePassengerCapacityPlotPng =>
      CreateItem(
        () =>
          ItemFactory.Single.Binary(
            label: "ShuttlePassengerCapacityPlotPng",
            filePath: $"{_basePath}/_06_Reporting/Datasets/shuttle_passenger_capacity_plot.png"
          )
      );

    /// <summary>
    /// Confusion matrix heatmap (PNG image).
    /// Static image representation of the confusion matrix visualization.
    /// Stored as binary PNG file.
    /// </summary>
    /// <remarks>
    /// Uses ItemFactory.Binary factory method to store actual PNG binary data with proper file format.
    /// The PNG file can be opened directly in image viewers or embedded in reports.
    /// </remarks>
    public IItem<byte[]> ConfusionMatrixPlotPng =>
      CreateItem(
        () =>
          ItemFactory.Single.Binary(
            label: "ConfusionMatrixPlotPng",
            filePath: $"{_basePath}/_06_Reporting/Datasets/confusion_matrix_plot.png"
          )
      );

    /// <summary>
    /// Cross-validation visualization chart (in-memory GenericChart).
    /// Intermediate chart object showing R² distribution analysis with scatter plot,
    /// normal curve, mean line, and Kedro reference line.
    /// </summary>
    public IItem<GenericChart> CrossValidationChart =>
      CreateItem(() => ItemFactory.Single.Memory<GenericChart>(label: "CrossValidationChart"));

    /// <summary>
    /// Cross-validation visualization (Plotly JSON).
    /// Multi-layer chart showing R² distribution from cross-validation with:
    /// - Scatter plot of fold R² scores
    /// - Normal distribution curve fit
    /// - Mean R² vertical line
    /// - Kedro reference R² vertical line
    /// </summary>
    /// <remarks>
    /// Stored as Plotly JSON specification for interactive visualization.
    /// </remarks>
    public IItem<string> CrossValidationPlot =>
      CreateItem(
        () =>
          ItemFactory.Single.Text(
            label: "CrossValidationPlot",
            filePath: $"{_basePath}/_06_Reporting/Datasets/cross_validation_plot.json"
          )
      );

    /// <summary>
    /// Cross-validation visualization (PNG image).
    /// Static image representation of the cross-validation analysis chart.
    /// Stored as binary PNG file.
    /// </summary>
    /// <remarks>
    /// Uses ItemFactory.Binary factory method to store actual PNG binary data with proper file format.
    /// The PNG file can be opened directly in image viewers or embedded in reports.
    /// </remarks>
    public IItem<byte[]> CrossValidationPlotPng =>
      CreateItem(
        () =>
          ItemFactory.Single.Binary(
            label: "CrossValidationPlotPng",
            filePath: $"{_basePath}/_06_Reporting/Datasets/cross_validation_plot.png"
          )
      );

    /// <summary>
    /// Prediction scatter plot chart (in-memory GenericChart).
    /// Intermediate chart object showing actual vs predicted values with color-coded dots
    /// (yellow for over-estimates, red for under-estimates) and a 1:1 identity reference line.
    /// </summary>
    public IItem<GenericChart> PredictionScatterChart =>
      CreateItem(() => ItemFactory.Single.Memory<GenericChart>(label: "PredictionScatterChart"));

    /// <summary>
    /// Prediction scatter plot visualization (Plotly JSON).
    /// Scatter plot showing actual vs predicted prices with:
    /// - Yellow dots for over-estimates (predicted > actual)
    /// - Red dots for under-estimates (predicted <= actual)
    /// - Dotted gray line for perfect prediction (1:1 identity)
    /// - R² score in title
    /// </summary>
    /// <remarks>
    /// Stored as Plotly JSON specification for interactive visualization.
    /// </remarks>
    public IItem<string> PredictionScatterPlot =>
      CreateItem(
        () =>
          ItemFactory.Single.Text(
            label: "PredictionScatterPlot",
            filePath: $"{_basePath}/_06_Reporting/Datasets/prediction_scatter_plot.json"
          )
      );

    /// <summary>
    /// Prediction scatter plot visualization (PNG image).
    /// Static image representation of the prediction accuracy visualization.
    /// Stored as binary PNG file at 600x600 resolution.
    /// </summary>
    /// <remarks>
    /// Uses ItemFactory.Binary factory method to store actual PNG binary data with proper file format.
    /// The PNG file can be opened directly in image viewers or embedded in reports.
    /// </remarks>
    public IItem<byte[]> PredictionScatterPlotPng =>
      CreateItem(
        () =>
          ItemFactory.Single.Binary(
            label: "PredictionScatterPlotPng",
            filePath: $"{_basePath}/_06_Reporting/Datasets/prediction_scatter_plot.png"
          )
      );
}
