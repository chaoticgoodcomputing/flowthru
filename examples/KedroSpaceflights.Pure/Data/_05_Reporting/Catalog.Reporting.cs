using Flowthru.Data;
using KedroSpaceflights.Pure.Data._05_Reporting.Schemas;

namespace KedroSpaceflights.Pure.Data;

public partial class Catalog
{
  public ICatalogEntry<IEnumerable<ShuttleCapacityReport>> ShuttleCapacityReport =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<ShuttleCapacityReport>(
          label: "ShuttleCapacityReport",
          filePath: $"{_basePath}/_05_Reporting/Datasets/shuttle_capacity_report.json"
        )
    );
}
