using Flowthru.Core.Steps;
using SimpleEffectsExample.Services;
#if FUNIT_ENABLED
using Flowthru.FUnit;
using Microsoft.Extensions.DependencyInjection;
#endif

namespace SimpleEffectsExample.Flows.Reporting.Steps;

/// <summary>
/// Single-step "effect" demonstrating Flowthru's effect-as-step pattern: this step
/// takes a string template input, calls an injected <see cref="IRemoteTimeService"/>
/// to fetch the current UTC time, and emits the formatted report string as output.
/// </summary>
/// <remarks>
/// <para>
/// The <c>Create(...)</c> factory accepts the service as a parameter. The
/// source-generated <c>ReportTimeStep_Metadata</c> companion records
/// <see cref="IRemoteTimeService"/> as a service dependency, which:
/// </para>
/// <list type="bullet">
///   <item>Flows into <c>FlowStep.ServiceDependencies</c> at flow construction (Phase 4).</item>
///   <item>Triggers preflight inspection via the registered
///   <c>AddFlowthruInspect&lt;IRemoteTimeService&gt;(...)</c> sidecar (Phase 3).</item>
///   <item>Renders as a service node + dashed edge in the Mermaid metadata (Phase 6).</item>
/// </list>
/// <para>
/// <see cref="FlowthruStepAttribute.IsIdempotent"/> is <c>true</c> because re-running
/// the step always emits a fresh report — there's no accumulating state.
/// <see cref="FlowthruStepAttribute.HasSideEffects"/> is <c>true</c> because the step
/// reaches out to an external system.
/// </para>
/// </remarks>
[FlowthruStep(IsIdempotent = true, HasSideEffects = true)]
public static class ReportTimeStep
{
  public static Func<string, Task<string>> Create(IRemoteTimeService timeService) =>
    async template =>
    {
      var now = await timeService.GetCurrentUtcAsync();
      return string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        template,
        now.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture)
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
    [StepTest(typeof(ReportTimeStep))]
    public void FormatsTemplateWithFetchedTime()
    {
      var service = GetRequiredService<IRemoteTimeService>();
      var transform = ReportTimeStep.Create(service);

      // The FUnit runner generator currently emits sync runners; we block on
      // the async transform via GetResult so the test runs deterministically.
      var result = InvokeAsync(transform, "The time is currently {0}").GetAwaiter().GetResult();

      Assert.That(result, Is.EqualTo("The time is currently 2026-04-30T14:00:00Z"));
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
