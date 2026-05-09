using Flowthru.Data.Catalog;

namespace SimpleEffectsExample.Data;

/// <summary>
/// Reporting data layer: Final formatted outputs — one file per US timezone.
/// </summary>
public partial class Catalog
{
  /// <summary>Eastern-time report (America/New_York).</summary>
  public IItem<string> EasternTimeReport =>
    CreateItem(() => Item.Of<string>("EasternTimeReport")
      .Text()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/eastern-time.txt")
      .Build());

  /// <summary>Central-time report (America/Chicago).</summary>
  public IItem<string> CentralTimeReport =>
    CreateItem(() => Item.Of<string>("CentralTimeReport")
      .Text()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/central-time.txt")
      .Build());

  /// <summary>Mountain-time report (America/Denver).</summary>
  public IItem<string> MountainTimeReport =>
    CreateItem(() => Item.Of<string>("MountainTimeReport")
      .Text()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/mountain-time.txt")
      .Build());

  /// <summary>Pacific-time report (America/Los_Angeles).</summary>
  public IItem<string> PacificTimeReport =>
    CreateItem(() => Item.Of<string>("PacificTimeReport")
      .Text()
      .AtPath($"{_basePath}/_08_Reporting/Datasets/pacific-time.txt")
      .Build());
}
