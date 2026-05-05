using Flowthru.Core.Effects;

namespace Flowthru.Tests.Kits.Effects;

/// <summary>
/// Backend abstraction for <see cref="FlowResourceConformance{TBackend, TScope}"/>.
/// Implementors construct a fresh <see cref="FlowResource{TScope}"/> per test
/// and expose hooks the kit uses to observe lifecycle behaviour.
/// </summary>
/// <remarks>
/// <para>
/// The kit is provider-agnostic: a backend can target a real database
/// (SQLite, PostgreSQL), a filesystem area (temp directory), or an in-memory
/// shim that records calls. The conformance suite verifies the bracket
/// contract regardless of what's behind it.
/// </para>
/// <para>
/// Each kit test instantiates a new backend via <c>new TBackend()</c> in
/// <c>SetUp</c>, so implementations should be cheap to construct and
/// independent across instances.
/// </para>
/// </remarks>
/// <typeparam name="TScope">
/// The scope value the resource produces on acquire. Carries any handle the
/// release callback needs.
/// </typeparam>
public interface IResourceBackend<TScope>
{
  /// <summary>
  /// Build a fresh <see cref="FlowResource{TScope}"/> for one test case.
  /// </summary>
  FlowResource<TScope> CreateResource();

  /// <summary>
  /// Probe whether the external state managed by the resource currently
  /// exists (e.g., the file is present, the schema is created, the token
  /// is valid). Used by the kit to verify acquire/release effects.
  /// </summary>
  Task<bool> ResourceExists();

  /// <summary>
  /// Optional cleanup hook called from <c>TearDown</c>. Implementations
  /// should drop any external state created during the test, even if a
  /// release path was not exercised.
  /// </summary>
  Task Cleanup() => Task.CompletedTask;
}
