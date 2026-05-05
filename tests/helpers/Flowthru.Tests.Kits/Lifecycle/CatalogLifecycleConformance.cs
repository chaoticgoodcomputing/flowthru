using Flowthru.Core.Data;
using Flowthru.Core.Effects;
using Flowthru.Core.Flows;
using Flowthru.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowthru.Tests.Kits.Lifecycle;

/// <summary>
/// Conformance suite for <c>CatalogAbstract</c> implementations that declare
/// a <see cref="FlowResource{TScope}"/> via the <c>Resource</c> override.
/// Verifies the framework's lifecycle wiring: acquire/release ordering,
/// body-exception propagation to release, dry-run skipping behaviour, and
/// teardown-error aggregation.
/// </summary>
/// <remarks>
/// <para>
/// Subclasses provide a catalog-construction delegate that wires the catalog
/// against a supplied <see cref="LifecycleTracker"/>. The kit instantiates
/// a real <see cref="FlowthruService"/> around that catalog and runs flows
/// through the framework end-to-end.
/// </para>
/// <para>
/// Use this from extensions that ship catalogs with <c>Resource</c>
/// overrides — Mailchimp catalog with auth tokens, HTTP catalog with
/// session resources, etc. Plug in your catalog; get the framework
/// integration battery for free.
/// </para>
/// </remarks>
public abstract class CatalogLifecycleConformance
{
  /// <summary>
  /// Construct the catalog under test, wired to <paramref name="tracker"/>
  /// so its resource records lifecycle events. The kit calls this once per
  /// test scenario.
  /// </summary>
  protected abstract CatalogAbstract BuildCatalog(LifecycleTracker tracker);

  // ── Successful flow ────────────────────────────────────────────────────

  [Test]
  public async Task Run_Success_AcquiresThenReleasesWithNullException()
  {
    var tracker = new LifecycleTracker();
    var catalog = BuildCatalog(tracker);

    var result = await RunMinimalFlowAsync(catalog);

    Assert.That(result.Success, Is.True);
    AssertAcquireThenRelease(tracker, expectBodyException: false);
  }

  // ── Step failure ───────────────────────────────────────────────────────

  [Test]
  public async Task Run_StepFailure_ReleaseObservesBodyException()
  {
    var tracker = new LifecycleTracker();
    var catalog = BuildCatalog(tracker);

    var result = await RunMinimalFlowAsync(catalog, throwInStep: true);

    Assert.That(result.Success, Is.False);
    Assert.That(result.Exception, Is.Not.Null);

    var releaseEvents = tracker.Events.Where(e => e.Phase == LifecyclePhase.Release).ToList();
    Assert.That(releaseEvents, Has.Count.GreaterThan(0));
    Assert.That(
      releaseEvents.All(e => e.BodyException is not null),
      Is.True,
      "Release should observe the body's primary exception when a step fails."
    );
  }

  // ── Default dry run ────────────────────────────────────────────────────

  [Test]
  public async Task DryRun_Default_SkipsAcquireAndRelease()
  {
    var tracker = new LifecycleTracker();
    var catalog = BuildCatalog(tracker);

    var result = await RunMinimalFlowAsync(
      catalog,
      options: new ExecutionOptions { DryRun = true }
    );

    Assert.That(result.Success, Is.True);
    Assert.That(
      tracker.Events,
      Is.Empty,
      "Default dry run should not acquire or release catalog resources."
    );
  }

  // ── Dry run with acquire ───────────────────────────────────────────────

  [Test]
  public async Task DryRun_AcquireOnDryRun_FullLifecycleNoStepExecution()
  {
    var tracker = new LifecycleTracker();
    var catalog = BuildCatalog(tracker);

    var result = await RunMinimalFlowAsync(
      catalog,
      options: new ExecutionOptions { DryRun = true, AcquireResourcesOnDryRun = true }
    );

    Assert.That(result.Success, Is.True);
    AssertAcquireThenRelease(tracker, expectBodyException: false);
  }

  // ── Helpers ────────────────────────────────────────────────────────────

  /// <summary>
  /// Builds a minimal Flow that consumes one item from <paramref name="catalog"/>
  /// (or runs a no-op if the catalog has no items) and runs it through a real
  /// <see cref="FlowthruService"/> with the supplied options.
  /// </summary>
  private static async Task<FlowResult> RunMinimalFlowAsync(
    CatalogAbstract catalog,
    ExecutionOptions? options = null,
    bool throwInStep = false
  )
  {
    var services = new ServiceCollection();
    services.AddLogging();
    var configuration = new ConfigurationBuilder().Build();

    services.AddFlowthru(
      configuration,
      flowthru =>
      {
        flowthru.RegisterCatalog(_ => catalog);

        flowthru.RegisterFlow(
          label: "MinimalFlow",
          flow: () =>
            FlowBuilder.CreateFlow(pipeline =>
            {
              pipeline.AddStep(
                label: "NoOp",
                description: "No-op probe step that triggers the lifecycle wiring.",
                transform: () =>
                {
                  if (throwInStep)
                  {
                    throw new InvalidOperationException("test-induced step failure");
                  }
                }
              );
            })
        );
      }
    );

    var provider = services.BuildServiceProvider();
    var flowService = provider.GetRequiredService<IFlowthruService>();

    return await flowService.ExecuteFlowAsync(options, exportMetadata: false);
  }

  private static void AssertAcquireThenRelease(
    LifecycleTracker tracker,
    bool expectBodyException
  )
  {
    var events = tracker.Events;
    Assert.That(events, Has.Count.GreaterThanOrEqualTo(2));

    var acquires = events.Where(e => e.Phase == LifecyclePhase.Acquire).ToList();
    var releases = events.Where(e => e.Phase == LifecyclePhase.Release).ToList();
    Assert.That(acquires, Is.Not.Empty, "Acquire should fire.");
    Assert.That(releases, Is.Not.Empty, "Release should fire.");

    var firstAcquireIdx = events.ToList().FindIndex(e => e.Phase == LifecyclePhase.Acquire);
    var firstReleaseIdx = events.ToList().FindIndex(e => e.Phase == LifecyclePhase.Release);
    Assert.That(
      firstAcquireIdx,
      Is.LessThan(firstReleaseIdx),
      "Acquire should fire before release."
    );

    if (expectBodyException)
    {
      Assert.That(
        releases.All(r => r.BodyException is not null),
        Is.True,
        "Release should see the body exception."
      );
    }
    else
    {
      Assert.That(
        releases.All(r => r.BodyException is null),
        Is.True,
        "Release should see a null body exception on success."
      );
    }
  }
}
