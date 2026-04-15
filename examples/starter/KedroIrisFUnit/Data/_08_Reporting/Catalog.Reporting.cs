using Flowthru.Core.Data;
using KedroIrisFUnit.Data._08_Reporting.Schemas;

namespace KedroIrisFUnit.Data;

/// <summary>
/// Reporting data layer: Ad hoc analyses and visualizations.
/// </summary>
public partial class Catalog
{
    /// <summary>
    /// Model evaluation metrics including accuracy and confusion statistics.
    /// </summary>
    public IItem<MetricsSchema> Metrics =>
      CreateItem(
        () =>
          ItemFactory.Single.Json<MetricsSchema>(
            label: "Metrics",
            filePath: $"{_basePath}/_08_Reporting/Datasets/metrics.json"
          )
      );
}
