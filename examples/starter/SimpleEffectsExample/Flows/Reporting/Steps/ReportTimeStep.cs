using Flowthru.Step;
using SimpleEffectsExample.Services;
#if FUNIT_ENABLED
using Flowthru.Step.Testing;
using Microsoft.Extensions.DependencyInjection;
#endif

namespace SimpleEffectsExample.Flows.Reporting.Steps;

/// <summary>
/// Effect step that fetches the current UTC time via the injected
/// <see cref="IRemoteTimeService"/>, converts it to a target time zone, and
/// renders a formatted line against the input template.
/// </summary>
/// <remarks>
/// <para>
/// The factory accepts the service plus a <see cref="TimeZoneInfo"/> and a short
/// label (e.g., "ET"). The flow adds the same step four times — once per US
/// timezone — so all four steps share a single <see cref="IRemoteTimeService"/>
/// service node in the rendered DAG (one node, four dashed <c>-.uses.-&gt;</c>
/// edges).
/// </para>
/// <para>
/// Source-generated <c>ReportTimeStep_Metadata</c> records
/// <see cref="IRemoteTimeService"/> as the only service dependency;
/// <see cref="TimeZoneInfo"/> and <see cref="string"/> are non-interface params
/// and skipped by the metadata generator's service-detection heuristic.
/// </para>
/// </remarks>
[FlowthruStep(IsIdempotent = true, HasSideEffects = true)]
public static class ReportTimeStep
{
  public static Func<string, Task<string>> Create(
    IRemoteTimeService timeService,
    TimeZoneInfo timeZone,
    string zoneLabel
  ) =>
    async template =>
    {
      var utc = await timeService.GetCurrentUtcAsync();
      var local = TimeZoneInfo.ConvertTime(utc, timeZone);
      return string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        template,
        $"{local:yyyy-MM-dd HH:mm:ss} {zoneLabel}"
      );
    };

#if FUNIT_ENABLED

  /// <summary>
  /// FUnit stub container. <see cref="FUnitContext"/> reflects on the test assembly
  /// for <c>[FUnitStubContainer]</c>-attributed types and invokes their
  /// <c>Configure</c> method against the per-test DI container.
  /// </summary>
  [FUnitStubContainer]
  internal static class TestStubs
  {
    public static void Configure(IServiceCollection services)
    {
      services.AddSingleton<IRemoteTimeService, FixedTimeService>();
    }
  }

  /// <summary>FUnit tests for <see cref="ReportTimeStep"/>.</summary>
  /// <remarks>
  /// Demonstrates the <c>[FUnitStubContainer]</c> + <c>[StepTest]</c> pattern.
  /// The stub container at the bottom of this file registers a
  /// <see cref="FixedTimeService"/> so tests are deterministic and never hit the network.
  /// </remarks>
  public class Tests : FUnitContext
  {
    [FUnitStepTest(typeof(ReportTimeStep))]
    public void FormatsTemplateWithEasternTime()
    {
      var service = GetRequiredService<IRemoteTimeService>();
      // 2026-04-30 14:00 UTC → 10:00 ET (DST: UTC-4)
      var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
      var transform = ReportTimeStep.Create(service, eastern, "ET");

      // The FUnit runner generator currently emits sync runners; we block on
      // the async transform via GetResult so the test runs deterministically.
      var result = InvokeAsync(transform, "The time is currently {0}").GetAwaiter().GetResult();

      Assert.That(result, Is.EqualTo("The time is currently 2026-04-30 10:00:00 ET"));
    }
  }

  /// <summary>
  /// Deterministic <see cref="IRemoteTimeService"/> for tests. The stub container
  /// below registers it; <see cref="FUnitContext"/>'s constructor discovers the
  /// container at fixture instantiation and populates the test's DI container.
  /// </summary>
  internal sealed class FixedTimeService : IRemoteTimeService
  {
    public Task<DateTimeOffset> GetCurrentUtcAsync(CancellationToken cancellationToken = default) =>
      Task.FromResult(new DateTimeOffset(2026, 04, 30, 14, 0, 0, TimeSpan.Zero));
  }

#endif
}
