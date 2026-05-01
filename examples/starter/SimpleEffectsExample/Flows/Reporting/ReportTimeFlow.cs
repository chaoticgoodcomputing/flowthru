using Flowthru.Core.Flows;
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
  private static readonly (string Label, string ZoneId, string Abbrev, Func<Catalog, Flowthru.Core.Data.IItem<string>> Output)[] Zones =
  {
    ("ReportEastern",  "America/New_York",    "ET", c => c.EasternTimeReport),
    ("ReportCentral",  "America/Chicago",     "CT", c => c.CentralTimeReport),
    ("ReportMountain", "America/Denver",      "MT", c => c.MountainTimeReport),
    ("ReportPacific",  "America/Los_Angeles", "PT", c => c.PacificTimeReport),
  };

  public static Flow Create(Catalog catalog, Services.IRemoteTimeService timeService)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      foreach (var (label, zoneId, abbrev, outputSelector) in Zones)
      {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        pipeline.AddStep(
          label: label,
          description: $"Fetches current UTC time and writes a {abbrev} report.",
          transform: ReportTimeStep.Create(timeService, zone, abbrev),
          input: catalog.ReportTemplate,
          output: outputSelector(catalog)
        );
      }
    });
  }
}
