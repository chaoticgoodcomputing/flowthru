namespace Flowthru.Prelude;

/// <summary>
/// A pair of effects representing a managed resource — an
/// <em>acquire</em> step that produces a scope value, and a <em>release</em>
/// step that disposes it. Modelled on Haskell's <c>bracket</c> /
/// cats-effect's <c>Resource</c>: the framework runs <em>acquire</em>
/// before flow execution and guarantees <em>release</em> runs on every
/// exit path, including failure and cancellation.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why pair, not two unrelated effects.</strong> Bundling acquire
/// and release into one value keeps cleanup logically attached to setup. A
/// catalog declaring a <see cref="FlowResource{TScope}"/> cannot
/// accidentally publish an acquire without a corresponding release; the
/// framework cannot run one without preparing the other.
/// </para>
/// <para>
/// <strong>Release receives the body's error.</strong> The release closure
/// is invoked with the primary <see cref="RuntimeError"/> that caused the
/// body to abort, or <c>null</c> on success. This lets release implement
/// policies like <c>PreserveOnFailure</c> without requiring framework-level
/// configuration.
/// </para>
/// <para>
/// <strong>Failures are values.</strong> Unlike a try/catch-driven shape,
/// the release closure receives a <see cref="RuntimeError"/>, matching the
/// rest of the FP runtime. Failures flow through the FlowIO sum; nothing
/// throws.
/// </para>
/// </remarks>
/// <typeparam name="TScope">
/// The scope value produced by acquire and consumed by release. Carries any
/// state needed to clean up (connection handles, paths, transactions).
/// </typeparam>
public sealed class FlowResource<TScope> : IFlowResource
{
  private readonly FlowIO<TScope> _acquire;
  private readonly Func<TScope, RuntimeError?, FlowIO<FlowUnit>> _release;

  internal FlowResource(
    FlowIO<TScope> acquire,
    Func<TScope, RuntimeError?, FlowIO<FlowUnit>> release
  )
  {
    _acquire = acquire;
    _release = release;
  }

  /// <summary>Effect that acquires the resource, producing the scope value.</summary>
  public FlowIO<TScope> Acquire => _acquire;

  /// <summary>
  /// Effect that releases a previously acquired scope. Receives the body's
  /// primary <see cref="RuntimeError"/> (or <c>null</c> on success) so the
  /// closure can inspect it for policy decisions.
  /// </summary>
  public Func<TScope, RuntimeError?, FlowIO<FlowUnit>> Release => _release;

  /// <summary>
  /// Acquires the resource, runs <paramref name="body"/>, and guarantees
  /// release on exit. Outcomes:
  /// <list type="bullet">
  ///   <item>Acquire fails → returned effect fails with the acquire error.</item>
  ///   <item>Body succeeds, release succeeds → returns body value.</item>
  ///   <item>Body succeeds, release fails → returned effect fails with the release error.</item>
  ///   <item>Body fails (regardless of release) → returned effect fails with the body error; release errors are suppressed.</item>
  /// </list>
  /// </summary>
  public FlowIO<TResult> Use<TResult>(Func<TScope, FlowIO<TResult>> body) =>
    Acquire.Bind(scope =>
      body(scope)
        .Catch(bodyError =>
          // Body failed: run release with the body error, suppress any
          // release error, propagate the body error.
          _release(scope, bodyError)
            .Catch(_ => FlowIO<FlowUnit>.Pure(FlowUnit.Default))
            .Bind<TResult>(_ => FlowIO<TResult>.Fail(bodyError))
        )
        .Bind(bodyValue =>
          // Body succeeded: run release; release outcome wins. If release
          // fails, the failure propagates via Map's success-only behavior.
          _release(scope, null).Map(_ => bodyValue)
        )
    );

  // ── IFlowResource (type-erased framework view) ─────────────────────────

  FlowIO<object?> IFlowResource.AcquireUntyped() => _acquire.Map(s => (object?)s);

  FlowIO<FlowUnit> IFlowResource.ReleaseUntyped(object? scope, RuntimeError? bodyError) =>
    _release((TScope)scope!, bodyError);
}

/// <summary>Factory and combinators for <see cref="FlowResource{TScope}"/>.</summary>
public static class FlowResource
{
  /// <summary>
  /// A resource that acquires nothing and releases nothing. Used as the
  /// default catalog override when no resource is needed.
  /// </summary>
  public static FlowResource<FlowUnit> Empty { get; } =
    new(
      FlowIO<FlowUnit>.Pure(FlowUnit.Default),
      (_, _) => FlowIO<FlowUnit>.Pure(FlowUnit.Default)
    );

  /// <summary>Builds a resource from explicit acquire and release effects.</summary>
  public static FlowResource<TScope> Make<TScope>(
    FlowIO<TScope> acquire,
    Func<TScope, RuntimeError?, FlowIO<FlowUnit>> release
  ) => new(acquire, release);

  /// <summary>
  /// Builds a resource that returns a precomputed value with no release work.
  /// </summary>
  public static FlowResource<TScope> Pure<TScope>(TScope value) =>
    new(
      FlowIO<TScope>.Pure(value),
      (_, _) => FlowIO<FlowUnit>.Pure(FlowUnit.Default)
    );
}
