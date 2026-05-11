// Derived from LanguageExt v5 (https://github.com/louthy/language-ext) by Paul Louth.
// Copyright (c) 2014-2025 Paul Louth. MIT License — see ../LICENSE-LanguageExt.md.
// Simplified for Flowthru:
//   - Failure type is RuntimeError (no generic E parameter; no Error abstraction).
//   - No TRuntime / Has<> capability environment — services enter via closure
//     capture in catalog factories and step factories (see fp-rewrite-spec §2.6).
//   - No HKT (K<F, A>) — FlowIO is not polymorphic over a monad type-class.
//   - No transformer stack — FlowIO wraps a Func directly.
//   - Always async — Run returns Task<EffResult<A>>.

namespace Flowthru.Prelude;

/// <summary>
/// The Flowthru effect type. A <c>FlowIO&lt;A&gt;</c> is a description of
/// a computation that, when run, yields either a value of type
/// <typeparamref name="A"/> or a <see cref="RuntimeError"/>. It is the
/// substrate of every Flowthru runtime path.
/// </summary>
/// <remarks>
/// <para>
/// Lazy: no side effect runs until <see cref="Run"/> is called. Errors are
/// values, not exceptions: <see cref="Lift"/> and <see cref="LiftAsync"/>
/// capture thrown exceptions and route them through
/// <see cref="RuntimeError"/>.
/// </para>
/// <para>
/// FlowIO is framework-internal. Flow Developers and Catalog Developers
/// do not name FlowIO directly; the framework wraps the IO around their
/// pure-function transforms.
/// </para>
/// </remarks>
public sealed class FlowIO<A>
{
  private readonly Func<CancellationToken, Task<EffResult<A>>> _run;

  private FlowIO(Func<CancellationToken, Task<EffResult<A>>> run)
  {
    _run = run;
  }

  // ───────────────────────────────────────────────────────────────────────
  // Run
  // ───────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Run the effect. This is the only operation that performs side
  /// effects — every combinator above this point is pure description.
  /// </summary>
  public Task<EffResult<A>> Run(CancellationToken cancellationToken = default) =>
    _run(cancellationToken);

  // ───────────────────────────────────────────────────────────────────────
  // Constructors
  // ───────────────────────────────────────────────────────────────────────

  /// <summary>Lift a value into an effect that always succeeds.</summary>
  public static FlowIO<A> Pure(A value) =>
    new(_ => Task.FromResult<EffResult<A>>(new EffResult<A>.Success(value)));

  /// <summary>Lift an error into an effect that always fails.</summary>
  public static FlowIO<A> Fail(RuntimeError error) =>
    new(_ => Task.FromResult<EffResult<A>>(new EffResult<A>.Failure(error)));

  /// <summary>
  /// Lift a synchronous side-effecting function. Exceptions are captured
  /// and routed through <see cref="RuntimeError.External"/>;
  /// <see cref="OperationCanceledException"/> becomes
  /// <see cref="RuntimeError.Cancelled"/>.
  /// </summary>
  /// <param name="f">The side-effecting function.</param>
  /// <param name="source">
  /// Diagnostic label included in <see cref="RuntimeError.External.Source"/>
  /// when the function throws. Defaults to <c>"FlowIO.Lift"</c>.
  /// </param>
  public static FlowIO<A> Lift(Func<A> f, string source = "FlowIO.Lift") =>
    new(ct =>
    {
      if (ct.IsCancellationRequested)
      {
        return Task.FromResult<EffResult<A>>(
          new EffResult<A>.Failure(new RuntimeError.Cancelled("Cancellation requested"))
        );
      }

      try
      {
        return Task.FromResult<EffResult<A>>(new EffResult<A>.Success(f()));
      }
      catch (OperationCanceledException)
      {
        return Task.FromResult<EffResult<A>>(
          new EffResult<A>.Failure(new RuntimeError.Cancelled("Operation cancelled"))
        );
      }
      catch (Exception ex)
      {
        return Task.FromResult<EffResult<A>>(
          new EffResult<A>.Failure(new RuntimeError.External(source, ex))
        );
      }
    });

