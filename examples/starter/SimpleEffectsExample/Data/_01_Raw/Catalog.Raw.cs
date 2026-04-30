using Flowthru.Core.Data;

namespace SimpleEffectsExample.Data;

/// <summary>
/// Raw data layer: Immutable source data, never modified.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Format string consumed by the reporting step. <c>{0}</c> is substituted with
  /// the fetched UTC timestamp.
  /// </summary>
  public IItem<string> ReportTemplate =>
    CreateItem(
      () => ItemFactory.Single.Text(
        label: "ReportTemplate",
        filePath: $"{_basePath}/_01_Raw/Datasets/report-template.txt"
      )
    );
}
