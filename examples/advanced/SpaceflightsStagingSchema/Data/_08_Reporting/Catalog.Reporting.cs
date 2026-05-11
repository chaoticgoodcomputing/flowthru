using Flowthru.Data.Catalog;
using Plotly.NET;
using SpaceflightsStagingSchema.Data._08_Reporting.Schemas;

namespace SpaceflightsStagingSchema.Data;

public partial class ProductionCatalog
{
  /// <summary>Passenger capacity report grouped by shuttle type (filesystem JSON).</summary>
  public IItem<IEnumerable<ShuttleCapacityReport>> ShuttleCapacityReport =>
    CreateItem(() => Item.Of<IEnumerable<ShuttleCapacityReport>>("ShuttleCapacityReport")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/shuttle_capacity_report.json")
      .Build());

  /// <summary>Confusion matrix heatmap (in-memory chart object).</summary>
  public IItem<GenericChart> ConfusionMatrixChart =>
    CreateItem(() => Item.Of<GenericChart>("ConfusionMatrixChart").Memory().Build());

  /// <summary>Passenger capacity bar chart (in-memory chart object).</summary>
  public IItem<GenericChart> ShuttlePassengerCapacityChart =>
    CreateItem(() => Item.Of<GenericChart>("ShuttlePassengerCapacityChart").Memory().Build());
}
