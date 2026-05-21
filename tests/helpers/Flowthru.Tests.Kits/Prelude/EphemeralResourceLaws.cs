using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Tests.Kits.Prelude;

/// <summary>
/// Laws every <see cref="IEphemeralResourceBackend{TScope}"/> implementer
/// must satisfy <em>in addition to</em> the base
/// <see cref="FlowResourceLaws{TBackend, TScope}"/>. Covers behaviours
/// specific to resources that create and drop external state — peer
/// isolation, idempotent acquire over leftover state, and the
/// preserve-on-failure semantic.
/// </summary>
/// <typeparam name="TBackend">Concrete ephemeral backend under test.</typeparam>
/// <typeparam name="TScope">Scope type the backend produces.</typeparam>
public abstract class EphemeralResourceLaws<TBackend, TScope>
  : FlowResourceLaws<TBackend, TScope>
  where TBackend : IEphemeralResourceBackend<TScope>, new()
{
  /// <summary>
  /// Release on a successful run drops the resource's external state
  /// but leaves untouched any peer state (a sibling database, a
  /// different schema, a sibling directory) the backend can create.
  /// Skipped for backends that report no peer concept by returning
  /// <c>null</c> from
  /// <see cref="IEphemeralResourceBackend{TScope}.CreatePeerState"/>.
  /// </summary>
  [Test]
  public async Task ReleasePreservesPeerStateLaw()
  {
    await using var peer = await Backend.CreatePeerState();
    if (peer is null)
    {
      Assert.Pass("Backend reports no meaningful peer-state concept.");
    }

    var resource = Backend.CreateResource(preserveOnFailure: false);
    var result = await resource.Use(scope => FlowIO<TScope>.Pure(scope)).Run();

    Assert.That(result, Is.InstanceOf<EffResult<TScope>.Success>());
    Assert.That(await peer!.StillExists(), Is.True,
      "Release should leave peer state untouched.");
  }

  /// <summary>
  /// <c>PreserveOnFailure</c> keeps external state alive when the body
  /// errors, so a developer can inspect it post-mortem. The default
  /// release path (already covered in
  /// <see cref="FlowResourceLaws{TBackend, TScope}.BodyFailurePropagatesAndStillReleasesLaw"/>)
  /// drops state on failure; this law verifies the opt-in inversion.
  /// </summary>
  [Test]
  public async Task PreserveOnFailureRetainsStateLaw()
  {
    var resource = Backend.CreateResource(preserveOnFailure: true);
    var sentinel = new InvalidOperationException("intentional body failure");

    var result = await resource.Use<TScope>(_ =>
      FlowIO.Fail<TScope>(new RuntimeError.External(
        nameof(PreserveOnFailureRetainsStateLaw),
        sentinel
      ))
    ).Run();

    Assert.That(result, Is.InstanceOf<EffResult<TScope>.Failure>(),
      "Body failure should propagate.");
    Assert.That(await Backend.ResourceExists(), Is.True,
      "preserveOnFailure=true should retain external state for post-mortem inspection.");

    // The preserved state lingers until OneTimeTearDown — that's the
    // semantic point. Subsequent tests call CreateResource() and update
    // the backend's tracked state to a fresh identifier, so they don't
    // observe this leftover. (Calling Backend.Cleanup() here would tear
    // down shared resources like the Postgres container, breaking
    // every subsequent test in the fixture.)
  }

  /// <summary>
  /// Acquire wipes leftover state from a previous preserved-on-failure
  /// run. The idempotency guarantee: a fresh acquire never trips over
  /// detritus from earlier runs.
  /// </summary>
  [Test]
  public async Task AcquireWipesLeftoverStateLaw()
  {
    // Pre-populate external state to simulate a leftover.
    await Backend.SeedLeftoverState();
    Assume.That(await Backend.ResourceExists(), Is.True,
      "Precondition: leftover state should be present after SeedLeftoverState().");

    // Acquire and release — should wipe the leftover and reset cleanly.
    var resource = Backend.CreateResource(preserveOnFailure: false);
    var result = await resource.Use(scope => FlowIO<TScope>.Pure(scope)).Run();

    Assert.That(result, Is.InstanceOf<EffResult<TScope>.Success>(),
      $"Body should succeed even with leftover state present at entry. Got: {result}");
    Assert.That(await Backend.ResourceExists(), Is.False,
      "Default release should drop the (now-replaced) state.");
  }
}
