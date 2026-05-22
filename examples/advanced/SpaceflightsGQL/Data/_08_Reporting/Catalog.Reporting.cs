using Flowthru.Data.Catalog;
using SpaceflightsGQL.Data._08_Reporting.Schemas;
using Plotly.NET;

namespace SpaceflightsGQL.Data;

/// <summary>
/// Reporting data layer: Ad hoc descriptive cuts and visualizations.
/// Contains analysis outputs, reports, and visualizations for stakeholders.
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
    CreateItem(() => Item.Of<GenericChart>("ShuttlePassengerCapacityChart").Memory().Build());

  /// <summary>Confusion matrix heatmap (in-memory GenericChart).</summary>
  public IItem<GenericChart> ConfusionMatrixChart =>
    CreateItem(() => Item.Of<GenericChart>("ConfusionMatrixChart").Memory().Build());
}
