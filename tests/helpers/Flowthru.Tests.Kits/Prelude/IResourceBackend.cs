using Flowthru.Prelude;

namespace Flowthru.Tests.Kits.Prelude;

/// <summary>
/// Backend abstraction for <see cref="FlowResourceLaws{TBackend, TScope}"/>.
/// Implementors build fresh <see cref="FlowResource{TScope}"/> values per
/// test and expose hooks the kit uses to observe lifecycle behaviour.
/// </summary>
/// <remarks>
/// <para>
/// The kit is provider-agnostic: a backend can target a real database
/// (SQLite, PostgreSQL), a filesystem area (temp directory), or an
/// in-memory shim that records calls. The laws verify the bracket
/// contract regardless of what's behind it.
/// </para>
/// <para>
/// <strong>Constructor contract.</strong> Each fixture instantiates a new
/// backend via <c>new TBackend()</c>. Constructors must be cheap and
/// configuration-only: no Docker probe, no container start, no DB
/// connection. Capability checks happen via
/// <see cref="RequiredCapabilities"/>, not in the constructor — so a
/// fixture with a missing dependency goes <em>Inconclusive</em> before
/// any expensive setup runs. Expensive shared setup belongs in a
/// <see cref="Lazy{T}"/> field touched on first
/// <see cref="CreateResource"/>, or in a fixture-scoped init the laws
/// kit invokes after the capability gate.
/// </para>
/// <para>
/// <strong>Re-entrancy contract.</strong> A single backend instance lives
/// for a whole fixture (one per <c>[TestFixture(typeof(TBackend))]</c>
/// binding). <see cref="CreateResource"/> is invoked per test, and the
/// <see cref="FlowResource{TScope}"/> it returns must own
/// <em>disjoint external state</em> — fresh database name, temp file,
/// schema, etc. — so tests never observe each other's effects.
/// Backends are required to be safe to call from multiple test threads
/// concurrently; the
/// <c>ConcurrentCreateResourceProducesDisjointStateLaw</c> in
/// <see cref="FlowResourceLaws{TBackend, TScope}"/> enforces this
/// against every backend by spawning N concurrent
/// <see cref="CreateResource"/> calls and asserting their
/// <see cref="ExternalStateIdentifier"/>s are unique.
/// </para>
/// </remarks>
/// <typeparam name="TScope">
/// The scope value the resource produces on acquire. Carries any handle
/// the release callback needs.
/// </typeparam>
public interface IResourceBackend<TScope>
{
  /// <summary>
  /// Build a fresh <see cref="FlowResource{TScope}"/> for one test
  /// case. Must return a resource whose external state is disjoint from
  /// every prior and concurrent call (see re-entrancy contract in the
  /// type-level remarks).
  /// </summary>
  FlowResource<TScope> CreateResource();

  /// <summary>
  /// Probe whether the external state managed by the resource currently
  /// exists (e.g., the file is present, the schema is created, the
  /// token is valid). Used by the kit to verify acquire/release
  /// effects.
  /// </summary>
  Task<bool> ResourceExists();

  /// <summary>
  /// Optional async initialisation hook. The laws kit invokes this
  /// from <c>OneTimeSetUp</c> after the
  /// <see cref="RequiredCapabilities"/> gate has cleared, before any
  /// <see cref="CreateResource"/> call. Use this for expensive shared
  /// setup that needs an async context — starting a Testcontainers
  /// instance, opening a connection pool, pulling fixtures from a
  /// remote. Backends with no expensive shared setup leave the
  /// default no-op.
  /// </summary>
  Task InitializeAsync() => Task.CompletedTask;

  /// <summary>
  /// Optional cleanup hook called from <c>OneTimeTearDown</c>.
  /// Implementations drop any external state created during the
  /// fixture — containers, files, server-side schemas — including
  /// state from resources that were not explicitly released by a
  /// test's body path.
  /// </summary>
  Task Cleanup() => Task.CompletedTask;

  /// <summary>
  /// Capabilities this backend depends on (Docker, SPARK_HOME, etc.).
  /// The kit's <c>OneTimeSetUp</c> runs
  /// <see cref="TestCapability.IsAvailable"/> over this list via
  /// <c>Assume.That</c> before any <see cref="CreateResource"/> call —
  /// missing capabilities yield an Inconclusive fixture rather than a
  /// failure. Defaults to empty for backends with no external
  /// dependencies.
  /// </summary>
  IReadOnlyList<TestCapability> RequiredCapabilities => [];

  /// <summary>
  /// Identifier of the external state this <paramref name="scope"/>
  /// owns — the DB name, file path, schema name, container ID, etc.
  /// Used by the
  /// <c>ConcurrentCreateResourceProducesDisjointStateLaw</c> to verify
  /// that concurrent <see cref="CreateResource"/> calls produce
  /// non-overlapping external state.
  /// </summary>
  /// <remarks>
  /// Must be deterministic for a given <paramref name="scope"/> and
  /// unique per isolated resource the backend creates. For an in-memory
  /// shim with no meaningful external identity, returning the scope's
  /// GUID or a monotonically-incremented per-instance counter (protected
  /// by an <see cref="Interlocked"/> increment) is sufficient.
  /// </remarks>
  string ExternalStateIdentifier(TScope scope);
}
