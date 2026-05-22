using Flowthru.Data.Catalog;
using SpaceflightsFUnit.Data._08_Reporting.Schemas;
using Plotly.NET;

namespace SpaceflightsFUnit.Data;

/// <summary>
/// Reporting data layer: Ad hoc descriptive cuts and visualizations.
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<ShuttleCapacityReport>> ShuttleCapacityReport =>
    CreateItem(() => Item.Of<IEnumerable<ShuttleCapacityReport>>("ShuttleCapacityReport")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/shuttle_capacity_report.json")
      .Build());

  public IItem<GenericChart> ShuttlePassengerCapacityChart =>
    CreateItem(() => Item.Of<GenericChart>("ShuttlePassengerCapacityChart").Memory().Build());

  public IItem<GenericChart> ConfusionMatrixChart =>
    CreateItem(() => Item.Of<GenericChart>("ConfusionMatrixChart").Memory().Build());
}
