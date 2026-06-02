using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Hosting;

/// <summary>
/// Tests for the registration-validation hook surface — IRegistrationValidationHook,
/// the IFlowthruBuilder.RegisterValidationHook overloads, and the
/// IFlowthruService.ValidateRegistrationAsync auto-invocation contract.
/// </summary>
[TestFixture]
public class RegistrationValidationHookTests
{
  /// <summary>Trivial catalog used by tests that need RegisterCatalog wired.</summary>
  public sealed class EmptyCatalog : CatalogAbstract { }

  // ── ValidateRegistrationAsync direct usage ──────────────────────────

  [Test]
  public async Task ValidateRegistrationAsync_NoHooks_ReturnsValidImmediately()
  {
    var services = BuildHost(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
    });

    var service = services.GetRequiredService<IFlowthruService>();
    var result = await service.ValidateRegistrationAsync();
    Assert.That(result, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>());
  }

  [Test]
  public async Task ValidateRegistrationAsync_AllHooksSucceed_ReturnsValid()
  {
    var services = BuildHost(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.RegisterValidationHook("hook-a", _ => FlowIO.Pure(
        Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default)
      ));
      b.RegisterValidationHook("hook-b", _ => FlowIO.Pure(
        Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default)
      ));
    });

    var service = services.GetRequiredService<IFlowthruService>();
    var result = await service.ValidateRegistrationAsync();
    Assert.That(result, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>());
  }

  [Test]
  public async Task ValidateRegistrationAsync_OneHookFails_ReturnsInvalidWithHookId()
  {
    var services = BuildHost(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.RegisterValidationHook("hook-a", _ => FlowIO.Pure(
        Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default)
      ));
      b.RegisterValidationHook("hook-b", _ => FlowIO.Pure(
        Validated<PreFlightError, FlowUnit>.Fail(new PreFlightError.RegistrationCheckFailed(
          HookId: "hook-b",
          CheckMessage: "intentional failure"
        ))
      ));
    });

    var service = services.GetRequiredService<IFlowthruService>();
    var result = await service.ValidateRegistrationAsync();
    Assert.That(result, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());

    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)result;
    Assert.That(invalid.Errors, Has.Count.EqualTo(1));
    var failure = (PreFlightError.RegistrationCheckFailed)invalid.Errors[0];
    Assert.That(failure.HookId, Is.EqualTo("hook-b"),
      "Failure should be attributable to the hook that produced it.");
  }

  [Test]
  public async Task ValidateRegistrationAsync_MultipleHooksFail_AggregatesAllErrors()
  {
    var services = BuildHost(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.RegisterValidationHook("hook-a", _ => FlowIO.Pure(
        Validated<PreFlightError, FlowUnit>.Fail(new PreFlightError.RegistrationCheckFailed(
          HookId: "hook-a", CheckMessage: "a failed"
        ))
      ));
      b.RegisterValidationHook("hook-b", _ => FlowIO.Pure(
        Validated<PreFlightError, FlowUnit>.Fail(new PreFlightError.RegistrationCheckFailed(
          HookId: "hook-b", CheckMessage: "b failed"
        ))
      ));
    });

    var service = services.GetRequiredService<IFlowthruService>();
    var result = await service.ValidateRegistrationAsync();
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)result;
    Assert.That(invalid.Errors, Has.Count.EqualTo(2),
      "All hook failures should aggregate into a single Invalid result.");
  }

  // ── Caching ─────────────────────────────────────────────────────────

  [Test]
  public async Task ValidateRegistrationAsync_SuccessIsCached_HookOnlyRunsOnce()
  {
    var hookCount = 0;
    var services = BuildHost(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.RegisterValidationHook("counted-hook", _ =>
      {
        Interlocked.Increment(ref hookCount);
        return FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default));
      });
    });

    var service = services.GetRequiredService<IFlowthruService>();
    await service.ValidateRegistrationAsync();
    await service.ValidateRegistrationAsync();
    await service.ValidateRegistrationAsync();

    Assert.That(hookCount, Is.EqualTo(1),
      "Successful registration validation should be cached — re-running is a no-op.");
  }

  [Test]
  public async Task ValidateRegistrationAsync_FailureIsNotCached_HookRunsEveryCall()
  {
    var hookCount = 0;
    var services = BuildHost(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.RegisterValidationHook("counted-hook", _ =>
      {
        Interlocked.Increment(ref hookCount);
        return FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Fail(
          new PreFlightError.RegistrationCheckFailed("counted-hook", "always fails")
        ));
      });
    });

    var service = services.GetRequiredService<IFlowthruService>();
    await service.ValidateRegistrationAsync();
    await service.ValidateRegistrationAsync();
    await service.ValidateRegistrationAsync();

    Assert.That(hookCount, Is.EqualTo(3),
      "Failed hooks must re-run on every call so transient failures clear without a process restart.");
  }

  // ── Auto-invocation from RunAsync ───────────────────────────────────

  [Test]
  public async Task RunAsync_RegistrationFailure_BlocksFlowExecution()
  {
    var stepRan = false;
    var services = BuildHost(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("blocked", () =>
        FlowBuilder.CreateFlow("blocked", p =>
          p.AddStep("never", () => { stepRan = true; })
        )
      );
      b.RegisterValidationHook("blocker", _ => FlowIO.Pure(
        Validated<PreFlightError, FlowUnit>.Fail(new PreFlightError.RegistrationCheckFailed(
          HookId: "blocker", CheckMessage: "host is misconfigured"
        ))
      ));
    });

    var service = services.GetRequiredService<IFlowthruService>();
    // Shallow: registration hooks default to MinimumDepth = Shallow, so this
    // is the lightest depth at which the blocker hook participates. (None
    // skips registration entirely; Hermetic runs only zero-I/O wiring hooks.)
    var result = await service.RunAsync(
      "blocked",
      new ExecutionOptions { ValidationDepth = ValidationDepth.Shallow }
    );

    Assert.That(result.HasFailures, Is.True);
    Assert.That(stepRan, Is.False,
      "Registration validation must run before any step executes.");

    var failed = (StepResult.Failed)result.StepResults[0];
    Assert.That(failed.StepLabel, Is.EqualTo("preflight:registration:blocker"),
      "Synthetic step result should be labelled preflight:registration:<hookId>.");
    Assert.That(failed.Error, Is.InstanceOf<RuntimeError.PreFlightFailed>(),
      "Registration failures are legitimate user-actionable pre-flight errors — they "
        + "surface via PreFlightFailed so the classifier yields FT3006, not FT4004.");
  }

  // ── Hook implementation throwing ────────────────────────────────────

  [Test]
  public async Task ValidateRegistrationAsync_HookEffectFailure_SurfacesAsRegistrationCheckFailed()
  {
    // A hook implementation that returns a FlowIO failure (rather than
    // a Validated.Invalid) — the service should translate the FlowIO
    // failure into a typed RegistrationCheckFailed for that hook.
    var services = BuildHost(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.RegisterValidationHook("misbehaving", _ =>
        FlowIO.Fail<Validated<PreFlightError, FlowUnit>>(
          new Flowthru.Validation.Runtime.RuntimeError.External(
            "misbehaving-hook",
            new InvalidOperationException("the hook itself crashed")
          )
        )
      );
    });

    var service = services.GetRequiredService<IFlowthruService>();
    var result = await service.ValidateRegistrationAsync();
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)result;
    Assert.That(invalid.Errors, Has.Count.EqualTo(1));
    var failure = (PreFlightError.RegistrationCheckFailed)invalid.Errors[0];
    Assert.That(failure.HookId, Is.EqualTo("misbehaving"),
      "A FlowIO-failing hook should be attributed to its own hook id.");
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static IServiceProvider BuildHost(Action<IFlowthruBuilder> configure)
  {
    var services = new ServiceCollection();
    services.AddFlowthru(configure);
    return services.BuildServiceProvider();
  }
}
