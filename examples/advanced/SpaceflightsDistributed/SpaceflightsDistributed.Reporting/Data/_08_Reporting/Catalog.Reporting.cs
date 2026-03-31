using Flowthru.Data;
using Plotly.NET;
using SpaceflightsDistributed.Reporting.Data._08_Reporting.Schemas;

namespace SpaceflightsDistributed.Reporting.Data;

/// <summary>
/// Reporting data layer: Ad hoc descriptive cuts and visualizations.
/// </summary>
public partial class ReportingCatalog
{
  public ICatalogEntry<IEnumerable<ShuttleCapacityReport>> ShuttleCapacityReport =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<ShuttleCapacityReport>(
          label: "ShuttleCapacityReport",
          filePath: $"{_basePath}/_08_Reporting/Datasets/shuttle_capacity_report.json"
        )
    );

  public ICatalogEntry<GenericChart> ShuttlePassengerCapacityChart =>
    GetOrCreateEntry(
      () => CatalogEntries.Single.Memory<GenericChart>(label: "ShuttlePassengerCapacityChart")
    );

  public ICatalogEntry<GenericChart> ConfusionMatrixChart =>
    GetOrCreateEntry(
      () => CatalogEntries.Single.Memory<GenericChart>(label: "ConfusionMatrixChart")
    );
}
