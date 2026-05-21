using Flowthru.Data.Catalog;
using SpaceflightsEnhanced.Data._06_Reporting.Schemas;
using Plotly.NET;

namespace SpaceflightsEnhanced.Data;

public partial class Catalog
{
  /// <summary>Cross-validation results with R² distribution analysis.</summary>
  public IItem<CrossValidationResults> CrossValidationResults =>
    CreateItem(() => Item.Of<CrossValidationResults>("CrossValidationResults")
      .Json()
      .AtPath($"{_basePath}/_06_Reporting/Datasets/cross_validation_results.json")
      .Build());

  /// <summary>Cross-validation summary report in Markdown format.</summary>
  public IItem<string> CrossValidationReport =>
    CreateItem(() => Item.Of<string>("CrossValidationReport")
      .Text()
      .AtPath($"{_basePath}/_06_Reporting/Datasets/cross_validation_report.md")
      .Build());

  /// <summary>Shuttle passenger capacity bar chart (in-memory GenericChart).</summary>
  public IItem<GenericChart> ShuttlePassengerCapacityChart =>
    CreateItem(() => Item.Of<GenericChart>("ShuttlePassengerCapacityChart")
      .Memory()
      .Build());

  /// <summary>Shuttle passenger capacity visualization (Plotly JSON).</summary>
  public IItem<string> ShuttlePassengerCapacityPlot =>
    CreateItem(() => Item.Of<string>("ShuttlePassengerCapacityPlot")
      .Text()
      .AtPath($"{_basePath}/_06_Reporting/Datasets/shuttle_passenger_capacity_plot.json")
      .Build());

  /// <summary>Confusion matrix heatmap (in-memory GenericChart).</summary>
  public IItem<GenericChart> ConfusionMatrixChart =>
    CreateItem(() => Item.Of<GenericChart>("ConfusionMatrixChart")
      .Memory()
      .Build());

  /// <summary>Confusion matrix heatmap visualization (Plotly JSON).</summary>
  public IItem<string> ConfusionMatrixPlot =>
    CreateItem(() => Item.Of<string>("ConfusionMatrixPlot")
      .Text()
      .AtPath($"{_basePath}/_06_Reporting/Datasets/confusion_matrix_plot.json")
      .Build());

  /// <summary>Shuttle passenger capacity bar chart (PNG image).</summary>
  public IItem<byte[]> ShuttlePassengerCapacityPlotPng =>
    CreateItem(() => Item.Of<byte[]>("ShuttlePassengerCapacityPlotPng")
      .Binary()
      .AtPath($"{_basePath}/_06_Reporting/Datasets/shuttle_passenger_capacity_plot.png")
      .Build());

  /// <summary>Confusion matrix heatmap (PNG image).</summary>
  public IItem<byte[]> ConfusionMatrixPlotPng =>
    CreateItem(() => Item.Of<byte[]>("ConfusionMatrixPlotPng")
      .Binary()
      .AtPath($"{_basePath}/_06_Reporting/Datasets/confusion_matrix_plot.png")
      .Build());

  /// <summary>Cross-validation visualization chart (in-memory GenericChart).</summary>
  public IItem<GenericChart> CrossValidationChart =>
    CreateItem(() => Item.Of<GenericChart>("CrossValidationChart")
      .Memory()
      .Build());

  /// <summary>Cross-validation visualization (Plotly JSON).</summary>
  public IItem<string> CrossValidationPlot =>
    CreateItem(() => Item.Of<string>("CrossValidationPlot")
      .Text()
      .AtPath($"{_basePath}/_06_Reporting/Datasets/cross_validation_plot.json")
      .Build());

  /// <summary>Cross-validation visualization (PNG image).</summary>
  public IItem<byte[]> CrossValidationPlotPng =>
    CreateItem(() => Item.Of<byte[]>("CrossValidationPlotPng")
      .Binary()
      .AtPath($"{_basePath}/_06_Reporting/Datasets/cross_validation_plot.png")
      .Build());

  /// <summary>Prediction scatter plot chart (in-memory GenericChart).</summary>
  public IItem<GenericChart> PredictionScatterChart =>
    CreateItem(() => Item.Of<GenericChart>("PredictionScatterChart")
      .Memory()
      .Build());

  /// <summary>Prediction scatter plot visualization (Plotly JSON).</summary>
  public IItem<string> PredictionScatterPlot =>
    CreateItem(() => Item.Of<string>("PredictionScatterPlot")
      .Text()
      .AtPath($"{_basePath}/_06_Reporting/Datasets/prediction_scatter_plot.json")
      .Build());

  /// <summary>Prediction scatter plot visualization (PNG image).</summary>
  public IItem<byte[]> PredictionScatterPlotPng =>
    CreateItem(() => Item.Of<byte[]>("PredictionScatterPlotPng")
      .Binary()
      .AtPath($"{_basePath}/_06_Reporting/Datasets/prediction_scatter_plot.png")
      .Build());
}