  /// <summary>
  /// Lift an asynchronous side-effecting function. Exceptions are captured
  /// and routed through <see cref="RuntimeError.External"/>;
  /// <see cref="OperationCanceledException"/> becomes
  /// <see cref="RuntimeError.Cancelled"/>.
  /// </summary>
  public static FlowIO<A> LiftAsync(
    Func<CancellationToken, Task<A>> f,
    string source = "FlowIO.LiftAsync"
  ) =>
    new(async ct =>
    {
      try
      {
        var value = await f(ct).ConfigureAwait(false);
        return new EffResult<A>.Success(value);
      }
      catch (OperationCanceledException)
      {
        return new EffResult<A>.Failure(new RuntimeError.Cancelled("Operation cancelled"));
      }
      catch (Exception ex)
      {
        return new EffResult<A>.Failure(new RuntimeError.External(source, ex));
      }
    });

  // ───────────────────────────────────────────────────────────────────────
  // Combinators
  // ───────────────────────────────────────────────────────────────────────

  /// <summary>Transform the success value.</summary>
  public FlowIO<B> Map<B>(Func<A, B> f) =>
    new(async ct =>
    {
      var result = await _run(ct).ConfigureAwait(false);
      return result switch
      {
        EffResult<A>.Success s => new EffResult<B>.Success(f(s.Value)),
        EffResult<A>.Failure x => new EffResult<B>.Failure(x.Error),
        _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
      };
    });

  /// <summary>Sequence another effect that depends on the success value.</summary>
  public FlowIO<B> Bind<B>(Func<A, FlowIO<B>> f) =>
    new(async ct =>
    {
      var result = await _run(ct).ConfigureAwait(false);
      return result switch
      {
        EffResult<A>.Success s => await f(s.Value).Run(ct).ConfigureAwait(false),
        EffResult<A>.Failure x => new EffResult<B>.Failure(x.Error),
        _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
      };
    });

  /// <summary>Transform the failure value while leaving successes untouched.</summary>
  public FlowIO<A> MapError(Func<RuntimeError, RuntimeError> f) =>
    new(async ct =>
    {
      var result = await _run(ct).ConfigureAwait(false);
      return result switch
      {
        EffResult<A>.Success s => s,
        EffResult<A>.Failure x => new EffResult<A>.Failure(f(x.Error)),
        _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
      };
    });

  /// <summary>
  /// Recover from a failure by running a handler effect. Successes pass through.
  /// </summary>
  public FlowIO<A> Catch(Func<RuntimeError, FlowIO<A>> handler) =>
    new(async ct =>
    {
      var result = await _run(ct).ConfigureAwait(false);
      return result switch
      {
        EffResult<A>.Success s => s,
        EffResult<A>.Failure x => await handler(x.Error).Run(ct).ConfigureAwait(false),
        _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
      };
    });

  // ───────────────────────────────────────────────────────────────────────
  // LINQ syntax (monadic; short-circuits on failure)
  // ───────────────────────────────────────────────────────────────────────

  public FlowIO<B> Select<B>(Func<A, B> f) => Map(f);

  public FlowIO<C> SelectMany<B, C>(
    Func<A, FlowIO<B>> bind,
    Func<A, B, C> project
  ) => Bind(t => bind(t).Map(n => project(t, n)));
}

/// <summary>
/// Non-generic helper that lets callers construct <see cref="FlowIO{A}"/>
/// values without having to spell the generic argument explicitly. The
/// methods here are pass-throughs to <c>FlowIO&lt;A&gt;</c>'s static
/// factories with the <c>A</c> inferred from the call's parameters.
/// </summary>
public static class FlowIO
{
  /// <summary>Lift a value into an effect that always succeeds.</summary>
  public static FlowIO<A> Pure<A>(A value) => FlowIO<A>.Pure(value);

  /// <summary>Lift an error into an effect that always fails.</summary>
  public static FlowIO<A> Fail<A>(RuntimeError error) => FlowIO<A>.Fail(error);

  /// <summary>
  /// Lift a synchronous side-effecting function. Exceptions are captured
  /// and routed through <see cref="RuntimeError.External"/>.
  /// </summary>
  public static FlowIO<A> Lift<A>(Func<A> f, string source = "FlowIO.Lift") =>
    FlowIO<A>.Lift(f, source);

  /// <summary>
  /// Lift an asynchronous side-effecting function with cancellation support.
  /// Exceptions are captured and routed through <see cref="RuntimeError.External"/>;
  /// <see cref="OperationCanceledException"/> becomes
  /// <see cref="RuntimeError.Cancelled"/>.
  /// </summary>
  public static FlowIO<A> LiftAsync<A>(
    Func<CancellationToken, Task<A>> f,
    string source = "FlowIO.LiftAsync"
  ) => FlowIO<A>.LiftAsync(f, source);
}
