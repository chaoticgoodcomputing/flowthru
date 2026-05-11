using Flowthru.Data.Catalog;

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
    CreateItem(() => Item.Of<string>("ReportTemplate")
      .Text()
      .AtPath($"{_basePath}/_01_Raw/Datasets/report-template.txt")
      .Build());
}
