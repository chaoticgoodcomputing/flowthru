using Flowthru.Data.Catalog;
using Plotly.NET;
using SpaceflightsHybridCatalog.Data._08_Reporting.Schemas;

namespace SpaceflightsHybridCatalog.Data;

/// <summary>
/// Reporting layer — shared across Development and Production. The report
/// JSON is always emitted to a file under <c>Data/_08_Reporting/</c>, and the
/// intermediate chart objects are in-memory. EF would add no value here.
/// </summary>
public abstract partial class Catalog
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
