using Flowthru.Step.DuckDb;

namespace Flowthru.Validation.Runtime.DuckDb;

/// <summary>
/// Declares the <see cref="ServiceProfile"/> of the shared
/// <see cref="IDuckDbEngine"/> — the conflict resource every DuckDB
/// transform step depends on (see
/// <c>DuckDbTransformStep&lt;&gt;.EngineDependency</c>). Each transform
/// may use the engine's full memory/disk budget, so this contributor
/// reports the engine's <see cref="IDuckDbEngine.MaxConcurrency"/> as
/// the engine key's capacity, and the <c>ParallelFlowScheduler</c>
/// gates accordingly.
/// </summary>
/// <remarks>
/// <para>
/// Registered by <c>UseDuckDb()</c> and aggregated by Core's
/// <c>CompositeServiceProfileProvider</c> alongside every other
/// extension's contributor. It speaks only for the
/// <see cref="IDuckDbEngine"/> dependency and returns <c>null</c> for
/// everything else.
/// </para>
/// <para>
/// <see cref="ServiceProfile.AffectsOutputs"/> is <c>false</c>: the
/// engine's runtime identity adds no caching information — a
/// transform's determinism lives in its SQL text, engine version, and
/// inputs, not in which engine instance ran it. Those first two enter
/// the cache key through the step's declared cache identity
/// (<c>DuckDbTransformStep.DeclaredCacheIdentity</c>); keeping the
/// engine dependency cache-neutral is what lets the step be cacheable
/// at all.
/// </para>
/// <para>
/// Capacity is read from the resolved engine rather than hardcoded:
/// the conservative serial floor is the options default, but a host
/// with memory to spare raises
/// <c>DuckDbEngineOptions.MaxConcurrentTransforms</c> and the scheduler
/// lets its transforms overlap. Hardcoding <c>1</c> here would pin
/// every engine to serial, since the composite provider's meet can
/// only lower a capacity, never raise it.
/// </para>
/// </remarks>
internal sealed class DuckDbEngineProfileContributor : IServiceProfileContributor
{
  private readonly IDuckDbEngine _engine;

  public DuckDbEngineProfileContributor(IDuckDbEngine engine) =>
    _engine = engine ?? throw new ArgumentNullException(nameof(engine));

  /// <inheritdoc/>
  public ServiceProfile? Contribute(ServiceDependency dependency) =>
    dependency is ServiceDependency.CSharp cs && cs.ServiceType == typeof(IDuckDbEngine)
      ? new ServiceProfile
        {
          Capacity = _engine.MaxConcurrency,
          AffectsOutputs = false,
        }
      : null;
}
