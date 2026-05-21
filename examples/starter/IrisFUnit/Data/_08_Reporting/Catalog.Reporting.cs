using Flowthru.Data.Catalog;
using IrisFUnit.Data._08_Reporting.Schemas;

namespace IrisFUnit.Data;

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
