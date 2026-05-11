using Flowthru.Data.Catalog;
using Flowthru.Flow;
using SimpleEffectsExample.Data;
using SimpleEffectsExample.Flows.Reporting.Steps;

namespace SimpleEffectsExample.Flows.Reporting;

/// <summary>
/// Fan-out pipeline: read a single format template, then for each US timezone call
/// the injected time service, convert UTC → local, and write a per-zone report file.
/// All four steps share a single <see cref="Services.IRemoteTimeService"/>
/// dependency, which renders as a single service node in the DAG.
/// </summary>
public static class ReportTimeFlow
{
  private static readonly (string Label, string ZoneId, string Abbrev, Func<Catalog, IItem<string>> Output)[] Zones =
  {
    ("ReportEastern",  "America/New_York",    "ET", c => c.EasternTimeReport),
    ("ReportCentral",  "America/Chicago",     "CT", c => c.CentralTimeReport),
    ("ReportMountain", "America/Denver",      "MT", c => c.MountainTimeReport),
    ("ReportPacific",  "America/Los_Angeles", "PT", c => c.PacificTimeReport),
  };

  public static BuiltFlow Create(Catalog catalog, Services.IRemoteTimeService timeService)
  {
    return FlowBuilder.CreateFlow("ReportTime", pipeline =>
    {
      foreach (var (label, zoneId, abbrev, outputSelector) in Zones)
      {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        pipeline.AddStep<string, string>(
          label: label,
          transform: ReportTimeStep.Create(timeService, zone, abbrev),
          inputs: catalog.ReportTemplate,
          outputs: outputSelector(catalog)
        );
      }
    });
  }
}
