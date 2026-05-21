using Flowthru.Prelude;

namespace Flowthru.Tests.Kits.Prelude;

/// <summary>
/// Backend abstraction for <see cref="EphemeralResourceLaws{TBackend, TScope}"/>.
/// Extends <see cref="IResourceBackend{TScope}"/> with hooks specific to
/// resources that <em>create and drop external state</em> — databases,
/// schemas, temp directories, etc.
/// </summary>
/// <remarks>
/// <para>
/// These backends expose two
/// <see cref="IResourceBackend{TScope}.CreateResource"/> forms — default
/// and preserve-on-failure — so the laws can verify the framework's
/// debugging-retain semantic. They also seed leftover state (to verify
/// idempotent acquire) and create independent peer state (to verify
/// isolation on release).
/// </para>
/// <para>
/// Inherits the constructor and re-entrancy contracts from
/// <see cref="IResourceBackend{TScope}"/>: cheap constructor, expensive
/// setup gated by capability checks and amortised across the fixture,
/// per-call disjoint state with a unique
/// <see cref="IResourceBackend{TScope}.ExternalStateIdentifier"/>.
/// </para>
/// </remarks>
public interface IEphemeralResourceBackend<TScope> : IResourceBackend<TScope>
{
  /// <summary>
  /// Build a fresh resource with the <c>PreserveOnFailure</c> behaviour
  /// requested. Default <see cref="IResourceBackend{TScope}.CreateResource"/>
  /// implementations should pass <c>false</c>.
  /// </summary>
  FlowResource<TScope> CreateResource(bool preserveOnFailure);

  /// <inheritdoc />
  FlowResource<TScope> IResourceBackend<TScope>.CreateResource() =>
    CreateResource(preserveOnFailure: false);

  /// <summary>
  /// Pre-populate the external state to simulate a leftover from a prior
  /// preserved-on-failure run. The conformance suite then runs acquire and
  /// asserts the leftover was wiped.
  /// </summary>
  Task SeedLeftoverState();

  /// <summary>
  /// Set up a peer external state (e.g., a different database, a different
  /// schema, a sibling directory) and return a probe that the conformance
  /// suite uses to assert the peer was untouched by acquire/release.
  /// Returns <c>null</c> when peer isolation is not meaningful for this
  /// backend (e.g., a global-singleton resource).
  /// </summary>
  Task<IPeerStateProbe?> CreatePeerState();
}

/// <summary>
/// Probe for verifying that resource acquire/release leaves peer state
/// untouched. Implementations dispose any peer state they created.
/// </summary>
public interface IPeerStateProbe : IAsyncDisposable
{
  /// <summary>Returns <c>true</c> if the peer state still exists.</summary>
  Task<bool> StillExists();
}
