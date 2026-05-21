using Flowthru.Data.Catalog;
using IrisPython.Data._08_Reporting.Schemas;

namespace IrisPython.Data;

/// <summary>
/// Reporting layer: Metrics, visualizations, and analysis outputs.
/// </summary>
public partial class Catalog
{
  public IItem<AccuracyReportSchema> AccuracyReport =>
    CreateItem(() => Item.Of<AccuracyReportSchema>("AccuracyReport")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/accuracy_report.json")
      .Build());
}
