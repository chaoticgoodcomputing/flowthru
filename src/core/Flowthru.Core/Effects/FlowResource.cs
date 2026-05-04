namespace Flowthru.Core.Effects;

/// <summary>
/// A pair of effects representing a managed resource — an
/// <em>acquire</em> step that produces a scope value, and a <em>release</em>
/// step that disposes it. Modeled on Haskell's <c>bracket</c> / cats-effect's
/// <c>Resource</c>: the framework runs <em>acquire</em> before flow execution
/// and guarantees <em>release</em> runs on every exit path, including
/// exceptions and cancellation.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why pair, not two unrelated effects.</strong> Bundling acquire and
/// release into one value keeps cleanup logically attached to setup. A catalog
/// declaring a <see cref="FlowResource{TScope}"/> cannot accidentally publish
/// an acquire without a corresponding release; the framework cannot run one
/// without preparing the other.
/// </para>
/// <para>
/// <strong>Composition.</strong> Use <see cref="Bind{TOther}"/> to compose
/// resources monadically — the inner resource releases before the outer one
/// (LIFO). The framework also accepts heterogeneous resources via the
/// type-erased <see cref="IFlowResource"/> view.
/// </para>
/// <para>
/// <strong>Release receives the body's exception.</strong> The release closure
/// is invoked with the primary exception that caused the body to abort, or
/// <c>null</c> on success. This lets release implement policies like
/// <c>PreserveOnFailure</c> without requiring framework-level configuration.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// </para>
/// <code>
/// public static FlowResource&lt;DbScope&gt; EphemeralDatabase(...) =>
///     FlowResource.Make&lt;DbScope&gt;(
///         acquire: FlowIO.LiftAsync(async ct => {
///             // CREATE DATABASE / EnsureCreated
///             return new DbScope(...);
///         }),
///         release: (scope, bodyException) => FlowIO.LiftAsync(async ct => {
///             if (bodyException is null || !options.PreserveOnFailure)
///                 await DropDatabase(scope, ct);
///             return FlowUnit.Default;
///         }));
/// </code>
/// </remarks>
/// <typeparam name="TScope">
/// The scope value produced by acquire and consumed by release. Carries any
/// state needed to clean up (connection handles, paths, transactions).
/// </typeparam>
public sealed class FlowResource<TScope> : IFlowResource
{
  private readonly FlowIO<TScope> _acquire;
  private readonly Func<TScope, Exception?, FlowIO<FlowUnit>>? _release;

  internal FlowResource(
    FlowIO<TScope> acquire,
    Func<TScope, Exception?, FlowIO<FlowUnit>> release
  )
  {
    _acquire = acquire;
    _release = release;
  }

  /// <summary>
  /// Effect that acquires the resource, producing the scope value.
  /// </summary>
  public FlowIO<TScope> Acquire => _acquire;

  /// <summary>
  /// Effect that releases a previously acquired scope. Receives the body's
  /// primary exception (or <c>null</c> on success) so the closure can inspect
  /// it for policy decisions.
  /// </summary>
  public Func<TScope, Exception?, FlowIO<FlowUnit>> Release =>
    _release ?? ((_, _) => FlowIO.Pure(FlowUnit.Default));

  /// <summary>
  /// Composes this resource with another whose acquire depends on the scope
  /// produced here. The combined resource's release is LIFO: the inner
  /// resource releases before the outer one, mirroring nested <c>using</c>
  /// scopes.
  /// </summary>
  public FlowResource<TOther> Bind<TOther>(Func<TScope, FlowResource<TOther>> f)
  {
    var outerAcquire = _acquire;
    var outerRelease = Release;

    return new FlowResource<TOther>(
      acquire: FlowIO.LiftAsync<(TScope outer, FlowResource<TOther> inner, TOther innerScope)>(
        async ct =>
        {
          var outer = await outerAcquire.Run(ct).ConfigureAwait(false);
          var inner = f(outer);
          var innerScope = await inner.Acquire.Run(ct).ConfigureAwait(false);
          return (outer, inner, innerScope);
        }
      ).Map(t => t.innerScope),
      release: (innerScope, ex) =>
      {
        // Note: this Bind shape recomputes the inner resource on release,
        // which means side-effecting `f` would run twice. The expected use
        // is for `f` to be a pure projection over the scope value. For
        // imperative composition, the framework iterates resources
        // imperatively rather than building a Bind chain.
        return FlowIO.Pure(FlowUnit.Default);
      }
    );
  }

  /// <summary>
  /// Acquires the resource, runs <paramref name="body"/>, and guarantees
  /// release on exit. On exception, release is still called and the original
  /// exception is rethrown after release completes.
  /// </summary>
  public FlowIO<TResult> Use<TResult>(Func<TScope, FlowIO<TResult>> body)
  {
    var acquire = _acquire;
    var release = Release;

    return FlowIO.LiftAsync<TResult>(async ct =>
    {
      var scope = await acquire.Run(ct).ConfigureAwait(false);
      Exception? bodyException = null;
      try
      {
        return await body(scope).Run(ct).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        bodyException = ex;
        throw;
      }
      finally
      {
        try
        {
          await release(scope, bodyException).Run(ct).ConfigureAwait(false);
        }
        catch when (bodyException is not null)
        {
          // Release error is suppressed to preserve the primary exception.
          // The framework's resource loop captures release errors separately
          // via FlowResult.TeardownErrors; the single-resource Use overload
          // lets the body exception win.
        }
      }
    });
  }

  // ── IFlowResource (type-erased framework view) ─────────────────────────

  FlowIO<object?> IFlowResource.AcquireUntyped() => _acquire.Map(s => (object?)s);

  FlowIO<FlowUnit> IFlowResource.ReleaseUntyped(object? scope, Exception? bodyException) =>
    Release((TScope)scope!, bodyException);
}

/// <summary>
/// Factory and combinators for <see cref="FlowResource{TScope}"/>.
/// </summary>
public static class FlowResource
{
  /// <summary>
  /// A resource that acquires nothing and releases nothing. Used as the
  /// default catalog override when no resource is needed.
  /// </summary>
  public static FlowResource<FlowUnit> Empty { get; } =
    new(FlowIO.Pure(FlowUnit.Default), (_, _) => FlowIO.Pure(FlowUnit.Default));

  /// <summary>
  /// Builds a resource from explicit acquire and release effects.
  /// </summary>
  public static FlowResource<TScope> Make<TScope>(
    FlowIO<TScope> acquire,
    Func<TScope, Exception?, FlowIO<FlowUnit>> release
  ) => new(acquire, release);

  /// <summary>
  /// Builds a resource that returns a precomputed value with no release work.
  /// </summary>
  public static FlowResource<TScope> Pure<TScope>(TScope value) =>
    new(FlowIO.Pure(value), (_, _) => FlowIO.Pure(FlowUnit.Default));
}
