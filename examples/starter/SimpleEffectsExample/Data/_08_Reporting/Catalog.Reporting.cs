using Flowthru.Core.Data;

namespace SimpleEffectsExample.Data;

/// <summary>
/// Reporting data layer: Final formatted outputs — one file per US timezone.
/// </summary>
public partial class Catalog
{
  /// <summary>Eastern-time report (America/New_York).</summary>
  public IItem<string> EasternTimeReport =>
    CreateItem(
      () => ItemFactory.Single.Text(
        label: "EasternTimeReport",
        filePath: $"{_basePath}/_08_Reporting/Datasets/eastern-time.txt"
      )
    );

  /// <summary>Central-time report (America/Chicago).</summary>
  public IItem<string> CentralTimeReport =>
    CreateItem(
      () => ItemFactory.Single.Text(
        label: "CentralTimeReport",
        filePath: $"{_basePath}/_08_Reporting/Datasets/central-time.txt"
      )
    );

  /// <summary>Mountain-time report (America/Denver).</summary>
  public IItem<string> MountainTimeReport =>
    CreateItem(
      () => ItemFactory.Single.Text(
        label: "MountainTimeReport",
        filePath: $"{_basePath}/_08_Reporting/Datasets/mountain-time.txt"
      )
    );

  /// <summary>Pacific-time report (America/Los_Angeles).</summary>
  public IItem<string> PacificTimeReport =>
    CreateItem(
      () => ItemFactory.Single.Text(
        label: "PacificTimeReport",
        filePath: $"{_basePath}/_08_Reporting/Datasets/pacific-time.txt"
      )
    );
}
