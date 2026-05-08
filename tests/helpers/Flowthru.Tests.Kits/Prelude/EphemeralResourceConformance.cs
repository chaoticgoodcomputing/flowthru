using Flowthru.Core.Effects;

namespace Flowthru.Tests.Kits.Prelude;

/// <summary>
/// Conformance suite for any <see cref="FlowResource{TScope}"/> publisher
/// that creates and drops <em>external state</em> — databases, schemas,
/// temp directories, etc. Layers on top of
/// <see cref="FlowResourceConformance{TBackend, TScope}"/> with scenarios
/// specific to ephemeral lifecycle: state is created on acquire, dropped on
/// release, retained when <c>PreserveOnFailure</c> kicks in, and idempotent
/// against leftover state from prior runs.
/// </summary>
public abstract class EphemeralResourceConformance<TBackend, TScope>
  : FlowResourceConformance<TBackend, TScope>
  where TBackend : IEphemeralResourceBackend<TScope>, new()
{
  // ── State materialization ───────────────────────────────────────────────

  [Test]
  public async Task Acquire_StateDidNotExist_NowExists()
  {
    Assert.That(
      await Backend.ResourceExists(),
      Is.False,
      "Pre-condition: no resource state should exist before acquire."
    );

    var resource = Backend.CreateResource();
    await resource.Acquire.Run();

    Assert.That(
      await Backend.ResourceExists(),
      Is.True,
      "Acquire should have created the resource's external state."
    );
  }

  // ── Default release semantics ──────────────────────────────────────────

  [Test]
  public async Task Use_BodySucceeds_ReleaseDropsState()
  {
    var resource = Backend.CreateResource(preserveOnFailure: false);

    await resource.Use(_ => FlowIO.Pure(FlowUnit.Default)).Run();

    Assert.That(
      await Backend.ResourceExists(),
      Is.False,
      "Default release should drop the state on body success."
    );
  }

  [Test]
  public void Use_BodyThrows_DefaultRelease_DropsState()
  {
    var resource = Backend.CreateResource(preserveOnFailure: false);

    Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await resource
        .Use<FlowUnit>(_ => FlowIO.Fail<FlowUnit>(new InvalidOperationException("body-failed")))
        .Run()
    );

    Assert.ThatAsync(
      Backend.ResourceExists,
      Is.False.After(0).MilliSeconds,
      "Default release should drop the state even on body failure."
    );
  }

  // ── PreserveOnFailure ──────────────────────────────────────────────────

  [Test]
  public void Use_BodyThrows_PreserveOnFailure_RetainsState()
  {
    var resource = Backend.CreateResource(preserveOnFailure: true);

    Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await resource
        .Use<FlowUnit>(_ => FlowIO.Fail<FlowUnit>(new InvalidOperationException("body-failed")))
        .Run()
    );

    Assert.ThatAsync(
      Backend.ResourceExists,
      Is.True.After(0).MilliSeconds,
      "PreserveOnFailure should retain the state when the body throws."
    );
  }

  [Test]
  public async Task Use_BodySucceeds_PreserveOnFailure_StillDropsState()
  {
    var resource = Backend.CreateResource(preserveOnFailure: true);

    await resource.Use(_ => FlowIO.Pure(FlowUnit.Default)).Run();

    Assert.That(
      await Backend.ResourceExists(),
      Is.False,
      "PreserveOnFailure only matters when the body throws — successful runs always drop."
    );
  }

  // ── Idempotent acquire ─────────────────────────────────────────────────

  [Test]
  public async Task Acquire_LeftoverState_IdempotentlyWiped()
  {
    await Backend.SeedLeftoverState();
    Assert.That(
      await Backend.ResourceExists(),
      Is.True,
      "Pre-condition: leftover state should be present."
    );

    var resource = Backend.CreateResource();
    await resource.Acquire.Run();

    Assert.That(
      await Backend.ResourceExists(),
      Is.True,
      "Resource state should be present after acquire (rebuilt fresh)."
    );

    // Round-trip: cleanup so the assertion below isn't poisoned by prior state.
    var scope = await resource.Acquire.Run();
    await resource.Release(scope, null).Run();
  }

  // ── Peer isolation ─────────────────────────────────────────────────────

  [Test]
  public async Task Release_PeerStateUntouched()
  {
    await using var peer = await Backend.CreatePeerState();
    if (peer is null)
    {
      Assert.Ignore("Backend does not model peer state isolation.");
      return;
    }

    Assert.That(await peer.StillExists(), Is.True, "Pre-condition: peer state set up.");

    var resource = Backend.CreateResource();
    await resource.Use(_ => FlowIO.Pure(FlowUnit.Default)).Run();

    Assert.That(
      await peer.StillExists(),
      Is.True,
      "Release should not touch peer state."
    );
  }
}
