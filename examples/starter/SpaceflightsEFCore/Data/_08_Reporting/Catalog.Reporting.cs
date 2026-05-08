using Flowthru.Data.Catalog;
using Plotly.NET;
using SpaceflightsEFCore.Data._08_Reporting.Schemas;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Reporting data layer: ad hoc descriptive cuts and visualizations.
/// </summary>
public partial class Catalog
{
  /// <summary>Passenger capacity analysis report grouped by shuttle type.</summary>
  public IItem<IEnumerable<ShuttleCapacityReport>> ShuttleCapacityReport =>
    CreateItem(() =>
      ItemFactory.Enumerable.Json<ShuttleCapacityReport>(
        label: "ShuttleCapacityReport",
        filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_capacity_report.json"
      )
    );

  /// <summary>
  /// Shuttle passenger capacity bar chart (in-memory <see cref="GenericChart"/>).
  /// PNG export is currently disabled because Plotly.NET's image
  /// pipeline is too slow to run as part of every flow.
  /// </summary>
  public IItem<GenericChart> ShuttlePassengerCapacityChart =>
    CreateItem(() =>
      ItemFactory.Singleton.Memory<GenericChart>(label: "ShuttlePassengerCapacityChart")
    );

  /// <summary>Confusion matrix heatmap (in-memory <see cref="GenericChart"/>).</summary>
  public IItem<GenericChart> ConfusionMatrixChart =>
    CreateItem(() =>
      ItemFactory.Singleton.Memory<GenericChart>(label: "ConfusionMatrixChart")
    );
}
