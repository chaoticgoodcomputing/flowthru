using Flowthru.Data;
using KedroIrisPython.Data._08_Reporting.Schemas;

namespace KedroIrisPython.Data;

/// <summary>
/// Reporting layer: Metrics, visualizations, and analysis outputs.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Model accuracy report with detailed metrics.
  /// </summary>
  public ICatalogEntry<AccuracyReportSchema> AccuracyReport =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<AccuracyReportSchema>(
          label: "AccuracyReport",
          filePath: $"{_basePath}/_08_Reporting/Datasets/accuracy_report.json"
        )
    );
}
