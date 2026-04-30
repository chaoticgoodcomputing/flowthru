using Flowthru.Core.Data;

namespace SimpleEffectsExample.Data;

/// <summary>
/// Reporting data layer: Final formatted outputs.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Text-file destination for the formatted "current time" report.
  /// </summary>
  public IItem<string> CurrentTimeReport =>
    CreateItem(
      () => ItemFactory.Single.Text(
        label: "CurrentTimeReport",
        filePath: $"{_basePath}/_08_Reporting/Datasets/current-time.txt"
      )
    );
}
