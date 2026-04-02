using Flowthru.Data;
using Plotly.NET;
using SpaceflightsDistributed.Reporting.Data._08_Reporting.Schemas;

namespace SpaceflightsDistributed.Reporting.Data;

/// <summary>
/// Reporting data layer: Ad hoc descriptive cuts and visualizations.
/// </summary>
public partial class ReportingCatalog
{
  public IItem<IEnumerable<ShuttleCapacityReport>> ShuttleCapacityReport =>
    CreateItem(
      () =>
        Items.Enumerable.Json<ShuttleCapacityReport>(
          label: "ShuttleCapacityReport",
          filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_capacity_report.json"
        )
    );

  public IItem<GenericChart> ShuttlePassengerCapacityChart =>
    CreateItem(() => Items.Single.Memory<GenericChart>(label: "ShuttlePassengerCapacityChart"));

  public IItem<GenericChart> ConfusionMatrixChart =>
    CreateItem(() => Items.Single.Memory<GenericChart>(label: "ConfusionMatrixChart"));
}
