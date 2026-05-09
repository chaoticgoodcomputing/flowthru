using Flowthru.Data.Catalog;
using KedroIrisFUnit.Data._08_Reporting.Schemas;

namespace KedroIrisFUnit.Data;

/// <summary>
/// Reporting data layer: Ad hoc analyses and visualizations.
/// </summary>
public partial class Catalog
{
  public IItem<MetricsSchema> Metrics =>
    CreateItem(() => Item.Of<MetricsSchema>("Metrics")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/metrics.json")
      .Build());
}
